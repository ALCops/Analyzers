---
applyTo: 'src/ALCops.ApplicationCop/**/CaptionRequired*'
---

# AC0011: CaptionRequired

## Purpose

Checks that user-facing symbols define a `Caption` (or `CaptionClass`/`CaptionML`) property: pages, tables, table fields, page controls, actions, enum values, permission sets, and analysis views.

## Diagnostic properties

**AC0011** · Category: Design · Severity: Warning · Enabled: true
Message: `Caption is missing.`
No version gate · Full netstandard2.1 support

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Caption satisfied by | `Caption`, `CaptionClass`, or `CaptionML` | Any of the three provides a user-facing caption. |
| `ShowCaption = false` | Suppresses the check | Explicitly hidden captions need no value. |
| API pages | Entire page skipped (`IsInApiPage`) | API pages are not user-facing. No pageextension handling needed: API pages cannot be extended. |
| HeadlinePart field controls | Skipped (`IsInHeadlinePartPage`), including via pageextension targets | The runtime ignores `Caption` on HeadlinePart field controls; only `Expression`, `Visible`, `ApplicationArea`, `Drilldown`, and `DrillDownPageID` apply ([docs](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-create-role-center-headline#in-development)). Page object, actions, and groups in HeadlinePart pages remain checked. See issue #293. |
| Field controls | Fall back to `RelatedFieldSymbol` caption | A page field without a caption inherits the source table field's caption. |
| Part controls | Fall back to `RelatedPartSymbol` caption | Same inheritance principle for page parts. |
| Area/Grid/Repeater/UserControl/SystemPart controls | Skipped | No user-facing caption requirement. |
| System tables/fields (Id >= 2000000000) | Skipped | System objects are Microsoft-owned. |
| Predefined action category groups | Skipped | Names like `Category_Process` get captions from the platform. |
| Promoted SplitButton groups | Checked only when containing repeater-scoped actionrefs | Only case where the runtime displays the group caption. |
| Empty enum values | Skipped | Blank enum values conventionally have no caption. |
| Non-assignable permission sets | Skipped | Not shown in the UI for assignment. |

## Architecture

Single `RegisterSymbolAction` over Page, Query, Table, Field, Action, EnumValue, Control, PermissionSet, and AnalysisView symbol kinds. Per-symbol dispatch on symbol kind, then control kind/action kind.

`IsInHeadlinePartPage` resolves the containing object via `GetContainingObjectTypeSymbol()`; for pageextensions it resolves `IApplicationObjectExtensionTypeSymbol.Target?.OriginalDefinition as IPageBaseTypeSymbol` (same pattern as `PermissionResolver`), then compares `PageType` to `EnumProvider.PageTypeKind.HeadlinePart`.

## Test coverage

**HasDiagnostic (5 cases):** EnumObject, HeadlinePartPage, PageObject, PageAnalysisView, TableObject.
**NoDiagnostic (7 cases):** ApiPage, EnumObject, HeadlinePartPage, HeadlinePartPageExtension, PageObject, PageAnalysisView, TableObject.

PageAnalysisView cases require the net10.0 SDK (skipped below version 18.0.36).
