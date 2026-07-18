---
applyTo: 'src/ALCops.LinterCop/**/BuiltInDateTimeMethod*'
---

# LC0083: BuiltInDateTimeMethod

## Purpose

Detects calls to outdated built-in date/time functions (`Date2DMY`, `Date2DWY`, `DT2Date`, `DT2Time`, `Format(... , 0, '<HOURS24|MINUTES|SECONDS|THOUSANDS>')`) and suggests the modern extension methods on `Date`, `Time`, and `DateTime` values (`.Date()`, `.Time()`, `.Day()`, `.Month()`, `.Year()`, `.DayOfWeek()`, `.WeekNo()`, `.Hour()`, `.Minute()`, `.Second()`, `.Millisecond()`). Ships with `BuiltInDateTimeMethodCodeFixProvider`.

## Diagnostic properties

**LC0083** · Category: Design · Severity: Warning · Enabled: true
Message: `Use the new method {0}.{1} to extract specific parts of date/time values.`
Version gate: `Fall2024OrGreater` (extension methods introduced in BC25) · CodeFix: yes

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Registration | `RegisterOperationAction` on `InvocationExpression` | Analyzer works on invocation operations, not raw syntax |
| Method filter | `MethodKind == BuiltInMethod` | Cheap early exit; user-defined methods are never candidates |
| Replacement selection | Static `switch` in `GetReplacementMethod` keyed by method name (+ arg count where relevant) | Deterministic mapping; no reflection or metadata lookup |
| `Date2DWY(x, 3)` no fix | Returns `null` on purpose | Year part of `Date2DWY` can disagree with `.Year()` in ISO weeks spanning two years — silent skip is safer than incorrect fix |
| Guard order | `IsFieldRefValueAccess` → `IsVariantOrJokerArgument` → replacement | FieldRef guard is a specific case; variant/joker guard is the general fallback; both must run before replacement generation |
| `IsVariantOrJokerArgument` | Skip when type is `Variant` **or** `Joker` | `FieldRef.Value` returns `NavTypeKind.Joker` (AL wildcard), **not** `Variant`. Without the Joker branch the analyzer would emit invalid `.Date()`/`.Time()` suggestions for dynamic values |
| `IsFieldRefValueAccess` | Explicit `.Value`-on-FieldRef guard (belt & suspenders alongside the Joker check) | Defensive redundancy: even if a future SDK version stopped typing `.Value` as `Joker`, this guard still catches the case via the operation shape |
| `IsFieldRefValueAccess` operation shapes | Both `IInvocationExpression` **and** `IFieldAccess` | Current SDK models `FieldRef.Value` as a getter invocation. `IFieldAccess` branch is intentionally defensive against future SDK versions remodelling it as a property/field access — must not be pruned as dead code |
| `IsFieldRefValueAccess` member scope | Only `.Value` (via `IsSameName`) | Other FieldRef members (`.Name`, `.GetFilter()`, `.Number`, …) intentionally flow through the normal analysis path |
| Diagnostic properties bag | `ReplacementMethodName` string only | Minimal state passed to the code fix; syntax reconstruction happens in the code fix itself |

## Architecture

### Analysis flow

1. Skip obsolete symbols and non-invocation operations.
2. Skip non-`BuiltInMethod` invocations and calls with zero arguments.
3. `IsFieldRefValueAccess` — early return when the first argument is `.Value` on a `FieldRef` receiver.
4. `IsVariantOrJokerArgument` — early return when the first argument's static type is `Variant` or `Joker`.
5. `GetReplacementMethod` — map `(methodName, argCount, discriminator)` to a synthetic `InvocationExpressionSyntax`.
6. Report LC0083 with the replacement carried in `ImmutableDictionary` properties for the code fix.

### CodeFix

`BuiltInDateTimeMethodCodeFixProvider` reads `ReplacementMethodName` from the diagnostic properties and rewrites `Outer(<arg>[, extras])` into `<arg>.Replacement()`. `arg` is preserved verbatim from the original source.

## Test coverage

**HasDiagnostic (9 cases):** Date2DMY, Date2DWY, DT2Date, DT2Time, DT2XOnRecordField, FormatHour, FormatMillisecond, FormatMinute, FormatSecond.
**NoDiagnostic (8 cases):** VariantDT2Date, VariantDT2Time, VariantDate2DMY, VariantDate2DWY, VariantFormat, RecRefFieldRefDT2DateTime, FieldRefValueChained, FormatFieldRefValue.
**HasFix (11 cases):** Date2DMY_Day, Date2DMY_Month, Date2DMY_Year, Date2DWY_DayOfWeek, Date2DWY_WeekNo, DT2Date, DT2Time, Format_Hour, Format_Minute, Format_Second, Format_Millisecond.

## Known issues

- **`FieldRef.Value` is not `Variant`, it is `Joker`.** The AL SDK exposes the dynamic result type of `FieldRef.Value` (and the like) as `NavTypeKind.Joker`, not `NavTypeKind.Variant`. A plain Variant check therefore misses these expressions. `IsVariantOrJokerArgument` covers both; `IsFieldRefValueAccess` is a defensive second line for the specific `.Value` case.
- **`Date2DWY(x, 3)` intentionally unfixed.** For week-year splits (ISO week 1/52-53 straddling January), `Date2DWY`'s year output and `x.Year()` can differ. The mapping returns `null` to suppress the fix; the diagnostic is not reported for this combination either.
- **Format reasoning is string-based.** `GetFormatReplacement` matches on the literal format string only. If someone writes `Format(TextVar, 0, '<HOURS24>')`, the analyzer would still propose `TextVar.Hour()` — nonsensical but pre-existing and unrelated to the FieldRef work. No planned change.
