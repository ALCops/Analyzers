---
paths:
  - "src/ALCops.DocumentationCop/**/WriteToFlowFieldRequiresComment*"
---

# DC0002: WriteToFlowFieldRequiresComment

## Purpose

Detects `Validate` calls on FlowFields without a preceding comment explaining why.

## Design decisions

| Decision | Rationale |
|---|---|
| Receiver forms: bare implicit self fixed (#348) | The analyzer previously gated on non-null invocation instance; bare `Validate()` in a table was missed |

## Known issues

- None specific to receiver forms after #348.
