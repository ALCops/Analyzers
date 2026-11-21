---
paths:
  - "src/ALCops.ApplicationCop/**/ToolTipPunctuation*"
  - "src/ALCops.ApplicationCop.Test/Rules/ToolTipMustEndWithPunctuation/**"
  - "src/ALCops.ApplicationCop.Test/Rules/ToolTipShouldStartWithSpecifies/**"
  - "src/ALCops.ApplicationCop.Test/Rules/ToolTipDoNotUseLineBreaks/**"
  - "src/ALCops.ApplicationCop.Test/Rules/ToolTipMaximumLength/**"
---

# AC0014: ToolTipMustEndWithPunctuation

## Purpose

Checks that ToolTip text ends with an allowed punctuation character. The allowed set is configurable through `ToolTipAllowedPunctuations` in `alcops.json`.

Registers syntax-node actions from CompilationStart on `PageField`, `PageAction`, `Field`, `PageAnalysisView`, `PageObject` and `ReportObject`; main type `ToolTipPunctuation` (shared with AC0015, AC0016 and AC0017, which therefore cover the same kinds).

## Design decisions

| Decision | Rationale |
|---|---|
| Implemented inside the shared `ToolTipPunctuation` analyzer rather than its own class | One extraction of the ToolTip text serves all ToolTip punctuation and phrasing checks. |
| Allowed punctuation uses the settings snapshot for the compilation captured at CompilationStart, with the current callback's cancellation token | Keeps every rule and CM0001 on one snapshot despite the SDK's different SemanticModel.Compilation object; interrupted HTTP loads stop promptly. |
| Missing, empty or fully invalid settings fall back to the dot (`.` / `dot`) | Preserves the pre-configuration AC0014 behaviour instead of disabling the check. |
| The message lists the configured punctuation names, not the characters | Gives guidance that matches the user's own configuration. |
| Object-level ToolTips on pages and reports are checked by all four rules | Microsoft's ToolTip guideline makes no exception for objects, and the object ToolTip is user-facing text like any other. |
| AC0017 keeps the 200-character limit for object ToolTips | In the Base App the 241 object-level ToolTips are all on reports; 74 of them exceed 200 characters, 5 lack a final dot and none contains a line break. The limit stays because the guideline does not exempt objects, accepting that Base-App-style report descriptions get flagged. |
| Page and report extensions are not registered | The object-level ToolTip has neither extension nor customization modifiability in the SDK property tables (`GetPageProperties`, `GetReportProperties` in `ObjectParser`), so an extension object cannot carry it. |

## Deliberate non-reports

- Obsolete symbols, and ToolTips that are not a plain label (`LabelPropertyValueSyntax`) are skipped; every other ToolTip on the registered kinds is checked, the only silence being a text that ends in one of the allowed characters.
- AC0015 ("Specifies") stays silent on object ToolTips: `MustStartWithSpecifies` only accepts table fields and page field controls, because "Specifies" describes a field value, not what a page or report does.

## SDK facts

- `GetPropertyValue` on a `PageObject` or `ReportObject` node reads only the object's own property list (`SyntaxNodeExtensions.GetPropertyList` maps them to `PageSyntax.PropertyList` and `ReportSyntax.PropertyList`), never a ToolTip of a nested control, action or request page field (verified against SDK net8.0 and net10.0 in `../nav-sdk-source`).
- The page object ToolTip requires runtime 18.0 and the report object ToolTip runtime 15.0 (`ObjectParser` compatibility); reports have no `ToolTipML`. The analyzer needs no version gate: the property is only in the tree when the parser accepted it.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `ToolTipAllowedPunctuations` | `[{ "Character": ".", "Name": "dot" }]` | The characters a ToolTip may end with; the `Name` values appear in the message. |

## Test notes

- Fixtures with custom settings (an exclamation-mark-only list, an empty list, an invalid entry) run on fixtures created with `TestHelper.CreateConfigWithSettings`.
- ToolTips on table fields require 13.0; those fixtures are version-gated.
- `PageObject` fixtures are gated on 18.0 and `ReportObject` fixtures on 15.0, per test case. Each nests a child ToolTip that satisfies every rule, so a diagnostic can only come from the object property. Report fixtures are `ProcessingOnly` reports without a dataset because the harness has no system tables.
