---
applyTo: 'src/ALCops.LinterCop/**/ParameterNotReferenced*'
---

# LC0095 - Parameter Not Referenced

## Purpose

Flags parameters that are declared but never referenced in the procedure body. Extends CodeCop AA0137 to cover internal/public procedures and event subscribers.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | LC0095 |
| Category | Design |
| Severity | Warning |
| Has CodeFix | Yes (removes parameter from signature) |

## Architecture

Uses `RegisterCodeBlockAction` pattern:
1. Gets method syntax and symbol from `CodeBlockAnalysisContext`
2. Applies `ShouldAnalyzeMethod` filter (skips local, triggers, interface impls, events, obsolete)
3. Special case: event subscribers ARE included even though they are local (AA0137 excludes them)
4. Collects non-synthesized parameter names into a `Dictionary<string, IParameterSymbol>`
5. Walks `methodSyntax.Body.DescendantNodes()` for `IdentifierNameSyntax` matches (case-insensitive)
6. Reports diagnostic for any parameters with no matching identifier in the body

Key helper: `MethodImplementsInterfaceMethod()` from `ALCops.Common.Extensions.MethodSymbolInterfaceExtensions`

## Design decisions

| Decision | Rationale |
|---|---|
| Skip local methods | AA0137 handles them; avoids duplicate diagnostics |
| Include event subscribers | AA0137 explicitly excludes them despite being local |
| Skip interface implementations | Parameters are contractually required |
| Skip handler functions | Platform-enforced signatures (MessageHandler, ConfirmHandler, etc.); uses reflection on MethodSymbol.IsHandler |
| Skip ErrorInfo/Notification AddAction callbacks | Single ErrorInfo/Notification param in public/internal codeunit method is contractually required by platform AddAction API |
| Skip triggers | Platform-defined signatures |
| Skip event declarations | Parameters define subscriber contract |
| Skip obsolete methods | No value in modifying deprecated code |
| CodeFix removes param only | Updating call sites is complex and risky |
| Use `SemanticFacts.NameEqualityComparer` | Case-insensitive AL identifier comparison |

## Test coverage

**HasDiagnostic (7 cases):** InternalProcedure, PublicProcedure, EventSubscriber, MultipleParamsOneUnused, VarParameterUnused, ErrorInfoInPage, ErrorInfoMultipleParams.
**NoDiagnostic (11 cases):** LocalProcedure, TriggerUnusedParam, InterfaceImplementation, EventDeclaration, ObsoleteProcedure, AllParametersUsed, ParameterUsedInExpression, ErrorInfoCallbackInCodeunit, NotificationCallbackInCodeunit, MessageHandlerInCodeunit, ConfirmHandlerInCodeunit.
**HasFix (2 cases):** RemoveSingleParameter, RemoveMiddleParameter.
