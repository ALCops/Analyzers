---
paths:
  - "src/ALCops.PlatformCop/**/DuplicateODataEntityName*"
  - "src/ALCops.PlatformCop.Test/Rules/DuplicateODataEntityName/**"
---

# PC0033: DuplicateODataEntityName

## Purpose

Detects page controls that produce duplicate OData EntityNames after the EDMX name transformation. For example, `"PTE No."` and `"PTE No"` both transform to `PTE_No`, causing a runtime error when users use "Edit in Excel" or any OData integration. The AL compiler has similar checks (AL0757/AL0758/AL0678) but they use a different name mangling (`MangleUnquotedIdentifierName()` which maps `.` → `a46`), NOT the OData/EDMX transformation, so those checks don't catch OData-specific collisions.

Registers `RegisterSymbolAction` on `Page` and `PageExtension`; main type `DuplicateODataEntityName`.

**References:**
- [GitHub Discussion #119](https://github.com/ALCops/Analyzers/discussions/119)
- [MS Docs: EDMX Metadata](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/webservices/return-obtain-service-metadata-edmx-document)
- [MS Docs: Edit in Excel](https://learn.microsoft.com/en-us/dynamics365/business-central/across-work-with-excel)

## Design decisions

| Decision | Rationale |
|---|---|
| PlatformCop | Platform runtime behaviour (OData/EDMX transformation, Edit in Excel) |
| Severity Warning | Causes runtime errors in OData integrations and Edit in Excel |
| Page types Card, Document, List, ListPart, ListPlus, Worksheet only | These support Edit in Excel / OData; API pages have their own naming rules (AL0528) and RoleCenter, ConfirmationDialog, NavigatePage etc. do not expose OData |
| PageExtensions are analyzed against the base page and all sibling extensions, but diagnostics are reported only on extension-added controls | Extension fields participate in OData, yet the developer can only fix their own code |
| Primary-key fields join the uniqueness check | EDMX docs: PK fields are auto-added as OData properties |
| Case-insensitive comparison of OData names | OData property names are case-insensitive per the OData spec |
| Name transformation delegated via reflection to the SDK's `NameTransformations.MangleIntoValidXmlIdentifier` (`ODataNameHelper` in ALCops.Common) rather than reimplemented | The transformation has many edge cases (underscore dedup, trailing trim, Subform→Line, `XmlConvert` encoding); the SDK method is what the platform runs |
| When the SDK method is unavailable the analyzer exits early with no diagnostics | Older SDKs lack the method; silent degradation instead of errors |
| No CodeFix | Auto-renaming controls is complex and could break existing integrations |

## Deliberate non-reports

- Non-Field controls (Group, Area, Part): only Field controls produce OData properties.
- API pages and page types that do not expose OData.
- Query objects: they already restrict special characters.
- Table fields with `AllowInCustomizations = Always`: deferred, not part of the check.
- Obsolete symbols (standard ALCops convention).
- Base-page controls when analyzing a PageExtension: only extension-added controls are reported.

## Known issues

- On SDKs without `MangleIntoValidXmlIdentifier` the rule produces nothing at all.
- Collisions caused by space→underscore are also caught by the compiler's AL0757 (MetadataName maps spaces the same way); the redundant warning is acceptable.

## SDK facts

- `Microsoft.Dynamics.Nav.AL.Common.NameTransformations.MangleIntoValidXmlIdentifier` is the OData/EDMX transform: space, `.`, `()`, `/`, `-`, `:`, `@`, `\` and `"` become `_`, consecutive underscores are deduplicated, trailing underscores trimmed, `%` becomes `Percent`, a `Subform` suffix becomes `Line`, and remaining characters go through `XmlConvert.EncodeName` (`'` → `_x0027_`).
- The compiler's `MangleUnquotedIdentifierName()` (behind AL0757/AL0758) maps `.` → `a46`, `(` → `a40`, `/` → `a47`, so `"PTE No."` and `"PTE No"` have distinct MetadataNames but the same OData name.

## Test notes

- Fixtures must not rely on names that differ only by space versus underscore (`"PTE No"` vs `PTE_No`): AL0757 makes them a compile error before this rule runs.
