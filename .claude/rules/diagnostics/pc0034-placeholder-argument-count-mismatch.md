---
paths:
  - "src/ALCops.PlatformCop/**/PlaceholderArgumentCountMismatch*"
---

# PC0034: PlaceholderArgumentCountMismatch

## Purpose

Detects mismatches between placeholder count in format strings and the number of substitution arguments passed to `StrSubstNo`, `Error`, `Message`, and `Confirm`. This rule extends CodeCop AA0131 to cover its gaps.

## Design decisions

| Decision | Rationale |
|---|---|
| Text variable support — Bail out (no diagnostic) | Matches AA0131 behavior, avoids false positives |
| TextConst support — Not implemented | Legacy type, NavTypeKind.TextConst not in EnumProvider |
| Confirm default button — args[1] is always Boolean | Substitution args start at index 2 for Confirm |
| Duplicate placeholder counting — HashSet-based (unique) | `%1 appears %1 twice` counts as 1 placeholder |
| Regex pattern — `[#%](\d+)` | Both `%N` and `#N` are valid AL placeholder syntax |

## Architecture

- Registers for `OperationKind.InvocationExpression`
- Matches built-in methods by name (case-insensitive)
- Unwraps ConversionExpression chain to get the format string operand
- Bails out if any unwrapped type is `NavTypeKind.Text` (runtime-determined string)
- Extracts text from `ILabelTypeSymbol.Text` or `ConstantValue` for string literals

## Relationship to CodeCop AA0131

This rule is an **extension of CodeCop AA0131** ([docs](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/analyzers/codecop-aa0131)).

AA0131 has two gaps:
1. **Zero-args gap**: When `args.Length - 1 < 1`, AA0131 exits early without checking placeholder count
2. **No Confirm coverage**: AA0131 only handles `StrSubstNo`, `Error`, `Message`

PC0034 fires when:
- For StrSubstNo/Error/Message: placeholders exist but zero substitution args are passed (gap #1)
- For Confirm: any placeholder/argument mismatch (gap #2)

PC0034 intentionally avoids overlap: for StrSubstNo/Error/Message with `argumentCount >= 1`, AA0131 already handles it.
