---
paths:
  - "src/ALCops.PlatformCop/**/PageVariableSetRecordTemporaryRecord*"
  - "src/ALCops.PlatformCop.Test/Rules/PageVariableSetRecordTemporaryRecord/**"
---

# PC0036: PageVariableSetRecordTemporaryRecord

## Purpose

Detects calls to `Page.SetRecord()` where the record argument is a temporary record. The SDK explicitly states "You cannot use a temporary record for the Record parameter" and such calls will fail at runtime.

Registers `RegisterOperationAction` on `InvocationExpression`; main type `PageVariableSetRecordTemporaryRecord`.

## Design decisions

| Decision | Rationale |
|---|---|
| Only `SetRecord` | The SDK documents the restriction for `SetRecord` alone; `GetRecord`, `SetTableView` and `SetSelectionFilter` have none |
| Standalone rule instead of embedding in PC0017 | ALCops one-concern-per-ID pattern |

## Deliberate non-reports

- Records of `TableType = Temporary` tables declared without the `temporary` keyword: `IRecordTypeSymbol.Temporary` is only true for the keyword.
- `Page.Run`/`Page.RunModal` with a temporary record: strict scope matching the original LC0058 intent.
- Other page methods (`GetRecord`, `SetTableView`, `SetSelectionFilter`): no documented restriction.

## SDK facts

- `IRecordTypeSymbol.Temporary` reflects the `temporary` variable keyword only, not the table's `TableType`.
