---
applyTo: 'src/ALCops.ApplicationCop/**/ToolTipPunctuation*'
---

# AC0014: ToolTipMustEndWithPunctuation

## Purpose

Checks that ToolTip text ends with an allowed punctuation character. The allowed set is configurable through `ToolTipAllowedPunctuations` in `alcops.json`.

## Diagnostic properties

**AC0014** · Category: Design · Severity: Info · Enabled: **true**
Message: `ToolTip must end with one of the following punctuations: '{0}'.`
No version gate · Full netstandard2.1 support

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Rule scope | Implemented in `ToolTipPunctuation` analyzer | Keeps all ToolTip punctuation and phrasing checks in one analyzer for shared extraction logic. |
| Allowed punctuation source | `ALCopsSettingsProvider.GetSettings(compilation.FileSystem)` | Makes punctuation configurable per workspace/app using existing settings infrastructure. |
| Default behavior | Fallback to dot (`.` / `dot`) when settings are missing | Preserves backward compatibility with prior AC0014 behavior. |
| Match logic | Suffix check against raw tooltip text ending before closing quote | Works with AL syntax representation where value is read from property source text. |
| Message parameterization | Report configured punctuation names in diagnostic argument | Gives actionable guidance to users based on current configuration. |

## Architecture

1. Extract ToolTip value from property syntax.
2. Resolve settings via file-system-based settings provider.
3. Build allowed punctuation set from `ToolTipAllowedPunctuations` or fallback default.
4. Return if any configured punctuation matches the tooltip ending.
5. Report AC0014 with configured punctuation names when no match exists.

## Known issues

- Empty or fully invalid `ToolTipAllowedPunctuations` configurations are ignored and the analyzer falls back to the default dot punctuation.

## Test coverage

**HasDiagnostic (7 cases):** PageAction, PageAnalysisView, PageField, TableField, CustomExclamationMissing, InvalidConfigFallbackToDefault, CustomNamesConfigDiagnostic.
**NoDiagnostic (6 cases):** PageAction, PageAnalysisView, PageField, TableField, CustomExclamationAllowed, EmptyConfigFallbackToDefault.
