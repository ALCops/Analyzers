---
paths:
  - "src/ALCops.LinterCop/**/BuiltInDateTimeMethod*"
  - "src/ALCops.LinterCop.Test/Rules/BuiltInDateTimeMethod/**"
---

# LC0083: BuiltInDateTimeMethod

## Purpose

Detects calls to outdated built-in date/time functions (`Date2DMY`, `Date2DWY`, `DT2Date`, `DT2Time`, `Format(... , 0, '<HOURS24|MINUTES|SECONDS|THOUSANDS>')`) and suggests the modern extension methods on `Date`, `Time`, and `DateTime` values (`.Date()`, `.Time()`, `.Day()`, `.Month()`, `.Year()`, `.DayOfWeek()`, `.WeekNo()`, `.Hour()`, `.Minute()`, `.Second()`, `.Millisecond()`). Ships with `BuiltInDateTimeMethodCodeFixProvider`.

Registers `OperationAction` on `OperationKind.InvocationExpression`; main type `BuiltInDateTimeMethod`.

## Design decisions

| Decision | Rationale |
|---|---|
| Version gate `Fall2024OrGreater` | The extension methods were introduced in BC25. |
| Explicit `IsFieldRefValueAccess` guard kept alongside the `Variant`/`Joker` type check, matching both `IInvocationExpression` and `IFieldAccess` shapes | Defensive redundancy: if a future SDK stops typing `FieldRef.Value` as `Joker` or remodels it as a property access, the shape-based guard still catches it. The `IFieldAccess` branch must not be pruned as dead code. |
| Only `.Value` is guarded among `FieldRef` members | `.Name`, `.GetFilter()`, `.Number` and the rest intentionally flow through the normal analysis path. |
| Diagnostic properties carry only `ReplacementMethodName` | Minimal state handed to the CodeFix; syntax reconstruction happens in the fix itself. |

## Deliberate non-reports

- User-defined methods: only `MethodKind == BuiltInMethod` invocations are candidates.
- `Date2DWY(x, 3)`: the year part can disagree with `x.Year()` in ISO weeks straddling January, so neither a diagnostic nor a fix is produced.
- Calls whose first argument is statically typed `Variant` or `Joker` (including `FieldRef.Value`): a `.Date()`/`.Time()` suggestion on a dynamic value would be invalid.
- Obsolete symbols (standard ALCops convention).

## Known issues

- `Format` reasoning is string-based: `Format(TextVar, 0, '<HOURS24>')` would still be offered `TextVar.Hour()`. Pre-existing and nonsensical only for misuse; no planned change.

## SDK facts

- `FieldRef.Value` (and similar dynamic accessors) has static type `NavTypeKind.Joker`, not `NavTypeKind.Variant`; a plain Variant check misses it.
- The current SDK models `FieldRef.Value` as a getter `IInvocationExpression`, not as an `IFieldAccess`.

## CodeFix: BuiltInDateTimeMethodCodeFixProvider

| Decision | Rationale |
|---|---|
| Rewrites `Outer(<arg>[, extras])` into `<arg>.Replacement()` using the `ReplacementMethodName` property, keeping `<arg>` verbatim | The analyzer already decided the replacement; preserving the argument text avoids reformatting user code. |
