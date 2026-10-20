---
paths:
  - "src/ALCops.DocumentationCop/**/PragmaDisableRequiresComment*"
  - "src/ALCops.DocumentationCop.Test/Rules/PragmaDisableRequiresComment/**"
---

# DC0011: PragmaDisableRequiresComment

## Purpose

Reports a `#pragma warning disable` directive that has no comment explaining why the warning is disabled, so suppressions stay reviewable.

Registers `RegisterSyntaxTreeAction`; walks the directives of the tree and checks the comment trivia around each `disable` pragma.

## Design decisions

| Decision | Rationale |
|---|---|
| Severity Info, enabled by default, category Style | Agreed with the rule owner; a missing comment is advice, not a defect. |
| Accept a `//` comment on the pragma line or a comment on the line directly above | Both are common ways to justify a suppression; requiring only the trailing form would flag the above-the-line style. |
| The comment above must stand alone on its line | A trailing comment on the preceding code line documents that code, not the pragma. |
| Block comments (`/* */`) count when placed above the pragma | The lexer only produces comment trivia for `/* */` outside directives (see Known issues). |
| An empty comment (`//` or `/* */`) does not count | A comment must carry text to explain anything. |
| `SyntaxTreeAction` instead of a node action | Directives are trivia, not nodes; the callback is self-contained per file. |
| No CodeFix, not configurable | Decided with the rule owner. |

## Deliberate non-reports

- `#pragma warning restore`: only `disable` suppresses a warning.

## Known issues

- Within a pragma line, a `/* */` comment is lexed as skipped tokens, not comment trivia, so only a trailing `//` comment is recognised there.
- With stacked pragmas, the comment above justifies only the first one; the following `disable` pragmas are reported.
