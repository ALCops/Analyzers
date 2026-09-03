---
paths:
  - "src/ALCops.ApplicationCop/**/UseReturnValueForDatabaseReadMethods*"
---

# AC0030: UseReturnValueForDatabaseReadMethods

## Purpose

Detects database read method calls (Find, FindFirst, FindLast, FindSet, Get, GetBySystemId) whose boolean return value is discarded.

## Design decisions

| Decision | Rationale |
|---|---|
| Receiver forms: all four handled via `GetReceiverTableType` (#348) | Phase-0 helper adoption fixed the bare-call gap; no form-specific logic needed |

## Known issues

- None specific to receiver forms after #348.
