---
paths:
  - "src/ALCops.PlatformCop/**/TemporaryRecordTriggerInvocation*"
---

# PC0027: TemporaryRecordTriggerInvocation

## Purpose

Detects invocations of trigger-executing methods (Insert, Modify, Delete) on temporary record variables, where the triggers have no effect.

## Design decisions

| Decision | Rationale |
|---|---|
| Bare and this self-forms: by-design no diagnostic (#348) | Self-reference forms inside a table/tableextension target the object's own record, which is not a local temporary variable. No realistic use case for triggering this rule on self-calls. Pinned by `test(PC0027)` commit. |

## Known issues

- None specific to receiver forms; by-design verdicts pinned by fixture matrix.
