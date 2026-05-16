---
applyTo: 'src/ALCops.ApplicationCop/**/RunPageImplementPageManagement*'
---

# AC0006: Use "Page Management" codeunit instead of Page.Run

## Purpose

Detects `Page.Run(...)` and `Page.RunModal(...)` calls that can be replaced with the `"Page Management"` codeunit's methods (`PageRun`, `PageRunModal`, `PageRunAtField`). The CodeFix refactors the call and adds the necessary variable declaration and `using` directive.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | AC0006 |
| Prefix | AC |
| Severity | Warning |
| Category | Design |
| DiagnosticIds field | `RunPageImplementPageManagement` |

## Analyzer logic

Triggers on `InvocationExpression` operations where:
1. Target method is a `BuiltInMethod` on a `Page` type with >= 2 arguments
2. Not `EnqueueBackgroundTask`
3. Return type `Action` is not used (Page Management doesn't support it)
4. First argument is either literal `0` or an `OptionAccessExpression` (e.g., `Page::MyPage`)
5. For `OptionAccessExpression`, the record must be in the `SupportedRecords` dictionary (well-known BC tables)

## CodeFix logic

The CodeFix (`RunPageImplementPageManagementCodeFixProvider`) performs three transformations:

1. **Replace invocation**: `Page.Run(0, Rec)` → `PageManagement.PageRun(Rec)`
2. **Add variable**: If no existing `Codeunit "Page Management"` variable exists (local or global), adds `PageManagement: Codeunit "Page Management"` as a local variable
3. **Add using directive**: If the file has a `namespace` declaration and `using Microsoft.Utilities;` is not already present, adds it in sorted order among existing usings

## Design decisions

| Decision | Rationale |
|---|---|
| Add `using` only when namespace present | Files without `namespace` resolve globally; adding `using` would be unnecessary |
| Skip if `using Microsoft.Utilities;` exists | Prevents duplicates during FixAll or when user already has the import |
| Sorted insertion for usings | Maintains alphabetical order among existing `using` directives |
| Replicate sorted insertion (not reflection) | SDK's `NamespaceActionUtilities` is `internal`; public API (`CompilationUnitSyntax.WithUsings`) suffices |
| Case-insensitive duplicate check | AL is case-insensitive; `Microsoft.Utilities` == `microsoft.utilities` |

## Test coverage

**HasDiagnostic (4 cases):** PageRunModalPageIdentifierAndRecord, PageRunModalZeroIdentifierAndRecord, PageRunPageIdentifierAndRecord, PageRunZeroIdentifierAndRecord.
**NoDiagnostic (2 cases):** PageRunPageIdentifierWithoutRecord, PageRunWithReturnTypeAction.
**HasFix (7 cases):** PageRunModelPageIdentifierAndRecord, PageRunModelPageIdentifierAndRecordWithPageFIeld, PageRunPageIdentifierAndRecord, PageRunPageIdentifierAndRecordWithPageField, PageRunZeroIdentifierAndRecord, PageRunZeroIdentifierAndRecordWithNamespace, PageRunPageIdentifierAndRecordWithNamespaceAndUsing.
