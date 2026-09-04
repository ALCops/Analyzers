---
paths:
  - "src/ALCops.PlatformCop/**/PlaceholderArgumentCountMismatch*"
  - "src/ALCops.PlatformCop.Test/Rules/PlaceholderArgumentCountMismatch/**"
---

# PC0034: PlaceholderArgumentCountMismatch

## Purpose

Detects mismatches between placeholder count in format strings and the number of substitution arguments passed to `StrSubstNo`, `Error`, `Message`, and `Confirm`. This rule extends CodeCop AA0131 to cover its gaps.

Registers `RegisterOperationAction` on `InvocationExpression`; main type `PlaceholderArgumentCountMismatch`.

**References:**
- [CodeCop AA0131](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/analyzers/codecop-aa0131)

## Design decisions

| Decision | Rationale |
|---|---|
| Fires only in AA0131's gaps: `StrSubstNo`/`Error`/`Message` with placeholders but zero substitution arguments, and every mismatch on `Confirm` | AA0131 exits early when `args.Length - 1 < 1` and does not cover `Confirm`; with one or more arguments AA0131 already reports, so overlapping would double-warn |
| Bail out when the format string is a `Text` variable | Runtime-determined string; matches AA0131 and avoids false positives |
| Duplicate placeholders count once (HashSet of unique numbers) | `%1 appears %1 twice` counts as one placeholder |
| Both `%N` and `#N` are placeholders (`[#%](\d+)`) | Both forms are valid AL placeholder syntax |

## Deliberate non-reports

- `StrSubstNo`/`Error`/`Message` calls with one or more substitution arguments: AA0131 territory.
- Format strings held in `Text` variables.
- `TextConst` format strings: legacy type, `NavTypeKind.TextConst` is not exposed by `EnumProvider`.

## Known issues

- `TextConst` support is not implemented (see non-reports).
