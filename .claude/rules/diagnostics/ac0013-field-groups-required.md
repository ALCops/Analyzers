---
paths:
  - "src/ALCops.ApplicationCop/**/FieldGroupsRequired*"
  - "src/ALCops.ApplicationCop.Test/Rules/FieldGroupsRequired/**"
---

# AC0013: FieldGroupsRequired

## Purpose

Checks that tables used by pages define both `Brick` and `DropDown` field groups. These field groups control how records are displayed in list views and lookup dropdowns in Business Central.

Registers `RegisterCompilationStartAction` (read-only index of page-referenced tables) with an inner `RegisterSymbolAction` on `Table`; main type `FieldGroupsRequired`.

## Design decisions

| Decision | Rationale |
|---|---|
| Pages are discovered once per compilation with `GetDeclaredApplicationObjectSymbols()` filtered to `IPageTypeSymbol`, reading `RelatedTable` off the symbol | The earlier syntax-tree walk created one semantic model per file (7938 on the Base App, 4.3s); the symbol query is a single call and needs no parsing or binding. |
| Both `Brick` and `DropDown` are checked, an empty group counts as missing, and each missing group is its own diagnostic | The two groups serve different purposes. |
| `isEnabledByDefault: false` | Opt-in: not all teams enforce field group conventions. |

## Deliberate non-reports

- Obsolete tables.
- Setup tables, via the shared `TableHelper.IsSetupTable()` heuristic: a single `Code` primary-key field named `Primary Key`/`PrimaryKey`, or a parameterless, return-less `GetRecordOnce` method declared on the table itself (any accessibility). Field groups are not useful on a single-record table ([#287](https://github.com/ALCops/Analyzers/issues/287)).
- Temporary tables that no page references; a temporary table with a page is still checked.
- Tables that no page references at all.

## Test notes

- The rule is disabled by default, so the test fixture injects `FieldGroupsRequired.ruleset.json` (sets AC0013 to `Info`).
