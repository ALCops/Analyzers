---
paths:
  - "src/ALCops.PlatformCop/**/PageVariableSetRecordTemporaryRecord*"
  - "src/ALCops.PlatformCop.Test/Rules/PageVariableSetRecordTemporaryRecord/**"
---

# PC0036: PageVariableSetRecordTemporaryRecord

## Purpose

Detects `Page.SetRecord()` (on a page variable or `CurrPage`) with a temporary record when the page reads its records from the database. `SetRecord` only positions the page on the record's key; it never hands the record's data to the page. A database-backed page looks that key up in the real table, so the temporary data is never shown and the page silently opens on another record (or none). A temporary record alone is not the signal: a page whose source is temporary shows the record as long as it has loaded it itself.

Registers `RegisterOperationAction` on `InvocationExpression`; main type `PageVariableSetRecordTemporaryRecord`.

**References:** [Page.SetRecord (Microsoft Learn)](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/page/page-setrecord-method) (its "You cannot use a temporary record" sentence is misleading: there is no runtime error), [ALCops/Analyzers#368](https://github.com/ALCops/Analyzers/issues/368), [LinterCop discussion #611](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/611)

## Design decisions

| Decision | Rationale |
|---|---|
| Report only database-backed target pages, rather than retiring the rule or reporting every temporary argument | Microsoft's own code calls `SetRecord` with temporary records in many places, every one on a page with a temporary source; the remaining case (a temp record on a database-backed card or list) is a reproduced silent wrong-record bug. |
| Temporary-ness by keyword or `TableType = Temporary` (Common `IsTemporary()`) | `IRecordTypeSymbol.Temporary` sees only the keyword, so a record of a temporary table declared without it would be missed; the same helper decides whether the page's source table is temporary. |
| An `OnFindRecord` trigger counts as "the page supplies its own data" | A page that overrides `OnFindRecord` serves its rows from its own buffer, so it can show a record that does not exist in the database. |
| `CurrPage` covered, resolved through `Instance.Type.OriginalDefinition is IPageTypeSymbol` | One cast covers page variables, `CurrPage` and `CurrPage` in a pageextension (whose original definition is the target page) without a `NavTypeKind` switch. |
| Severity Warning kept | The reported case is a silent wrong-record bug, not a style issue. |

## Deliberate non-reports

- Pages with `SourceTableTemporary = true`: they show the record once they have loaded it, the documented lookup pattern.
- Pages whose source table is `TableType = Temporary`.
- Pages that declare an `OnFindRecord` trigger.
- Pages without a source table: there is nothing to position.
- `Page.Run`/`Page.RunModal` with a temporary record: that is the supported way to show temporary data.
- `GetRecord`, `SetTableView`, `SetSelectionFilter`: they do not position the page on a record that the page must find.
- `SetRecord` on a temporary-source page whose loaded dataset may not contain the record: not statically decidable.

## Known issues

- The `OnFindRecord` check sees only triggers declared in the current compilation. Page triggers are not part of a referenced app's symbols: `SourceTriggerSymbol.IsLocal` is always true and `SerializableSymbolModelConverter.FilterMethods` drops local non-event methods, and `ReferencePageTypeSymbol.Triggers` is empty. A partner app calling `SetRecord(Temp)` on a Base App page that serves its data through `OnFindRecord` but has a database source table is therefore still reported. Accepted: no such page is a public lookup target in the Microsoft corpus.

## SDK facts

- `IRecordTypeSymbol.Temporary` reflects the `temporary` keyword only (`Binder.BindVariableOrParameterType`), not the table's `TableType`.
- `ISymbolWithTriggers.GetTriggerSymbol(name)` on a page looks the name up in `GetCodeMembers(name)`, a dictionary keyed with `SemanticFacts.NameEqualityComparer`, so the lookup is case-insensitive (verified against SDK 12.7 and 30.0 in `../nav-sdk-source`).
- `NavTypeKind.CurrPage` (202), `IPageBaseTypeSymbol.RelatedTable` and `ISymbolWithTriggers.GetTriggerSymbol` exist at the 12.0 floor; `PageReferenceTypeSymbol.OriginalDefinition` is the `IPageTypeSymbol`.
- The SDK's own help text (`Page_SetRecord_Record`) repeats the Learn sentence about temporary records; the runtime does not enforce it.

## Test notes

- The pageextension fixture is gated at 13.0 (same-module extension target).
