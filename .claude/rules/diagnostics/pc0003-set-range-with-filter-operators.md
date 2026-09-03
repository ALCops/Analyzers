---
paths:
  - "src/ALCops.PlatformCop/**/SetRangeWithFilterOperators*"
---

# PC0003: SetRangeWithFilterOperators

## Purpose

Detects `SetRange` calls that use filter operators (e.g. `*`, `..`) which should use `SetFilter` instead.

## Design decisions

| Decision | Rationale |
|---|---|
| Receiver forms: bare implicit self fixed (#348) | The analyzer gated on non-null `IInvocationExpression.Instance`; bare `SetRange()` in a table was missed |

## Known issues

- None specific to receiver forms after #348.
