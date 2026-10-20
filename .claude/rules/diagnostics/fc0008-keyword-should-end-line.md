---
paths:
  - "src/ALCops.FormattingCop/**/KeywordShouldEndLine*"
  - "src/ALCops.FormattingCop.Test/Rules/KeywordShouldEndLine/**"
---

# FC0008: KeywordShouldEndLine

## Purpose

Reports a `begin` or the `then` of an `if` statement when another token follows it on the same line, so every statement starts on its own line.

Registers `RegisterSyntaxNodeAction` on `IfStatement` and `Block`; the reported token is the keyword itself.

## Design decisions

| Decision | Rationale |
|---|---|
| Severity Warning, enabled by default, category Style | Matches the house convention this rule was ported from. |
| Compare the keyword's line with the next token's line | Comments are trivia, so a trailing `//` or `/* */` comment is allowed without special-casing trivia kinds. |
| Every `Block` is checked (procedure bodies, `else begin`, `case` branches, loops) | The convention applies to all blocks, not only those under `if`. |
| No CodeFix and not configurable | Decided with the rule owner; may be revisited separately. |

## Deliberate non-reports

- `then begin` on one line: the `then` is skipped when its statement is a block. The `begin` is still checked.
- `else begin`: the `begin` ends the line, so it is not reported.
- A trailing line or block comment after the keyword.
- Missing tokens and a keyword at the end of the file.

## Known issues

- A single-line `if x then exit;` is reported on purpose.
- `begin end` on one line is reported on the `begin`.
