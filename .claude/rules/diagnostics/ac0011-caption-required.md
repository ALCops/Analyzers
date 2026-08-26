---
paths:
  - "src/ALCops.ApplicationCop/**/CaptionRequired*"
---

# AC0011: CaptionRequired

## Purpose

Checks that user-facing symbols define a `Caption` (or `CaptionClass`/`CaptionML`) property: pages, tables, table fields, page controls, actions, enum values, permission sets, and analysis views.

## Design decisions

| Decision | Rationale |
|---|---|
| Caption satisfied by `Caption`, `CaptionClass`, or `CaptionML` | Any of the three provides a user-facing caption. |
| `ShowCaption = false` suppresses the check | Explicitly hidden captions need no value. |
| API pages: entire page skipped (`IsInApiPage`) | API pages are not user-facing. No pageextension handling needed: API pages cannot be extended. |
| HeadlinePart field controls skipped (`IsInHeadlinePartPage`), including via pageextension targets | The runtime ignores `Caption` on HeadlinePart field controls; only `Expression`, `Visible`, `ApplicationArea`, `Drilldown`, and `DrillDownPageID` apply ([docs](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-create-role-center-headline#in-development)). Page object, actions, and groups in HeadlinePart pages remain checked. See issue #293. |
| Field controls fall back to `RelatedFieldSymbol` caption | A page field without a caption inherits the source table field's caption. |
| Part controls fall back to `RelatedPartSymbol` caption | Same inheritance principle for page parts. |
| Area/Grid/Repeater/UserControl/SystemPart controls skipped | No user-facing caption requirement. |
| System tables/fields (Id >= 2000000000) skipped | System objects are Microsoft-owned. |
| Predefined action category groups skipped | Names like `Category_Process` get captions from the platform. |
| Promoted SplitButton groups checked only when containing repeater-scoped actionrefs | Only case where the runtime displays the group caption. |
| Empty enum values skipped | Blank enum values conventionally have no caption. |
| Non-assignable permission sets skipped | Not shown in the UI for assignment. |

## Architecture

Single `RegisterSymbolAction` over Page, Query, Table, Field, Action, EnumValue, Control, PermissionSet, and AnalysisView symbol kinds. Per-symbol dispatch on symbol kind, then control kind/action kind.

`IsInHeadlinePartPage` resolves the containing object via `GetContainingObjectTypeSymbol()`; for pageextensions it resolves `IApplicationObjectExtensionTypeSymbol.Target?.OriginalDefinition as IPageBaseTypeSymbol` (same pattern as `PermissionResolver`), then compares `PageType` to `EnumProvider.PageTypeKind.HeadlinePart`.
