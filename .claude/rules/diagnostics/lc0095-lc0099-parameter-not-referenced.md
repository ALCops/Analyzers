---
paths:
  - "src/ALCops.LinterCop/**/ParameterNotReferenced*"
  - "src/ALCops.LinterCop.Test/Rules/ParameterNotReferenced/**"
---

# LC0095 / LC0099: ParameterNotReferenced

## Purpose

Flags parameters that are declared but never referenced in the procedure body.

- **LC0095** covers non-subscriber methods (internal/public procedures), extending CodeCop AA0137. Severity Warning.
- **LC0099** covers event subscriber parameters that are declared but never referenced in the subscriber body. Severity Info.

Both IDs are emitted by the single analyzer class `ParameterNotReferenced` and fixed by the shared `ParameterNotReferencedCodeFixProvider` (removes the parameter from the signature).

Registers `CodeBlockAction`; main type `ParameterNotReferenced` (descriptor selected by `method.IsEventSubscriber()`).

## Design decisions

| Decision | Rationale |
|---|---|
| Two IDs with different severities from one analyzer and one CodeFix provider ([#425](https://github.com/ALCops/Analyzers/issues/425)) | Event subscriber signatures are often scaffolded with optional parameters, so LC0099 reports at Info while LC0095 stays Warning; the shared implementation keeps detection and fixes identical. |
| References are resolved through the operation tree: a parameter is referenced only when an `IParameterReferenceExpression.Parameter` in the bound body equals it; a case-insensitive name scan (`SemanticFacts.NameEqualityComparer`) runs first, and binding is skipped only when no parameter name appears in the body. Otherwise the body is bound once (pre-computed for a `CodeBlockAction`, see `analyzer-performance.md`) and walked until every name-matched candidate is confirmed, which ends early in the common case where parameters are used near the top | An identifier with the parameter's name may be a field, method or member name (`MyTable.Name`, a `SetRange` field argument), which hid unused parameters. |
| When `GetOperation` returns null or the bound body `IsInvalid`, the name-scan result stands | Binding errors propagate up to the block, and references under invalid nodes (a call on a record of an unresolved table) are missing from the tree; reporting from it would flag parameters that are used. |

## Deliberate non-reports

- Local methods: CodeCop AA0137 already covers them; avoids duplicate diagnostics.
- Interface implementations: the parameters are contractually required.
- Default interface method bodies (runtime 18.0): the interface declares the contract for all implementers, and defaults are typically no-op hooks or overload bridges that ignore a parameter by design (Microsoft's PEPPOL Tax Info Provider interface, w1-29, ships one of each). A codeunit that overrides a default method is already covered by the interface-implementation exemption, also when the interface comes from a dependency app.
- Handler functions (`MessageHandler`, `ConfirmHandler`, ...): platform-enforced signatures, detected through reflection on `MethodSymbol.IsHandler`.
- ErrorInfo/Notification `AddAction` callbacks: a single `ErrorInfo`/`Notification` parameter on a public/internal codeunit method is required by the platform API.
- Triggers: platform-defined signatures.
- Event declarations: the parameters define the subscriber contract.
- Obsolete methods: no value in modifying deprecated code.

## SDK facts

- `OperationWalker` does not descend into `IIsExpression`, `IAsExpression`, `IConditionalOperatorExpression` (SDK 14+), the test page accesses (`ITestPartAccess`, `ITestFilterAccess`, `ITestFilterFieldAccess`), `IPagePartAccess`, `IQueryElementAccess`, `IQueryDataItemAccess`, `ITableKeyAccess`, `ITableFieldGroupAccess`, the report accesses (`IReportLabelAccess`, `IReportDataItemAccess`, `IReportColumnAccess`, `IReportLayoutAccess`) or `IXmlPortNodeAccess`: they fall to `DefaultVisit`. `IOperation` has no `Children`. The access kinds have visit methods at the 12.0 floor, so the walker overrides them to visit their `Instance`. Is/as/conditional do not exist in the netstandard SDK the analyzer compiles against, so only they (and operand-free leaves) go through a `DefaultVisit` override that confirms candidates by name on the node's syntax.
- A default-bodied interface method has the interface type symbol as its `ContainingSymbol` (the SDK's own `MethodSymbol.IsDefaultInterfaceMethod` tests `ContainingSymbol.Kind == SymbolKind.Interface`, nav-sdk-source v30.0.44.47290-prerelease `Microsoft.Dynamics.Nav.CodeAnalysis/net10.0/Microsoft.Dynamics.Nav.CodeAnalysis.Symbols/MethodSymbol.cs:96`), so the analyzer checks `ContainingSymbol is IInterfaceTypeSymbol`, which exists at the 12.0 floor. `IMethodSymbol.IsOptional` would say the same but is missing from the 12.0 (netstandard) SDK. Below runtime 18.0 an interface method has no body, so the check needs no runtime gate.

## Test notes

- `RequireMinimumVersion("13.0")` on all tests: `IMethodSymbol.IsLocal` is only reliable from SDK v13.
- The default-interface-method tests (`*DefaultInterfaceMethod`, `NoDiagnosticWithDependencyProject`) are gated at 18.0: default bodies need runtime 18.0 and the harness compiles at the SDK's current release. The dependency case puts the interface in a `DependencyProjects` workspace project.
- Fixture sets include `HasFixAll/` (multi-parameter, comments, pragmas, mixed procedure kinds) and `NoFix/` (conditional-compilation parameter).

## CodeFix: ParameterNotReferencedCodeFixProvider

| Decision | Rationale |
|---|---|
| Removes the parameter from the signature only; call sites are never rewritten | Call-site updates are complex and risky; the signature edit is deterministic. |
| No fix offered for a parameter that owns `#if`/`#else`/`#endif` trivia (diagnostic still reported) | Such a parameter can also own inactive branch text; removing it could silently delete or reformat inactive code. |
| One equivalence key per ID (`...RegularProcedure`, `...EventSubscriber`); FixAll derives the procedure kind from the key | FixAll on LC0095 must not touch subscribers and vice versa. |
| Custom `FixAllProvider` with a single-pass `RemoveNodes` instead of `WellKnownFixAllProviders.BatchFixer` | Multiple removals in one signature share the `ParameterListSyntax` ancestor; `BatchFixer` produces conflicting `ReplaceNode` edits and keeps only one, so only one of N parameters would be removed. See `.claude/rules/codefix-development.md`. |
| `RemoveNodes` with `KeepNoTrivia`; parameter-bound comments are dropped except those immediately preceding a preserved pragma | `SeparatedSyntaxList` handles separator removal correctly when nodes are removed as a set; a comment that annotates a pragma belongs with that pragma. |
| Pragma pairing: only balanced, active `#pragma warning` pairs that lie wholly inside the parameter list and wrap removed parameters exclusively are deleted; every other directive (pairs spanning retained parameters, the body or beyond the procedure, mismatched code lists, inactive branches) is transferred with its comments to the next remaining parameter | A directive attached to a removed parameter may have its matching partner on a neighbouring parameter or outside the procedure; deleting it would change the diagnostic scope of unrelated code. |
| Fall back to `GetDocumentDiagnosticsAsync` when `fixAllSpans` is empty | The `Optional<ImmutableArray<TextSpan>>` HasValue-with-empty quirk; see `.claude/rules/codefix-development.md`. |
