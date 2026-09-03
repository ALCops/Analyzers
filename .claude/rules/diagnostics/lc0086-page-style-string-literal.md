---
paths:
  - "src/ALCops.LinterCop/**/PageStyleStringLiteral*"
---

# LC0086: PageStyleStringLiteral

## Purpose

Detects string literals that match `PageStyle` enum value names (e.g., `'Unfavorable'`, `'Standard'`, `'Attention'`) and suggests using the `PageStyle` datatype instead. String literals used for page styling lack IntelliSense, are prone to typos, and won't produce compile-time errors if misspelled.

**References:**
- [GitHub Issue #183](https://github.com/ALCops/Analyzers/issues/183) (false positive report)
- [MS Docs: PageStyle Option](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/pagestyle/pagestyle-option)
- [BC.LinterCop LC0086 Wiki](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0086)
- [BC.LinterCop Discussion #805](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/805)

## Design decisions

| Decision | Rationale |
|---|---|
| Cop: LinterCop (LC0086) | Reuses same ID as BC.LinterCop for migration compatibility |
| Approach: scan all string literals, suppress known-safe contexts | "Deny-list" approach; simpler than flow analysis from StyleExpr properties |
| Case sensitivity: `StringComparer.Ordinal` (case-sensitive) | `'STANDARD'` and `'standard'` are data constants, not style values. Only PascalCase matches (e.g., `'Standard'`) are flagged. Fixes issue #183. |
| Dictionary source: local `Lazy<ImmutableDictionary>` from `Enum.GetNames(typeof(StyleKind))` | Cannot reuse `EnumProvider.StyleKind.CanonicalNames` because that shared dictionary uses `OrdinalIgnoreCase` by design |
| Category: Design | Architectural choice between string literals and typed enums |
| Severity: Warning | Actionable code smell, not a correctness issue |
| Version gate: `Fall2024OrGreater` | PageStyle datatype didn't exist before BC25 |
| Registration: `RegisterSyntaxNodeAction` on `StringLiteralValue` | Syntax-level check, no operation tree needed |
| Caption suppression: always skip Caption and OptionCaption properties | Caption-like properties are user-facing text; `Caption = 'Standard'` and `OptionCaption = 'None'` are legitimate |
| Enum suppression: skip Enum and EnumValue contexts | Enum names/captions are identifiers, not style expressions |
| Unlocked labels: skip (no diagnostic) | Unlocked labels are translatable text, not style constants |
| StyleExpr direct: skip `StyleExpr = 'Standard'` | Already using the string in the correct property; the fix is to change the property value type, not the location |
| Table field writes: skip `MyRecord.MyField := 'Standard'` | Writing data to a record field, not styling |
| Receiver forms: bare/this field writes suppressed (#348) | The suppression checks `IFieldAccess.Instance` type; bare self-reference (null instance) and `this` both resolve via `GetReceiverTableType` to the record type, correctly triggering the field-write suppression |
| Data-access args: skip arguments to Record/RecordRef/FieldRef/Query methods | Data operations, not styling |
| Flow analysis (Option C): rejected | Tracing `StyleExpr = myVar` -> `myVar := 'Standard'` across triggers/methods has PC0030-level complexity. BC.LinterCop also doesn't do this. |

## Architecture

### Registration strategy

Uses `RegisterSyntaxNodeAction` on `SyntaxKind.StringLiteralValue` to analyze every string literal in the compilation.

### Analysis flow

1. Skip obsolete symbols
2. Skip Caption-like properties (`IPropertySymbol` with `PropertyKind.Caption` or `OptionCaption`)
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

## Suppressions

| Context | Suppressed | Rationale |
|---|---|---|
| Caption/OptionCaption properties | Yes | User-facing text, not style values |
| Enum/EnumValue symbols | Yes | Enum identifiers, not style expressions |
| Unlocked labels | Yes | Translatable text, not style constants |
| StyleExpr direct assignment | Yes | Already in the correct property location |
| Table field writes | Yes | Data operations, not styling |
| Data-access method arguments | Yes | Record/RecordRef/FieldRef/Query operations |
| Locked labels | **No** (flagged) | Very likely style constants |
| Text variable assignments | **No** (flagged) | Very likely style variable assignments |
| If/case comparisons | **No** (flagged) | Very likely style comparisons |
| Exit/return statements | **No** (flagged) | Very likely returning style values |

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

## Known issues

### False positives on rare PascalCase data

A locked label like `Label 'Standard', Locked = true` in a codeunit that genuinely stores data (not a style value) will still trigger the diagnostic. This was discussed in BC.LinterCop discussion #805 and the consensus is these occurrences are rare enough that `#pragma warning disable LC0086` is an acceptable resolution.

### EnumProvider.StyleKind.CanonicalNames is case-insensitive

The shared `EnumProvider.StyleKind.CanonicalNames` uses `StringComparer.OrdinalIgnoreCase` (all `CanonicalNames` dictionaries in EnumProvider follow this convention). The analyzer must NOT use this shared dictionary. Instead, it builds its own case-sensitive dictionary from `Enum.GetNames(typeof(StyleKind))`.

## Roadmap

- **StyleExpr flow analysis**: Trace from `StyleExpr = myVar` to assignments like `myVar := 'Standard'` in triggers/methods. Complex, PC0030-level effort.
- **CodeFix**: Replace string literal with `Format(PageStyle::Value)` expression.
- **Cross-object tracing**: Detect style values returned from helper codeunits.
