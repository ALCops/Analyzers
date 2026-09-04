---
paths:
  - "src/ALCops.ApplicationCop/**/CaptionRequired*"
  - "src/ALCops.ApplicationCop.Test/Rules/CaptionRequired/**"
---

# AC0011: CaptionRequired

## Purpose

Checks that user-facing symbols define a `Caption` (or `CaptionClass`/`CaptionML`) property: pages, tables, table fields, page controls, actions, enum values, permission sets, and analysis views.

Registers `RegisterSymbolAction` on `Page`, `Query`, `Table`, `Field`, `Action`, `EnumValue`, `Control`, `PermissionSet` and `AnalysisView`; main type `CaptionRequired`.

## Design decisions

| Decision | Rationale |
|---|---|
| `Caption`, `CaptionClass` or `CaptionML` all satisfy the check | Any of the three yields a user-facing caption. |
| Field controls fall back to the `RelatedFieldSymbol` caption, part controls to the `RelatedPartSymbol` caption | A page field or part without its own caption inherits the source field's or part's caption at runtime. |
| Promoted `SplitButton` groups are checked only when they contain repeater-scoped actionrefs | That is the only case in which the runtime displays the group caption. |

## Deliberate non-reports

- `ShowCaption = false` suppresses the check: an explicitly hidden caption needs no value.
- API pages are skipped entirely (`IsInApiPage`): they are not user-facing. No pageextension handling is needed because API pages cannot be extended.
- Field controls in HeadlinePart pages, including those added by a pageextension targeting one, are skipped (`IsInHeadlinePartPage`): the runtime ignores `Caption` there and only honours `Expression`, `Visible`, `ApplicationArea`, `Drilldown` and `DrillDownPageID` ([docs](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-create-role-center-headline#in-development), [#293](https://github.com/ALCops/Analyzers/issues/293)). The page object, actions and groups of a HeadlinePart page remain checked.
- Area, Grid, Repeater, UserControl and SystemPart controls have no user-facing caption requirement.
- System tables and fields (Id >= 2000000000) are Microsoft-owned.
- Predefined action category groups (`Category_Process` and friends) get their captions from the platform.
- Empty enum values conventionally have no caption.
- Non-assignable permission sets are never shown in the assignment UI.

## Test notes

- Analysis-view fixtures are gated on 18.0.36 (`PageAnalysisView` requires the net10.0 SDK); the same-module page-extension fixtures are gated on 13.0.
