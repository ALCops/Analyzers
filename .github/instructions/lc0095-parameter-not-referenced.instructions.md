---
applyTo: 'src/ALCops.LinterCop/**/ParameterNotReferenced*'
---

# LC0095 - Parameter Not Referenced

## Purpose

Flags parameters that are declared but never referenced in the procedure body for non-subscriber methods. Extends CodeCop AA0137 to cover internal/public procedures.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | LC0095 |
| Category | Design |
| Severity | Warning |
| Has CodeFix | Yes (shared provider with LC0099, removes parameter from signature) |

## Architecture

### Analyzer

Uses `RegisterCodeBlockAction` pattern:
1. Gets method syntax and symbol from `CodeBlockAnalysisContext`
2. Applies `GetDiagnosticDescriptor(method)` filter
3. For non-subscriber methods, returns descriptor `LC0095` (warning)
4. Collects non-synthesized parameter names into a `Dictionary<string, IParameterSymbol>`
5. Walks `methodSyntax.Body.DescendantNodes()` for `IdentifierNameSyntax` matches (case-insensitive)
6. Reports diagnostic for any parameters with no matching identifier in the body

Key helper: `MethodImplementsInterfaceMethod()` from `ALCops.Common.Extensions.MethodSymbolInterfaceExtensions`

### CodeFix

LC0095 and LC0099 share one provider class (`ParameterNotReferencedCodeFixProvider`) and one core implementation.

For LC0095, the provider registers one quick fix with:

| EquivalenceKey | Title resx key | Scope on Fix-All |
|---|---|---|
| `ParameterNotReferencedCodeFixProvider.RegularProcedure` | `ParameterNotReferencedCodeAction` | Only regular procedures |

LC0099 uses the same implementation with `ParameterNotReferencedCodeFixProvider.EventSubscriber` and its own title key.

Uses a **custom `FixAllProvider`** via `FixAllProvider.Create(FixAllAsync)` instead of `WellKnownFixAllProviders.BatchFixer`. See `codefix-development.instructions.md` for the general pattern and rationale.

Single-fix path (`RemoveUnreferencedParameter`):
- Loads syntax root, resolves the `ParameterSyntax` from the diagnostic span, applies the procedure-kind scope filter from diagnostic ID, and calls `root.RemoveNode(parameter, SyntaxRemoveOptions.KeepNoTrivia)`.

Fix-All path (`FixAllAsync`):
- Reads spans from `Optional<ImmutableArray<TextSpan>>` (see design decision below).
- Reads `fixAllContext.CodeActionEquivalenceKey` to derive `ProcedureKind`.
- Resolves every span to its `ParameterSyntax`, collects them in a `HashSet<ParameterSyntax>`, then applies **one** `root.RemoveNodes(..., SyntaxRemoveOptions.KeepNoTrivia)` call.

## Design decisions

| Decision | Rationale |
|---|---|
| Skip local methods | AA0137 handles them; avoids duplicate diagnostics |
| Exclude event subscribers from LC0095 | Subscribers are split to LC0099 with lower severity |
| Skip interface implementations | Parameters are contractually required |
| Skip handler functions | Platform-enforced signatures (MessageHandler, ConfirmHandler, etc.); uses reflection on MethodSymbol.IsHandler |
| Skip ErrorInfo/Notification AddAction callbacks | Single ErrorInfo/Notification param in public/internal codeunit method is contractually required by platform AddAction API |
| Skip triggers | Platform-defined signatures |
| Skip event declarations | Parameters define subscriber contract |
| Skip obsolete methods | No value in modifying deprecated code |
| CodeFix removes param only | Updating call sites is complex and risky |
| Use `SemanticFacts.NameEqualityComparer` | Case-insensitive AL identifier comparison |
| Custom `FixAllProvider` instead of `BatchFixer` | Multiple parameter removals in the same signature share a common ancestor (`ParameterListSyntax`). `BatchFixer` computes conflicting `ReplaceNode(parameterList, …)` edits per diagnostic and drops all but one, so only one of N parameters would be removed. Rewriting all parameters in one pass via `RemoveNodes` avoids the merge conflict entirely. |
| Use `RemoveNode`/`RemoveNodes` with `KeepNoTrivia` (not `ReplaceNode`) | `SeparatedSyntaxList` handles separator removal correctly when nodes are removed as a set. `KeepNoTrivia` prevents dangling comments/whitespace from the removed parameter (e.g. multi-line signatures with per-parameter comments). |
| Shared provider for LC0095 and LC0099 | Keeps fix behavior identical while surfacing separate IDs/severities. |
| Fall back to `GetDocumentDiagnosticsAsync` when `fixAllSpans` is empty | The AL SDK's `Optional<ImmutableArray<TextSpan>>` may report `HasValue = true` with an empty array (RoslynTestKit's default Document scope does this). Checking `!IsDefaultOrEmpty` and re-querying diagnostics keeps the FixAll functional in both hosts and tests. |

## Known issues

- **`Optional<ImmutableArray<TextSpan>>` empty-with-`HasValue=true` quirk.** When invoked from RoslynTestKit's default document-scope FixAll, `fixAllSpans.HasValue` is `true` but `fixAllSpans.Value.IsDefaultOrEmpty` is also `true`. Guarding only on `HasValue` produces a silent no-op. `FixAllAsync` therefore uses `fixAllSpans.HasValue && !fixAllSpans.Value.IsDefaultOrEmpty` before honoring the span filter, and falls back to `GetDocumentDiagnosticsAsync` otherwise.

## Test coverage

**HasDiagnostic (6 cases):** InternalProcedure, PublicProcedure, MultipleParamsOneUnused, VarParameterUnused, ErrorInfoInPage, ErrorInfoMultipleParams.
**NoDiagnostic (12 cases):** LocalProcedure, TriggerUnusedParam, InterfaceImplementation, InterfaceImplementationWrongCasing, EventDeclaration, ObsoleteProcedure, AllParametersUsed, ParameterUsedInExpression, ErrorInfoCallbackInCodeunit, NotificationCallbackInCodeunit, MessageHandlerInCodeunit, ConfirmHandlerInCodeunit.
**HasFix (3 cases):** RemoveSingleParameter, RemoveMiddleParameter, RemoveMiddleParameterMultiline.
**HasFixAll (2 cases):** RemoveTwoParametersSingleMethod, RemoveUnusedFromMultipleMethods.
