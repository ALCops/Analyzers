---
paths:
  - "src/ALCops.LinterCop/**/ParameterNotReferenced*"
---

# LC0095 / LC0099: ParameterNotReferenced

## Purpose

Flags parameters that are declared but never referenced in the procedure body.

- **LC0095** covers non-subscriber methods (internal/public procedures), extending CodeCop AA0137. Severity Warning.
- **LC0099** covers event subscriber parameters that are declared but never referenced in the subscriber body. Severity Info.

Both IDs are emitted by the single analyzer class `ParameterNotReferenced` and fixed by the shared `ParameterNotReferencedCodeFixProvider` (removes the parameter from the signature).

## Design decisions

| Decision | Rationale |
|---|---|
| Skip local methods | AA0137 handles them; avoids duplicate diagnostics |
| Exclude event subscribers from LC0095 | Subscribers are split to LC0099 with lower severity |
| LC0099: Separate ID from LC0095 | Event subscriber signatures are often scaffolded with optional parameters; Info severity provides guidance without warning-level pressure. |
| Shared implementation / provider for LC0095 and LC0099 | Keeps behavior and fixes identical while surfacing separate IDs/severities and configuration behavior. |
| Skip interface implementations | Parameters are contractually required |
| Skip handler functions | Platform-enforced signatures (MessageHandler, ConfirmHandler, etc.); uses reflection on MethodSymbol.IsHandler |
| Skip ErrorInfo/Notification AddAction callbacks | Single ErrorInfo/Notification param in public/internal codeunit method is contractually required by platform AddAction API |
| Skip triggers | Platform-defined signatures |
| Skip event declarations | Parameters define subscriber contract |
| Skip obsolete methods | No value in modifying deprecated code |
| CodeFix removes param only (fix only signature parameter list) | Updating call sites is complex and risky; safe and deterministic, does not attempt call-site rewrites |
| Do not offer a CodeFix for conditional parameters | A parameter that owns `#if` / `#else` / `#endif` trivia can also own inactive branch text. Removing it can silently delete or reformat inactive code, so the analyzer reports the diagnostic without registering a fix. |
| Use `SemanticFacts.NameEqualityComparer` | Case-insensitive AL identifier comparison |
| Custom `FixAllProvider` instead of `BatchFixer` | Multiple parameter removals in the same signature share a common ancestor (`ParameterListSyntax`). `BatchFixer` computes conflicting `ReplaceNode(parameterList, …)` edits per diagnostic and drops all but one, so only one of N parameters would be removed. Rewriting all parameters in one pass via `RemoveNodes` avoids the merge conflict entirely. |
| Use `RemoveNodes` with `KeepNoTrivia` (not `ReplaceNode`) | `SeparatedSyntaxList` handles separator removal correctly when nodes are removed as a set. Parameter-bound comments are removed, except comments immediately preceding a preserved pragma, which are transferred with that directive. |
| Preserve pragma pairing explicitly | Process only active `PragmaWarningDirectiveTriviaSyntax` nodes; directives from inactive conditional branches remain untouched. A pragma may be attached to a removed parameter while its matching directive belongs to a neighboring parameter or lies outside the procedure. Remove only stack-paired directives that are wholly within one parameter list and wrap removed parameters exclusively; otherwise transfer the directive and its immediately preceding comments to the next remaining parameter. This includes balanced pairs that span both removed and retained parameters, pairs that extend into the method body, pairs beyond the procedure, and mismatched, empty, or partially overlapping error-code lists, which must remain intact. Pair directives through the structured `DisableOrRestoreKeyword` and a case-insensitive, deduplicated, canonical `ErrorCodes` set, including nested pairs with the same error codes. When multiple directives move to a closing parenthesis, insert their collected trivia in source order. After parameter annotations rewrite the tree, resolve balanced-pair directives by their original spans and remove only the targeted directive plus trailing whitespace to avoid deleting an adjacent preserved directive or duplicating indentation. |
| Fall back to `GetDocumentDiagnosticsAsync` when `fixAllSpans` is empty | The AL SDK's `Optional<ImmutableArray<TextSpan>>` may report `HasValue = true` with an empty array (RoslynTestKit's default Document scope does this). Checking `!IsDefaultOrEmpty` and re-querying diagnostics keeps the FixAll functional in both hosts and tests. |

## Architecture

### Analyzer

Uses `RegisterCodeBlockAction` pattern:
1. Gets method syntax and symbol from `CodeBlockAnalysisContext`
2. Applies `GetDiagnosticDescriptor(method)` filter (common skip rules: handler methods, callback contracts, triggers, events, obsolete methods, interface implementations)
3. For non-subscriber methods, returns descriptor `LC0095` (warning); if `method.IsEventSubscriber()` is true, returns `DiagnosticDescriptors.EventSubscriberParameterNotReferenced` (LC0099)
4. Collects non-synthesized parameter names into a `Dictionary<string, IParameterSymbol>`
5. Walks `methodSyntax.Body.DescendantNodes()` for `IdentifierNameSyntax` matches (case-insensitive)
6. Reports diagnostic for any parameters with no matching identifier in the body

Unused-parameter detection is identical for both IDs.

Key helper: `MethodImplementsInterfaceMethod()` from `ALCops.Common.Extensions.MethodSymbolInterfaceExtensions`

## Relationship between LC0095 and LC0099

LC0095 and LC0099 were split out of one unreferenced-parameter rule (commit 0f39eeb, #425). They share one analyzer class (`ParameterNotReferenced`) and one code-fix provider; only the descriptor differs, selected by `method.IsEventSubscriber()`. Event subscriber signatures are often scaffolded with optional parameters, so LC0099 reports at Info severity while LC0095 stays at Warning.

## Known issues

- **`Optional<ImmutableArray<TextSpan>>` empty-with-`HasValue=true` quirk.** When invoked from RoslynTestKit's default document-scope FixAll, `fixAllSpans.HasValue` is `true` but `fixAllSpans.Value.IsDefaultOrEmpty` is also `true`. Guarding only on `HasValue` produces a silent no-op. `FixAllAsync` therefore uses `fixAllSpans.HasValue && !fixAllSpans.Value.IsDefaultOrEmpty` before honoring the span filter, and falls back to `GetDocumentDiagnosticsAsync` otherwise, to keep FixAll stable in RoslynTestKit and host integrations.

## CodeFix: ParameterNotReferencedCodeFixProvider

LC0095 and LC0099 share one provider class (`ParameterNotReferencedCodeFixProvider`) and one core implementation.

The provider registers one quick fix per ID with:

| ID | EquivalenceKey | Title resx key | Scope on Fix-All |
|---|---|---|---|
| LC0095 | `ParameterNotReferencedCodeFixProvider.RegularProcedure` | `ParameterNotReferencedCodeAction` | Only regular procedures |
| LC0099 | `ParameterNotReferencedCodeFixProvider.EventSubscriber` | `EventSubscriberParameterNotReferencedCodeAction` | Only event subscribers |

Uses a **custom `FixAllProvider`** via `FixAllProvider.Create(FixAllAsync)` instead of `WellKnownFixAllProviders.BatchFixer`, and performs one-pass `RemoveNodes(...)` rewrites to avoid batch merge conflicts in shared parameter lists. See `.claude/rules/codefix-development.md` for the general pattern and rationale.

Single-fix path (`RemoveUnreferencedParameter`):
- Loads syntax root, resolves the `ParameterSyntax` from the diagnostic span, applies the procedure-kind scope filter from diagnostic ID, and delegates the removal to `RemoveParameters`.

Fix-All path (`FixAllAsync`):
- Reads spans from `Optional<ImmutableArray<TextSpan>>` (see design decision above).
- Reads `fixAllContext.CodeActionEquivalenceKey` to derive `ProcedureKind`.
- Resolves every span to its `ParameterSyntax`, collects them in a `HashSet<ParameterSyntax>`, then delegates all removals to `RemoveParameters`, which rewrites directives before one consolidated `RemoveNodes` call.
- `RemoveParameters` removes balanced pragma pairs that exclusively wrap removed parameters, and transfers directives whose matching pair extends beyond the removed parameter to the next remaining parameter.

Trivia-safe removal (shared by both IDs): parameter-bound comments are removed, while comments immediately preceding a transferred active pragma move with it. Inactive conditional-branch directives remain unchanged. Stack-paired active pragma scopes are removed only when they stay within one parameter list and cover removed parameters exclusively; all other directives are preserved or relocated.
