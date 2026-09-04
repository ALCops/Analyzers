---
paths:
  - "src/ALCops.PlatformCop/**/TemporaryRecordTriggerInvocation*"
  - "src/ALCops.PlatformCop.Test/Rules/TemporaryRecordTriggerInvocation/**"
---

# PC0027: TemporaryRecordTriggerInvocation

## Purpose

Detects invocations of trigger-executing methods (Insert, Modify, Delete) on temporary record variables, where the triggers have no effect.

Registers `RegisterOperationAction` on `InvocationExpression`; main type `TemporaryRecordTriggerInvocation`.

## Design decisions

| Decision | Rationale |
|---|---|
| Self-reference receiver forms (bare, `this`) are out of scope | Inside a table or tableextension they target the object's own record, which is never a local temporary variable; no realistic use case triggers the rule there |

## Deliberate non-reports

- Bare `Insert(true)` / `this.Insert(true)` inside a table or tableextension: the receiver is the object's own record, not a temporary variable. Pinned by the receiver-form fixture matrix.
