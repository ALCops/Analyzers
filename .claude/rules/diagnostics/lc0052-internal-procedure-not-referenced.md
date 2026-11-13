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
| References are counted only when `GetSymbolInfo` resolves the identifier to an `IMethodSymbol`, with a lowercase name pre-filter in front | Name matching alone would count same-named variables, fields, and methods of other objects as references; the pre-filter keeps the binding cost to identifiers that can matter. |
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

## Test notes

- The `internalsVisibleTo` case needs a manifest: `CreateFixtureWithInternalsVisibleTo` injects an `app.json` through `MemoryFileSystem`, and the other cases keep the plain fixture so they compile without one. Dropping the `app.json` from that fixture makes the case fail, which proves the manifest is read.
- LC0052 and LC0053 tests share the analyzer class but live in separate test folders, one per diagnostic ID.
