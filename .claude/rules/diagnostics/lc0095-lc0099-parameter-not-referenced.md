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
| Identifier matching via `SemanticFacts.NameEqualityComparer` | AL identifiers are case-insensitive. |

## Deliberate non-reports

- Local methods: CodeCop AA0137 already covers them; avoids duplicate diagnostics.
- Interface implementations: the parameters are contractually required.
- Handler functions (`MessageHandler`, `ConfirmHandler`, ...): platform-enforced signatures, detected through reflection on `MethodSymbol.IsHandler`.
- ErrorInfo/Notification `AddAction` callbacks: a single `ErrorInfo`/`Notification` parameter on a public/internal codeunit method is required by the platform API.
- Triggers: platform-defined signatures.
- Event declarations: the parameters define the subscriber contract.
- Obsolete methods: no value in modifying deprecated code.

## Test notes

- `RequireMinimumVersion("13.0")` on all tests: `IMethodSymbol.IsLocal` is only reliable from SDK v13.
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
