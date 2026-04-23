---
applyTo: 'src/ALCops.PlatformCop/**/ReportLayoutPropertyLength*'
---

# PC0032: ReportLayoutPropertyLength

## Purpose

Detects `Caption` and `Summary` properties on report layout blocks (`rendering > layout`) that exceed 250 characters. The AL compiler allows any length, but at runtime Business Central throws "The length of the string is N, but it must be less than or equal to 250 characters" when a user opens the Report Layout Selection page. This is a hard crash with no workaround.

**References:**
- [GitHub Issue #176](https://github.com/ALCops/Analyzers/issues/176)

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `PC0032` |
| Category | Design |
| Severity | Error |
| Enabled by default | true |
| MessageFormat | `Report layout {0} is {1} characters long, but the maximum is 250. Business Central will fail at runtime when selecting this layout.` |
| Version gate | None |
| netstandard2.1 | Full support (no net8.0-only APIs used) |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | PlatformCop (PC0032) | Undocumented platform DB field limit causing runtime crash, same class as PC0028 (TableRelationFieldLength) |
| Severity | Error | 100% causes hard runtime crash when user opens Report Layout Selection page |
| Category | Design | Structural correctness issue |
| Registration | `RegisterSyntaxNodeAction` on `SyntaxKind.ReportLayout` | Direct syntax access to layout properties |
| Properties checked | Caption, Summary | Both stored in 250-char DB fields |
| Max length | 250 (constant) | Empirically confirmed from runtime error; undocumented in MS Learn |
| CodeFix | None | Auto-truncating text would produce nonsensical content |
| Version gate | None | `rendering > layout` blocks exist since BC21 (Spring 2023); all supported BC versions have them |
| Skip obsolete | Yes | Standard ALCops convention |
| Report/reportextension | Both checked | Layout blocks can appear in either object type |

## Architecture

### Registration strategy

Uses `RegisterSyntaxNodeAction` on `SyntaxKind.ReportLayout` to analyze each layout block individually.

### Analysis flow

1. Skip obsolete symbols
2. Check `Caption` property via `GetPropertyValue("Caption")` as `LabelPropertyValueSyntax`
3. Extract text via `labelProperty.Value.LabelText.GetLiteralValue()?.ToString()`
4. If text length > 250, report diagnostic at property location
5. Repeat for `Summary` property

### Pattern reference

Follows the same approach as `EmptyCaptionLocked` (AC0033), which registers on `SyntaxKind.ReportLayout` and reads Caption via `GetPropertyValue("Caption")` returning `LabelPropertyValueSyntax`.

## Test coverage

### HasDiagnostic (4 cases)

| Test case | Scenario |
|---|---|
| CaptionExceeds250 | Report layout with Caption of 251 characters |
| SummaryExceeds250 | Report layout with Summary of 251 characters |
| ReportExtensionCaptionExceeds250 | Reportextension layout with Caption of 251 characters |
| BothExceed250 | Report layout with both Caption and Summary exceeding 250 characters |

### NoDiagnostic (5 cases)

| Test case | Suppression reason |
|---|---|
| CaptionExactly250 | Caption at exactly 250 characters (boundary, inclusive limit) |
| CaptionUnder250 | Short Caption (16 characters) |
| SummaryUnder250 | Short Summary (39 characters) |
| NoCaptionOrSummary | Layout block without Caption or Summary properties |