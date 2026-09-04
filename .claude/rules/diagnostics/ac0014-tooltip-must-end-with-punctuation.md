---
paths:
  - "src/ALCops.ApplicationCop/**/ToolTipPunctuation*"
  - "src/ALCops.ApplicationCop.Test/Rules/ToolTipMustEndWithPunctuation/**"
---

# AC0014: ToolTipMustEndWithPunctuation

## Purpose

Checks that ToolTip text ends with an allowed punctuation character. The allowed set is configurable through `ToolTipAllowedPunctuations` in `alcops.json`.

Registers `RegisterSyntaxNodeAction` on `PageField`, `PageAction`, `Field` and `PageAnalysisView`; main type `ToolTipPunctuation` (shared with the other ToolTip rules).

## Design decisions

| Decision | Rationale |
|---|---|
| Implemented inside the shared `ToolTipPunctuation` analyzer rather than its own class | One extraction of the ToolTip text serves all ToolTip punctuation and phrasing checks. |
| Allowed punctuation comes from `ToolTipAllowedPunctuations` via `ALCopsSettingsProvider.GetSettings(compilation.FileSystem)` | Makes the set configurable per workspace/app on the existing settings infrastructure. |
| Missing, empty or fully invalid settings fall back to the dot (`.` / `dot`) | Preserves the pre-configuration AC0014 behaviour instead of disabling the check. |
| The message lists the configured punctuation names, not the characters | Gives guidance that matches the user's own configuration. |

## Deliberate non-reports

- Obsolete symbols, and ToolTips that are not a plain label (`LabelPropertyValueSyntax`) are skipped; every other ToolTip on the registered kinds is checked, the only silence being a text that ends in one of the allowed characters.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `ToolTipAllowedPunctuations` | `[{ "Character": ".", "Name": "dot" }]` | The characters a ToolTip may end with; the `Name` values appear in the message. |

## Test notes

- Fixtures with custom settings (an exclamation-mark-only list, an empty list, an invalid entry) run on fixtures created with `TestHelper.CreateConfigWithSettings`.
- ToolTips on table fields require 13.0; those fixtures are version-gated.
