---
paths:
  - "src/ALCops.PlatformCop/**/RecordGetProcedureArguments*"
---

# PC0013: RecordGetProcedureArguments

## Purpose

Detects `Get` calls where the argument count does not match the primary key field count.

## Design decisions

| Decision | Rationale |
|---|---|
| Receiver forms: bare implicit self fixed (#348) | The analyzer gated on non-null invocation instance; bare `Get()` in a table was missed |

## Known issues

- None specific to receiver forms after #348.
