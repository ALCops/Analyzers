---
paths:
  - "src/ALCops.LinterCop/**/PageStyleStringLiteral*"
  - "src/ALCops.LinterCop.Test/Rules/PageStyleStringLiteral/**"
---

# LC0086: PageStyleStringLiteral

## Purpose

Detects string literals that match `PageStyle` enum value names (e.g., `'Unfavorable'`, `'Standard'`, `'Attention'`) and suggests using the `PageStyle` datatype instead. String literals used for page styling lack IntelliSense, are prone to typos, and won't produce compile-time errors if misspelled.

Registers `SyntaxNodeAction` on `SyntaxKind.StringLiteralValue`; main type `PageStyleStringLiteral`.

**References:**
- [GitHub Issue #183](https://github.com/ALCops/Analyzers/issues/183) (false positive report)
- [MS Docs: PageStyle Option](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/pagestyle/pagestyle-option)
- [BC.LinterCop LC0086 Wiki](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0086)
- [BC.LinterCop Discussion #805](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/805)

## Design decisions

| Decision | Rationale |
|---|---|
| Same ID as BC.LinterCop; Category Design, Severity Warning | Migration compatibility; an actionable architectural smell (string vs typed enum), not a correctness issue. |
| Deny-list approach: scan every string literal and suppress known-safe contexts, instead of flow analysis from `StyleExpr` | Tracing `StyleExpr = myVar` back to `myVar := 'Standard'` across triggers and methods has PC0030-level complexity; BC.LinterCop does not do it either. |
| Case-sensitive (`StringComparer.Ordinal`) matching: only PascalCase spellings are flagged | `'STANDARD'` and `'standard'` are data constants, not style values ([#183](https://github.com/ALCops/Analyzers/issues/183)); the most ambiguous values (`None`, `Standard`, `Strong`) are common English words. |
| Local `Lazy<ImmutableDictionary>` built from `Enum.GetNames(typeof(StyleKind))` instead of `EnumProvider.StyleKind.CanonicalNames` | Every `CanonicalNames` dictionary in `EnumProvider` is `OrdinalIgnoreCase` by design and must not be used here. |
| Version gate `Fall2024OrGreater` | The `PageStyle` datatype did not exist before BC25. |
| Data-access receiver resolution: `GetSymbolInfo` fast path, `GetOperation(receiver)?.Type` fallback only when nothing resolves and the receiver is not a plain identifier | Keeps the ~300us `GetOperation` off the common named-variable path while still binding `this` receivers on AL 14.0-14.1 (see SDK facts). |

## Deliberate non-reports

- `Caption` and `OptionCaption` property values: user-facing text (`Caption = 'Standard'`, `OptionCaption = 'None'`).
- Literals inside Enum and EnumValue symbols: identifiers and captions, not style expressions.
- Unlocked labels: translatable text, not style constants (locked labels are flagged).
- Direct `StyleExpr = 'Standard'`: the fix there is changing the property value type, not the literal's location.
- Assignments to table fields (`MyRecord.MyField := 'Standard'`, including bare and `this` receivers): data writes, not styling.
- Arguments to Record/RecordRef/FieldRef/Query methods: data operations.
- Non-PascalCase spellings (`'STANDARD'`, `'standard'`) and obsolete symbols.

## Known issues

- A locked label like `Label 'Standard', Locked = true` that genuinely stores data is still flagged. Per BC.LinterCop discussion #805 these are rare enough that `#pragma warning disable LC0086` is the accepted resolution.

## SDK facts

- `GetSymbolInfo` on a `this` receiver returns no symbol before AL 14.2 (`BoundThisReference` gained its `ExpressionSymbol => Type` override in 14.2.19, verified by diffing v14.1.18.1238..v14.2.19.4832); see `.claude/rules/record-receiver-forms.md`. The member-name resolution in `IsWritingToTableField` binds through the field-access node, not `BoundThisReference`, so it needs no fallback.

## Test notes

- Fixtures are version-gated with `SkipTestIfVersionIsTooLow` at `14.0` (no `PageStyle` datatype below that).
