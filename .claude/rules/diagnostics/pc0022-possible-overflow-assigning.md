---
paths:
  - "src/ALCops.PlatformCop/**/PossibleOverflowAssigning*"
---

# PC0022: PossibleOverflowAssigning

## Purpose

Detects possible data loss when a longer Text/Code value is assigned or passed into a shorter destination — including `Get(...)` arguments checked against the primary-key field lengths.

## Design decisions

| Decision | Rationale |
|---|---|
| Receiver forms: bare implicit self fixed (#348) | The analyzer gated on non-null invocation instance; bare `Get()` in a table was missed |

## Known issues

- None specific to receiver forms after #348.
