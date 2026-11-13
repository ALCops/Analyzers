---
paths:
  - "src/ALCops.LinterCop/**/InternalProcedureNotReferenced*"
  - "src/ALCops.LinterCop.Test/Rules/InternalProcedureNotReferenced/**"
  - "src/ALCops.LinterCop.Test/Rules/InternalProcedureOnlyUsedInCurrentObject/**"
---

# LC0052: InternalProcedureNotReferenced

## Purpose

Reports internal procedures (and public procedures of an `Access = Internal` object) that nothing in the app references: they cannot be called from another app, so an unreferenced one is dead code. The same analyzer class also reports LC0053 (`InternalProcedureOnlyUsedInCurrentObject`): an internal procedure referenced only from its own object can be made local.

Registers one `CompilationAction`; main type `MethodSymbolAnalyzer`, which collects candidate methods from the declared objects and then walks every syntax tree, resolving each matching `IdentifierNameSyntax` through `SemanticModel.GetSymbolInfo`.

## Design decisions

| Decision | Rationale |
|---|---|
| One `CompilationAction` that walks every syntax tree, instead of per-declaration callbacks | A reference can sit in any file, and per-declaration callbacks are skipped by the host's partial-analysis pass, so an accumulator across callbacks would report false positives. |
| References are counted when `GetSymbolInfo` resolves the identifier to an `IMethodSymbol`, with a lowercase name pre-filter in front | Name matching alone would count same-named variables, fields, and methods of other objects as references; the pre-filter keeps the binding cost to identifiers that can matter. |
| An identifier that `GetSymbolInfo` leaves unresolved counts as a reference when it, or the member access it names, is the target of an assignment statement whose operation is an `IInvocationExpression`; the reference is that invocation's `TargetMethod` | A single-parameter procedure can be called with assignment syntax (`Obj.Proc := Value`, bare `Proc := Value`, `this.Proc := Value`) and the binder turns the whole statement into the call, so only the statement's operation names the method. Rejected: accepting `SymbolInfo.CandidateSymbols` for assignment targets, which would rely on overload-resolution-failure candidates instead of the binder's chosen method. The extra bind runs only for unresolved identifiers that pass the name pre-filter. |
| Any `internalsVisibleTo` entry in `app.json` turns the rule off for the whole app | A friend app (usually the test app) can call internal procedures, and its references are not in this compilation. |
| Single-parameter procedures taking `Notification` or `ErrorInfo` are skipped | They are action handlers bound by name at runtime (`Notification.AddAction`, `ErrorInfo.AddAction`), so no identifier references them. |
| Interface implementations, test-codeunit UI handlers (`IsHandler`), `[Test]` procedures, and `ObsoleteState = Pending` procedures are skipped | The platform or the test runner invokes them, or the procedure is already scheduled for removal. |
| Public procedures count only inside an `Access = Internal` object | Such a procedure is no more reachable from other apps than an internal one; public procedures of a public object are API. |
| LC0053 classifies only procedures declared `internal`; a public procedure of an internal object never gets LC0053 | Making it `local` changes the declared accessibility the author chose for the object's surface; LC0052 still covers it when it is unreferenced. |
| `FileNotFoundException` from `ManifestHelper.GetManifest` is caught and treated as no manifest | The assembly it loads by reflection is missing in some hosts, and an unhandled exception would silently drop the whole compilation's diagnostics. Trade-off: in such a host the `internalsVisibleTo` short-circuit cannot run, so an app with a friend app gets reports instead of none. |

## Deliberate non-reports

- Apps whose `app.json` declares `internalsVisibleTo`: nothing is reported, not even procedures the friend app cannot use.
- Test-codeunit UI handlers (`MessageHandler`, `ConfirmHandler`, `PageHandler` and the other attributes `IsHandler` recognises), interface implementations, test procedures, and pending-obsolete procedures.
- Public procedures of public objects.

## Known issues

- Event publishers (`[IntegrationEvent]`, `[BusinessEvent]`, `[InternalEvent]`) are not excluded, so an internal publisher that is never raised in the app is reported. The `IntegrationEvent` NoDiagnostic test case is disabled until the analyzer decides how publishers should be treated.
- Internal event subscribers are reported too: `IsHandler` only covers the test-codeunit handler attributes, and the `[EventSubscriber]` attribute names the event in a string literal, so no identifier ever references the subscriber.

## SDK facts

- `MethodSymbol.IsHandler` is true only for attributes tagged `AttributeTagKind.Handler`, which are the test-codeunit handlers (`MessageHandler` through `HttpClientHandler`); `[EventSubscriber]` does not carry the tag and event publishers and subscribers both have `MethodKind.Method` (verified against SDK 30.0 in `../nav-sdk-source`, `AttributeDefinitions.cs`).
- `ManifestHelper.GetManifest(compilation)` returns null when `Compilation.FileSystem` is null or holds no `app.json`, and loads `Microsoft.Dynamics.Nav.Analyzers.Common` by reflection.
- Assignment-syntax calls: `Binder.BindAssignmentStatement` (SDK 30.0 `Binder.cs` ~8392; the same code at the SDK 12 floor, `Binder.cs` ~7647) binds the target non-invoked, and when that yields a call to a method with one parameter it rebinds the statement as an invocation with the source as the argument and wraps it in a `BoundExpressionStatement`. The `BoundCall`'s syntax is the `AssignmentStatementSyntax`. Probed on SDK 18.0.41 for the named, bare and `this` receivers: `GetSymbolInfo` on the procedure identifier (and on the member access) returns a null `Symbol` with the method as the only `CandidateSymbols` entry and `CandidateReason.OverloadResolutionFailure`; `GetOperation(assignmentStatement)` returns the `IInvocationExpression` itself (the lowest bound node for that syntax, not the `IExpressionStatement`), with `TargetMethod` set to the procedure. The parameter type is not restricted. The getter form without parentheses (`X := Obj.Proc`) already resolves through `GetSymbolInfo`. Compound assignment (`Obj.Proc += 1`) has no call form: `BindCompoundAssignmentStatement` binds the target as an assignable value, so a method there is a compile error.

## Test notes

- The `internalsVisibleTo` case needs a manifest: `CreateFixtureWithInternalsVisibleTo` injects an `app.json` through `MemoryFileSystem`, and the other cases keep the plain fixture so they compile without one. Dropping the `app.json` from that fixture makes the case fail, which proves the manifest is read.
- LC0052 and LC0053 tests share the analyzer class but live in separate test folders, one per diagnostic ID.
- `ImplementsInterfaceMethod` (an internal procedure implementing a public interface method) is skipped from SDK 30 with `SkipTestIfVersionIsTooHigh`. The compiler has gated `WRN_ERR_InternalMethodCannotImplementInterface` on `RuntimeVersion.Spring2027` since at least SDK 18 (`ErrorFacts.GetWarningErrorLimit`, `CodeunitObjectMembers`), but SDK 30 is the first whose default runtime is Spring2027 (30.0), so the harness compiles the fixture with error AL0921 there. An internal procedure implementing an `Access = Internal` interface stays warning-free on every runtime.
- `PropertySyntaxSetterThis` uses `this` and is skipped below runtime 14.0 with `SkipTestIfVersionIsTooLow`. The LC0053 pair (`OnlyUsedInCurrentObjectPropertySyntax`, `UsedInOtherObjectOnlyPropertySyntax`) proves the same-object versus other-object classification sees the assignment-syntax call. `PropertySyntaxShadowedByVariable` guards the other direction: a same-named local assigned with `:=` binds as an assignment and must not count.
