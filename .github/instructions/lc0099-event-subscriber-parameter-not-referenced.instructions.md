---
applyTo: 'src/ALCops.LinterCop/**/ParameterNotReferenced*'
---

# LC0099 - Event Subscriber Parameter Not Referenced

## Purpose

Flags event subscriber parameters that are declared but never referenced in the subscriber body.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | LC0099 |
| Category | Design |
| Severity | Info |
| Has CodeFix | Yes (shared provider with LC0095, removes parameter from signature) |

## Architecture

### Analyzer

Implemented in the shared analyzer class `ParameterNotReferenced`.

Flow:
1. Analyzer resolves the method symbol and applies the common skip rules (handler methods, callback contracts, triggers, events, obsolete methods, interface implementations).
2. If `method.IsEventSubscriber()` is true, it reports `DiagnosticDescriptors.EventSubscriberParameterNotReferenced` (LC0099).
3. Unused-parameter detection is identical to LC0095: collect method parameters, walk body identifier usage, report missing references.

### CodeFix

Implemented in the shared provider class `ParameterNotReferencedCodeFixProvider`.

For LC0099, the provider registers one quick fix with:

| EquivalenceKey | Title resx key | Scope on Fix-All |
|---|---|---|
| `ParameterNotReferencedCodeFixProvider.EventSubscriber` | `EventSubscriberParameterNotReferencedCodeAction` | Only event subscribers |

The provider uses a custom FixAll implementation (`FixAllProvider.Create(FixAllAsync)`) and performs one-pass `RemoveNodes(...)` rewrites to avoid batch merge conflicts in shared parameter lists.

## Design decisions

| Decision | Rationale |
|---|---|
| Separate ID from LC0095 | Event subscriber signatures are often scaffolded with optional parameters; Info severity provides guidance without warning-level pressure. |
| Shared implementation with LC0095 | Keeps behavior and fixes consistent while allowing different ID/severity and configuration behavior. |
| Fix only signature parameter list | Safe and deterministic; does not attempt call-site rewrites. |
| Shared trivia-safe removal with LC0095 | Comments follow parser trivia ownership; balanced pragma pairs are removed or transferred to prevent unbalanced directives. |

## Known issues

- **`Optional<ImmutableArray<TextSpan>>` empty-with-`HasValue=true` quirk.** `FixAllAsync` must fall back to `GetDocumentDiagnosticsAsync` when spans are empty to keep FixAll stable in RoslynTestKit and host integrations.

## Test coverage

**HasDiagnostic (1 case):** EventSubscriber.
**HasFix (1 case):** RemoveSingleParameterEventSubscriber.
**HasFixAll (1 case):** RemoveTwoParametersEventSubscriber.
