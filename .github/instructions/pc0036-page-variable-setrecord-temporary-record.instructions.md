---
applyTo: 'src/ALCops.PlatformCop/**/PageVariableSetRecordTemporaryRecord*'
---

# PC0036: Page SetRecord with temporary record

## Purpose

Detects calls to `Page.SetRecord()` where the record argument is a temporary record. The SDK explicitly states "You cannot use a temporary record for the Record parameter" and such calls will fail at runtime.

## Diagnostic properties

| Property | Value |
|----------|-------|
| ID | PC0036 |
| Category | Usage |
| Severity | Warning |
| Enabled by default | Yes |
| Has CodeFix | No |

## Design decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Scope | Only `SetRecord` | SDK only documents restriction for SetRecord; other page methods (GetRecord, SetTableView, SetSelectionFilter) have no documented restriction |
| TableType = Temporary | Not flagged unless variable has `temporary` keyword | `IRecordTypeSymbol.Temporary` is only true when the variable is declared as temporary |
| Page.Run/RunModal | Not covered | Strict scope matching original LC0058 intent |
| Standalone vs embedded in PC0017 | Standalone | ALCops one-concern-per-ID pattern |

## Architecture

- Registers `OperationAction` for `InvocationExpression`
- Checks: built-in method, name is "SetRecord", containing type is Page, single argument is a conversion from a temporary record
- Reports with page variable name as `{0}` argument

## Test coverage

**HasDiagnostic (2 cases):** TempVarSetRecord, TempTableTypeSetRecord.
**NoDiagnostic (3 cases):** NonTempVarSetRecord, TempVarGetRecord, TempTableTypeWithoutKeyword.
