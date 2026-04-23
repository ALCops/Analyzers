---
applyTo: 'src/ALCops.PlatformCop/**/DuplicateODataEntityName*'
---

# PC0033: DuplicateODataEntityName

## Purpose

Detects page controls that produce duplicate OData EntityNames after the EDMX name transformation. For example, `"PTE No."` and `"PTE No"` both transform to `PTE_No`, causing a runtime error when users use "Edit in Excel" or any OData integration. The AL compiler has similar checks (AL0757/AL0758/AL0678) but they use a different name mangling (`MangleUnquotedIdentifierName()` which maps `.` → `a46`), NOT the OData/EDMX transformation, so those checks don't catch OData-specific collisions.

**References:**
- [GitHub Discussion #119](https://github.com/ALCops/Analyzers/discussions/119)
- [MS Docs: EDMX Metadata](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/webservices/return-obtain-service-metadata-edmx-document)
- [MS Docs: Edit in Excel](https://learn.microsoft.com/en-us/dynamics365/business-central/across-work-with-excel)

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `PC0033` |
| Category | Design |
| Severity | Warning |
| Enabled by default | true |
| MessageFormat | `Control '{0}' has a duplicate OData EntityName '{1}'. This will cause a runtime error when using 'Edit in Excel'.` |
| Version gate | None |
| netstandard2.1 | Full support (no net8.0-only APIs used) |
| OData transformation | Via SDK's `MangleIntoValidXmlIdentifier` accessed through reflection (`ODataNameHelper` in ALCops.Common) |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | PlatformCop (PC0033) | Platform runtime behavior (OData/EDMX transformation, Edit in Excel) |
| Severity | Warning | Causes runtime errors in OData integrations and Edit in Excel |
| Page types checked | Card, Document, List, ListPart, ListPlus, Worksheet | These page types support Edit in Excel / OData |
| Page types excluded | API, RoleCenter, ConfirmationDialog, NavigatePage, etc. | API pages have separate naming rules (AL0528); others don't expose OData |
| Object types | Page + PageExtension | PageExtensions add fields that participate in OData |
| PageExtension reporting | Only on extension-added controls | Developer can only fix their own code, not the base page |
| Primary keys | Included in uniqueness check | EDMX docs: PK fields are auto-added as OData properties |
| AllowInCustomizations | Skipped for v1 | Added complexity; deferred to Phase 2 |
| Query objects | Not checked | Query objects restrict special characters already |
| OData name comparison | Case-insensitive | OData property names are case-insensitive per OData spec |
| Control filtering | `ControlKind.Field` only | Only Field controls produce OData properties; Group/Area/Part are structural |
| Registration | `RegisterSymbolAction` on Page + PageExtension | Single-pass per symbol; sibling extension lookup via `ConditionalWeakTable` cache |
| Sibling extension detection | `ConditionalWeakTable<Compilation, Lazy<ImmutableArray<IPageExtensionBaseTypeSymbol>>>` | Lazily caches all page extensions per compilation; same pattern as `TransferFieldsSchemaCompatibility` for table extensions |
| Extension target matching | `SameApplicationObject()` via `OriginalDefinition` + ID comparison | Handles cross-module symbols where reference equality fails |
| CodeFix | None for v1 | Auto-renaming controls is complex and could break existing integrations |
| Skip obsolete | Yes | Standard ALCops convention |
| OData transformation location | `ODataNameHelper` in `ALCops.Common/Helpers/` via reflection to SDK's `MangleIntoValidXmlIdentifier` | Authoritative SDK method eliminates maintenance burden; graceful fallback if SDK method unavailable (older versions) |
| SDK method unavailable | Analyzer exits early (no diagnostics) | Older SDKs without `MangleIntoValidXmlIdentifier` silently skip; no errors |

## OData/EDMX Name Transformation

The OData property name transformation is performed by the SDK's `NameTransformations.MangleIntoValidXmlIdentifier()` method in `Microsoft.Dynamics.Nav.AL.Common`. The analyzer accesses this method via reflection through `ODataNameHelper` in `ALCops.Common/Helpers/`.

### Transformation rules (from SDK)

| Character | Transformation | Example |
|---|---|---|
| Space (` `) | `_` (underscore) | `"PTE No"` → `PTE_No` |
| Dot (`.`) | `_` (underscore) | `"No."` → `No_` → `No` (trailing trim) |
| Parentheses `()` | `_` (underscore) | `"Balance (LCY)"` → `Balance__LCY_` → `Balance_LCY` (dedup + trim) |
| Slash (`/`) | `_` (underscore) | `"Country/Region"` → `Country_Region` |
| Hyphen (`-`) | `_` (underscore) | `"Line-No"` → `Line_No` |
| Colon (`:`) | `_` (underscore) | `"Type:Code"` → `Type_Code` |
| At (`@`) | `_` (underscore) | `"Email@Work"` → `Email_Work` |
| Backslash (`\`) | `_` (underscore) | `"Path\Name"` → `Path_Name` |
| Double quote (`"`) | `_` (underscore) | Quotes in name → underscore |
| Percent (`%`) | `Percent` | `"Tax%"` → `TaxPercent` |
| Consecutive `_` | Deduplicated | `"A__B"` → `A_B` |
| Trailing `_` | Trimmed | `"Name_"` → `Name` |
| "Subform" suffix | Replaced with "Line" | `"SalesSubform"` → `"SalesLine"` |
| Other special chars | Via `XmlConvert.EncodeName` | `"O'Brien"` → `O_x0027_Brien` |

### Why we use SDK reflection (not custom implementation)

The transformation has many edge cases (consecutive underscore deduplication, trailing trim, Subform replacement, XmlConvert encoding). Maintaining a custom reimplementation is fragile and error-prone. The SDK's method is the authoritative source that matches what the BC platform actually does at runtime.

### SDK MetadataName vs OData (critical difference)

The SDK's `MangleUnquotedIdentifierName()` (used by AL0757/AL0758) maps characters differently:
- `.` → `a46` (not removed)
- `(` → `a40` (not removed)
- `/` → `a47` (not `_`)

So `"PTE No."` has MetadataName `PTE_Noa46` (unique from `"PTE No"` = `PTE_No`), but OData name `PTE_No` (duplicate). This is why the compiler's checks don't catch OData-specific collisions.

## Architecture

### Registration strategy

Uses `RegisterSymbolAction` on `Page` and `PageExtension` symbol kinds. Page extension analysis uses a `ConditionalWeakTable`-based cache to lazily gather all page extensions per compilation, enabling sibling extension collision detection.

### Analysis flow

**For Pages (`IPageBaseTypeSymbol`):**
1. Skip if `ODataNameHelper.IsAvailable` is false (SDK method unavailable)
2. Skip obsolete symbols
3. Check `PageType` against `RelevantPageTypes` set
4. Collect field controls from `FlattenedControls` (filter to `ControlKind.Field`)
5. Collect PK fields from `RelatedTable.PrimaryKey.Fields`
6. Transform all names via `ODataNameHelper.MangleIntoValidXmlIdentifier()`
7. Group by OData name (case-insensitive), report all members of groups with count > 1

**For PageExtensions (`IPageExtensionBaseTypeSymbol`):**
1. Skip obsolete symbols
2. Resolve target page via `Target.OriginalDefinition`, check `PageType`
3. Collect extension's own controls from `AddedControlsFlattened`
4. Collect base page controls from `FlattenedControls`
5. Collect PK fields from target page's `RelatedTable`
6. Collect controls from sibling page extensions (other extensions targeting the same base page) via `ConditionalWeakTable` cache
7. Combined duplicate check across all entries
8. Only report diagnostics on extension-added controls (filter via `extensionControlSet`)

### Sibling extension detection

Uses the `ConditionalWeakTable<Compilation, PageExtensionsCacheEntry>` pattern (same as `TransferFieldsSchemaCompatibility` for table extensions):
1. `GetCachedPageExtensions(compilation)` lazily loads all `IPageExtensionBaseTypeSymbol` via `GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection`
2. Filters to siblings targeting the same base page using `SameApplicationObject()` (ID + Kind comparison via `OriginalDefinition`)
3. Sibling controls are added with `Control = null` so they are not reportable (only the current extension's controls are reported)

### Key SDK interfaces

- `IPageBaseTypeSymbol`: `PageType`, `RelatedTable`, `FlattenedControls`
- `IPageExtensionBaseTypeSymbol`: `AddedControlsFlattened`, `Target`
- `IControlSymbol`: `ControlKind`, `Name`, `GetLocation()`
- `ITableTypeSymbol`: `PrimaryKey` → `IKeySymbol` → `Fields`

### Data structure

```
ODataNameEntry (record struct, #if guarded):
  - ODataName: string (transformed name)
  - OriginalName: string (source name)
  - Location: Location (for diagnostic reporting)
  - Control: IControlSymbol? (null for PK entries)
```

## Known issues and workarounds

### Space→underscore collisions already caught by AL0757

The SDK's MetadataName transformation maps spaces to underscores, same as OData. So `"PTE No"` and `PTE_No` both have MetadataName `PTE_No`, triggering AL0757. Our rule would also flag these, but the compiler catches them first. This is not a problem (redundant warnings are acceptable), but test fixtures must avoid this pattern to prevent compilation errors.

### OData name collisions not caught by compiler

Our unique value is in catching collisions caused by the OData/EDMX transformation (SDK's `MangleIntoValidXmlIdentifier`), which differs from the compiler's MetadataName mangling (`MangleUnquotedIdentifierName`):
- Dot, hyphen, colon, at, backslash, parentheses → `_` (with consecutive dedup + trailing trim)
- Percent → `Percent`
- Subform → Line

These transformations differ from the MetadataName mangling (where `.` → `a46`, `(` → `a40`), so the compiler's AL0757/AL0758 checks miss them.

### SDK method unavailability

If the SDK's `MangleIntoValidXmlIdentifier` method is not found (older BC SDK versions that don't have `Microsoft.Dynamics.Nav.AL.Common.NameTransformations`), the analyzer silently produces no diagnostics. This is by design: the `ODataNameHelper.IsAvailable` check at the top of `AnalyzeSymbol` returns early.

## Test coverage

### HasDiagnostic (8 cases)

| Test case | Scenario |
|---|---|
| DotRemoval | `"PTE No."` and `"PTE No"` both become `PTE_No` |
| PercentSign | `"Tax%"` and `TaxPercent` both become `TaxPercent` |
| ParenthesisRemoval | `"Balance (LCY)"` and `Balance_LCY` both become `Balance_LCY` |
| SlashToUnderscore | `"Country/Region"` and `Country_Region` both become `Country_Region` |
| PageExtensionCollision | Extension control `"Item No"` collides with base page `"Item No."` |
| PrimaryKeyCollision | Control `Primary_Key` collides with PK field `"Primary Key"` |
| ThreeWayCollision | Three controls with different special chars (`"A B"`, `"A.B"`, `"A-B"`) all producing `A_B` |
| MultiplePageExtensionCollision | Two page extensions each adding a control that collides (`"PTE No"` and `"PTE No."` both → `PTE_No`) |

### NoDiagnostic (5 cases)

| Test case | Suppression reason |
|---|---|
| UniqueNames | All controls have distinct OData names |
| ApiPage | API page type (excluded from check) |
| RoleCenterPage | RoleCenter page type (excluded) |
| ObsoletePage | Page with `ObsoleteState = Pending` (skipped) |
| PageExtensionUniqueNames | Extension controls have unique names relative to base |

## Phase 2 roadmap (not yet implemented)

- **AllowInCustomizations**: Check table fields with `AllowInCustomizations = Always` for potential collisions with page controls
- **CodeFix**: Suggest renaming controls to avoid collision
- **Query objects**: Evaluate if needed despite restricted characters
- **Apostrophe test case**: Add test for `'` → `_x0027_` transformation
