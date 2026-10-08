---
paths:
  - "src/ALCops.ApplicationCop/**/NotBlankOnPrimaryKeyField*"
  - "src/ALCops.ApplicationCop/**/NotBlankRequiredOnPrimaryKeyField*"
  - "src/ALCops.ApplicationCop/**/NotBlankNotAllowedOnPrimaryKeyField*"
  - "src/ALCops.ApplicationCop.Test/Rules/NotBlankRequiredOnPrimaryKeyField/**"
  - "src/ALCops.ApplicationCop.Test/Rules/NotBlankNotAllowedOnPrimaryKeyField/**"
---

# AC0002/AC0003: NotBlankOnPrimaryKeyField

## Purpose

AC0002 asks a table whose primary key is a single Code or Text field to set `NotBlank` explicitly. A record with a blank key is easy to create by accident, and renaming or deleting it cascades through every `TableRelation` that points at the table. AC0003 is the inverse for tables where any Code field has a `TableRelation` to `No. Series`: there the key can be assigned after the record is inserted, so `NotBlank = true` can make the insert fail.

Registers `RegisterSymbolAction` on `Table`; main type `NotBlankOnPrimaryKeyField`, which reports both IDs.

**References:** [#575](https://github.com/ALCops/Analyzers/issues/575) (setup tables)

## Design decisions

| Decision | Rationale |
|---|---|
| One analyzer reports both IDs and splits on whether the table has a Code field with a `TableRelation` to `No. Series` | The two rules give opposite advice for the same property; deciding the branch once keeps them from both firing on one field. |
| AC0002 accepts any explicit `NotBlank` value, `false` included | The rule asks for a deliberate decision, not a particular value; `NotBlank = false` documents that a blank key is intended. |
| Setup tables are skipped with the shared `TableHelper.IsSetupTable` instead of a rule-local name check | AC0013 already uses that heuristic, so both rules agree on what a setup table is; a `NotBlank = false` CodeFix for setup tables was rejected because the report itself is noise there. |

## Deliberate non-reports

- Obsolete tables, primary keys with more than one field, and single-field keys whose type has no length (Integer, Guid and so on).
- AC0002 on setup tables, as recognised by `TableHelper.IsSetupTable()`: a single `Code` primary-key field named `Primary Key`/`PrimaryKey`, or a parameterless, return-less `GetRecordOnce` method on the table. Setup tables are assumed to be singletons whose always-blank key no other table relates to, so there is no cascade to guard; the helper recognises them by convention and does not check inbound relations.
- AC0003 on a primary-key field named `Name`.

## Known issues

- The setup-table heuristic is Code-only and name-based: a `"Primary Key"` of type Text, or a singleton keyed by `PK` or `Code` without `GetRecordOnce`, still reports AC0002. The Base Application uses Code `"Primary Key"` for its setup tables, so the gap is accepted rather than widening a heuristic AC0013 shares.
- Changing `TableHelper.IsSetupTable` changes both AC0002 and AC0013.
- On netstandard2.1 the No. Series lookup does not filter on the field's type (`IFieldSymbol.Type` is net8.0+), so a non-Code field related to `No. Series` also selects the AC0003 branch there.

## CodeFix: NotBlankRequiredOnPrimaryKeyFieldCodeFixProvider

| Decision | Rationale |
|---|---|
| Always inserts or overwrites `NotBlank = true` | A table that reaches the fix is not a setup table, so a blank key is almost certainly unintended; offering `NotBlank = false` instead was rejected for the same reason the setup-table case is skipped rather than fixed. |
