---
applyTo: 'src/ALCops.LinterCop/**/PageStyleStringLiteral*'
---

# LC0086: PageStyleStringLiteral

## Purpose

Detects string literals that match `PageStyle` enum value names (e.g., `'Unfavorable'`, `'Standard'`, `'Attention'`) and suggests using the `PageStyle` datatype instead. String literals used for page styling lack IntelliSense, are prone to typos, and won't produce compile-time errors if misspelled.

**References:**
- [GitHub Issue #183](https://github.com/ALCops/Analyzers/issues/183) (false positive report)
- [MS Docs: PageStyle Option](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/pagestyle/pagestyle-option)
- [BC.LinterCop LC0086 Wiki](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0086)
- [BC.LinterCop Discussion #805](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/805)

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `LC0086` |
| Category | Design |
| Severity | Warning |
| Enabled by default | true |
| MessageFormat | `Avoid using the string literal '{0}' for page styling. Use the PageStyle datatype instead (PageStyle::{1}).` |
| Version gate | `Fall2024OrGreater` (PageStyle datatype introduced in BC25, 2024 Wave 2) |
| netstandard2.1 | Full support (no net8.0-only APIs used) |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | LinterCop (LC0086) | Reuses same ID as BC.LinterCop for migration compatibility |
| Approach | Scan all string literals, suppress known-safe contexts | "Deny-list" approach; simpler than flow analysis from StyleExpr properties |
| Case sensitivity | `StringComparer.Ordinal` (case-sensitive) | `'STANDARD'` and `'standard'` are data constants, not style values. Only PascalCase matches (e.g., `'Standard'`) are flagged. Fixes issue #183. |
| Dictionary source | Local `Lazy<ImmutableDictionary>` from `Enum.GetNames(typeof(StyleKind))` | Cannot reuse `EnumProvider.StyleKind.CanonicalNames` because that shared dictionary uses `OrdinalIgnoreCase` by design |
| Category | Design | Architectural choice between string literals and typed enums |
| Severity | Warning | Actionable code smell, not a correctness issue |
| Version gate | `Fall2024OrGreater` | PageStyle datatype didn't exist before BC25 |
| Registration | `RegisterSyntaxNodeAction` on `StringLiteralValue` | Syntax-level check, no operation tree needed |
| Caption suppression | Always skip Caption properties | Captions are user-facing text; `Caption = 'Standard'` is legitimate |
| Enum suppression | Skip Enum and EnumValue contexts | Enum names/captions are identifiers, not style expressions |
| Unlocked labels | Skip (no diagnostic) | Unlocked labels are translatable text, not style constants |
| StyleExpr direct | Skip `StyleExpr = 'Standard'` | Already using the string in the correct property; the fix is to change the property value type, not the location |
| Table field writes | Skip `MyRecord.MyField := 'Standard'` | Writing data to a record field, not styling |
| Data-access args | Skip arguments to Record/RecordRef/FieldRef/Query methods | Data operations, not styling |
| Flow analysis (Option C) | Rejected | Tracing `StyleExpr = myVar` -> `myVar := 'Standard'` across triggers/methods has PC0030-level complexity. BC.LinterCop also doesn't do this. |

## Architecture

### Registration strategy

Uses `RegisterSyntaxNodeAction` on `SyntaxKind.StringLiteralValue` to analyze every string literal in the compilation.

### Analysis flow

1. Skip obsolete symbols
2. Skip Caption properties (`IPropertySymbol` with `PropertyKind.Caption`)
3. Skip Enum and EnumValue symbol contexts
4. Extract string value, skip empty strings
5. **Case-sensitive lookup** in StyleKind dictionary (only PascalCase matches)
6. If inside a Label: skip unless `Locked = true`
7. Skip StyleExpr property assignments (`StyleExpressionPropertyValueSyntax`)
8. Skip table field writes (assignment target is `IFieldSymbol` on a Record type)
9. Skip data-access method arguments (instance is Record, RecordRef, FieldRef, Query, etc.)
10. Report diagnostic at the string literal location

### Case-sensitivity detail

The `StyleKindCanonicalNames` dictionary uses `StringComparer.Ordinal` to perform case-sensitive matching. This means:
- `'Standard'` matches (PascalCase, likely a style value)
- `'STANDARD'` does NOT match (all-caps, likely a data constant)
- `'standard'` does NOT match (lowercase, likely a data constant)

This aligns with BC.LinterCop behavior and eliminates false positives on common data patterns like `Label 'STANDARD', Locked = true`.

### StyleKind enum values (11 total)

None, Standard, StandardAccent, Strong, StrongAccent, Attention, AttentionAccent, Favorable, Unfavorable, Ambiguous, Subordinate

The most ambiguous values are None, Standard, and Strong (common English words). Case-sensitive matching ensures only the PascalCase form is flagged.

## Known issues and workarounds

### False positives on rare PascalCase data

A locked label like `Label 'Standard', Locked = true` in a codeunit that genuinely stores data (not a style value) will still trigger the diagnostic. This was discussed in BC.LinterCop discussion #805 and the consensus is these occurrences are rare enough that `#pragma warning disable LC0086` is an acceptable resolution.

### EnumProvider.StyleKind.CanonicalNames is case-insensitive

The shared `EnumProvider.StyleKind.CanonicalNames` uses `StringComparer.OrdinalIgnoreCase` (all `CanonicalNames` dictionaries in EnumProvider follow this convention). The analyzer must NOT use this shared dictionary. Instead, it builds its own case-sensitive dictionary from `Enum.GetNames(typeof(StyleKind))`.

## Suppressions

| Context | Suppressed | Rationale |
|---|---|---|
| Caption properties | Yes | User-facing text, not style values |
| Enum/EnumValue symbols | Yes | Enum identifiers, not style expressions |
| Unlocked labels | Yes | Translatable text, not style constants |
| StyleExpr direct assignment | Yes | Already in the correct property location |
| Table field writes | Yes | Data operations, not styling |
| Data-access method arguments | Yes | Record/RecordRef/FieldRef/Query operations |
| Locked labels | **No** (flagged) | Very likely style constants |
| Text variable assignments | **No** (flagged) | Very likely style variable assignments |
| If/case comparisons | **No** (flagged) | Very likely style comparisons |
| Exit/return statements | **No** (flagged) | Very likely returning style values |

## Test coverage

### HasDiagnostic (4 cases)

| Test case | Scenario |
|---|---|
| Label | `Label 'Unfavorable', Locked = true` in a codeunit |
| Page | `MyFieldStyle := 'Unfavorable'` in a page OnAfterGetRecord trigger |
| IfStatement | `if MyText = 'None' then` in a page procedure |
| ExitStatement | `exit('Standard')` in a codeunit procedure |

### NoDiagnostic (10 cases)

| Test case | Suppression reason |
|---|---|
| AssignToStyleExpr | `StyleExpr = 'Standard'` (direct StyleExpr property) |
| AssignToTableField | Assignment to a table field via global variable |
| AssignToTableFieldLocal | Assignment to a table field via local variable |
| AssignToTableFieldRec | Assignment to a table field via Rec variable |
| Enum | String literal inside an Enum definition |
| Label | Unlocked label `Label 'Unfavorable'` (no Locked = true) |
| LockedLabelLowercase | `Label 'standard', Locked = true` (case-sensitive, lowercase doesn't match) |
| LockedLabelUppercase | `Label 'STANDARD', Locked = true` (case-sensitive, uppercase doesn't match) |
| Page | Caption property on a page field |
| RecordMethodInvocation | String literal as argument to a Record method |

## Differences from BC.LinterCop LC0086

| Aspect | BC.LinterCop | ALCops |
|---|---|---|
| Case sensitivity | `StringComparer.Ordinal` | `StringComparer.Ordinal` (aligned after #183 fix) |
| Caption suppression | Yes | Yes |
| Unlocked label suppression | Yes | Yes |
| StyleExpr suppression | Yes | Yes |
| Table field suppression | No | Yes (additional safety) |
| Data-access method suppression | No | Yes (additional safety) |
| Enum context suppression | No | Yes (additional safety) |

## Phase 2 roadmap (not yet implemented)

- **StyleExpr flow analysis**: Trace from `StyleExpr = myVar` to assignments like `myVar := 'Standard'` in triggers/methods. Complex, PC0030-level effort.
- **CodeFix**: Replace string literal with `Format(PageStyle::Value)` expression.
- **Cross-object tracing**: Detect style values returned from helper codeunits.
