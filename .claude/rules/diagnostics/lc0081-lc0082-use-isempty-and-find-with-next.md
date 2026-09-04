---
paths:
  - "src/ALCops.LinterCop/**/AnalyzeCountMethod*"
---

# LC0081 / LC0082: UseIsEmptyMethodInsteadOfCount / UseQueryOrFindWithNextInsteadOfCount

## Purpose

LC0081 flags `Count() = 0` / `Count() > 0` patterns that should use `IsEmpty`. LC0082 flags `Count() > 1` / `Count() = N` patterns that should use `FindSet`+`Next`.

## Design decisions

| Decision | Rationale |
|---|---|
| Receiver forms: bare and this fixed (#348) | Both share the same `AnalyzeCountMethod` analyzer; the invocation-instance null check skipped bare self-calls |

## Known issues

- None specific to receiver forms after #348.
