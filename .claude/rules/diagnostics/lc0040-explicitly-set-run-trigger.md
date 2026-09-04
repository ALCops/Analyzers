---
paths:
  - "src/ALCops.LinterCop/**/ExplicitlySetRunTrigger*"
---

# LC0040: ExplicitlySetRunTrigger

## Purpose

Detects Insert/Modify/Delete/DeleteAll calls where the RunTrigger parameter is not explicitly set.

## Design decisions

| Decision | Rationale |
|---|---|
| Receiver forms: bare and this fixed (#348) | Both were missed because the analyzer only checked `MemberAccessExpressionSyntax` receivers |

## Known issues

- None specific to receiver forms after #348.
