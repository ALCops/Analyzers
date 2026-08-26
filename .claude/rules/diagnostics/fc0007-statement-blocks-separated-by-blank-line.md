---
paths:
  - "src/ALCops.FormattingCop/**/StatementBlocksSeparatedByBlankLine*"
---

# FC0007: StatementBlocksSeparatedByBlankLine

## Purpose

FC0007 reports missing blank lines around statement blocks: before/after control-flow constructs (`if`, `case`, `repeat`, `while`, `for`, `foreach`) and before scope-leaving statements (`exit`, built-in `Error(...)`). It is highly opinionated and therefore disabled by default; enable it explicitly and configure it via `alcops.json`.

## Design decisions

| Decision | Rationale |
|---|---|
| Rule is disabled by default | The rule is opinionated (spacing preferences vary widely); teams should opt in. |
| Move settings into a dedicated file (`StatementBlockSpacingSettings.cs`) | Keeps `ALCopsSettings` slim and colocates the 5 properties + 3 enums with their consumer. |
| Real C# enums instead of strings for tri-/multi-state options | Type-safe comparisons in the analyzer, discoverable values, IDE-friendly. |
| JSON string enums via `JsonStringEnumConverter` (net8+) and `StringEnumConverter` (netstandard2.1) | Human-readable JSON matches other alcops settings. Both converters are case-insensitive by default. |
| Blank-line check inspects source-text lines strictly between the two token positions | Comparing token line diffs alone counts a comment-only line as a separator, contradicting the intended semantics. Iterating `SourceText.Lines` and requiring at least one whitespace-only line is robust against comments and directives. |
| Use sibling `StatementSyntax` nodes from the parent | The rule is about statement sequencing in the same statement list and must also work in contexts not represented by `BlockSyntax` (for example `repeat`). |
| Give each statement gap one configuration-aware diagnostic owner | Avoids duplicate diagnostics before scope-leavers while preserving diagnostics next to one-liners or when `ControlFlowBefore` is disabled. An adjacent block owns its "before" gap only when that check actually runs. |
| Skip one-liners unless `OneLinerMode = All` | One-liner control-flow statements (`if X then Y`) rarely benefit from surrounding blank lines. |
| Detect `Error(...)` via `MethodKind.BuiltInMethod` + `IsSameName` | Distinguishes the built-in from user-defined `Error` procedures. |
| `ElseChainBeforeMode` short-circuits when `else` shares its line with the previous token | Prevents false positives on `if X then Y else Z` one-liners. |

## Scope

- Reports when a control-flow statement (`if`/`case`/`repeat`/`while`/`for`/`foreach`) is missing a blank line before it when it follows another statement in the same block.
- Reports when a normal statement follows one of those blocks without a blank line.
- Reports when a scope-leaving statement (`exit`, built-in `Error`) is missing a blank line before it when it follows another statement in the same block.
- Reports when `else` (block or else-if) has no blank line above it, if configured.
- Does not report the first statement in a block, the first statement directly owned by a control-flow construct, or a one-liner (unless configured otherwise).
- Adjacent control-flow blocks have one configuration-aware owner: the "after" check is skipped only when the next sibling's "before" check is enabled and includes that statement under `OneLinerMode`.
- A control-flow block followed by `exit` or built-in `Error(...)` produces one diagnostic for the gap. The block's active "after" check owns it; otherwise the scope-leaving "before" check does.

## Configuration surface (`alcops.json` → `StatementBlockSpacing`)

The rule reads settings from `alcops.json` via `ALCopsSettingsProvider`. All properties belong to the nested object `StatementBlockSpacing`. Enum values are deserialized case-insensitively.

| Property | Type | Default | Effect |
|---|---|---|---|
| `ControlFlowBefore` | `bool` | `true` | Require blank line before control-flow blocks. |
| `ControlFlowAfter` | `bool` | `true` | Require blank lines after control-flow blocks. For an adjacent control-flow sibling, the check is skipped only when that sibling's active "before" check owns the gap. |
| `ScopeLeavingMode` | enum `Off` \| `ExitOnly` \| `ErrorOnly` \| `ExitAndError` | `ExitAndError` | Which scope-leaving statements require a blank line before them. |
| `ElseChainBeforeMode` | enum `Off` \| `RequireBlank` | `Off` | Require blank line before `else` / `else if`. Skipped when `else` is on the same line as the previous token (one-liner). |
| `OneLinerMode` | enum `None` \| `All` | `None` | Include or exclude one-liner statements (whole statement spans a single line) from spacing checks. |

Settings type: [src/ALCops.Common/Settings/StatementBlockSpacingSettings.cs](src/ALCops.Common/Settings/StatementBlockSpacingSettings.cs).

## Known issues / non-goals

- The rule does not handle `foreach`-only scenarios differently from other control-flow constructs.
- No CodeFix is provided; the missing blank line must be added manually.
- Comments between statements are not treated as separator content: only whitespace-only lines count. See Roadmap → `TreatCommentOnlyLinesAsSeparator`.
- Blank lines **between the branches** of a `case` statement are not enforced (only the spacing around the whole `case` block is). See Roadmap → `CaseBranchMode`.
- An `exit` or `Error(...)` used directly as an `if` branch is not analyzed as an independent scope-leaver because branch statements are not siblings in a statement list. The containing `if` is governed by `ControlFlowBefore` / `ControlFlowAfter` and, for one-line guards, `OneLinerMode`. See Roadmap → `GuardClauseMode`.
- Loop-control statements (`break`, `continue`, `Skip`) are not covered — only `exit` and `Error(...)` are. See Roadmap → `LoopControlBeforeMode`.
- Compiler-directive boundaries (`#region` / `#endregion`, `#pragma`) count as non-blank interior lines under the standard rule and therefore do **not** satisfy the blank-line requirement. Configuring them as explicit separators is a Roadmap item. See Roadmap → `SkipDirectiveBoundaries`.

## Roadmap

Planned settings — none of these are implemented yet, and none has a scheduled milestone. Names, defaults, and value sets may change when a roadmap item is picked up. Track work here.

### `CaseBranchMode` (planned, not implemented)

A future `StatementBlockSpacing.CaseBranchMode` setting to control blank-line separation **between the individual branches of a `case` statement**, orthogonal to `ControlFlowBefore` / `ControlFlowAfter` (which govern the whole `case` block relative to its siblings).

Scope:
- Between `case Value:` label branches (single-statement form).
- Between `case Value: begin ... end;` block branches.
- Between the last branch and the `else` clause, and between `else` and `end;`.

Proposed values (final naming to be decided when implemented):

| Value | Behavior |
|---|---|
| `Off` | Do not enforce blank lines between branches (current behavior). |
| `BlocksOnly` | Require a blank line after each `begin ... end;` block branch, but not between label-only branches. |
| `All` | Require a blank line after every branch, regardless of whether the branch uses a block. |

Design questions to answer before implementation:
- Where is the diagnostic located? On the following branch's `case`-value token, or on the missing-blank position?
- Does it apply to one-line branches (`"A": DoA();`) or does `OneLinerMode` gate it?
- Is the `else` clause treated as just another branch, or is it a separate axis?
- Should adjacent label-only branches (`"A":\n"B":\n  DoAOrB();`) be exempt because they share a statement?

Implementation notes: `CaseStatementSyntax.CaseLines` (`SyntaxList<CaseLineSyntax>`) exposes each branch; `caseLine.Statement is BlockSyntax` distinguishes block-form from single-statement form. Estimated effort ~2.5 h including tests, schema, docs.

### `GuardClauseMode` (planned, not implemented)

A future `StatementBlockSpacing.GuardClauseMode` setting to define dedicated spacing for the widely-used **guard clause** early-exit pattern (`if X then exit;` / `if X then Error(...);` at the top of a method). Currently the direct branch is not analyzed as an independent scope-leaver; the containing `if` follows `ControlFlowBefore` / `ControlFlowAfter` and is excluded as a one-liner unless `OneLinerMode = All`.

Proposed values (final naming to be decided when implemented):

| Value | Behavior |
|---|---|
| `Off` | No dedicated guard-clause handling; the containing `if` follows the regular control-flow settings (current behavior). |
| `AllowStacked` | Consecutive guard clauses at the top of a method may be stacked without blank lines between them, but a blank line is required before the first non-guard statement below the guard block. |
| `Isolated` | Every guard clause requires a blank line before AND after it, regardless of stacking. |

Design questions to answer before implementation:
- What counts as a guard clause? Reuse the `IsGuardClause` heuristic already in [CognitiveComplexity.cs](src/ALCops.LinterCop/Analyzers/CognitiveComplexity.cs) (LC0090) so definitions do not drift.
- Is the "at the top of a method" positional requirement strict, or does any early-`exit` inside a loop also qualify?
- Interaction with `ScopeLeavingMode = Off`: should `GuardClauseMode` still activate? Recommended answer: no — if the user disabled scope-leaver spacing globally, the guard exception is moot.

### `LoopControlBeforeMode` (planned, not implemented)

A future `StatementBlockSpacing.LoopControlBeforeMode` setting for blank-line enforcement before **loop-control statements** — currently the analyzer only handles `exit` and built-in `Error(...)`, but `break`, `continue`, and `Skip` (the report/xmlport equivalents) share the same "control-flow-leaving" semantics and benefit from the same visual separation.

Proposed values (final naming to be decided when implemented):

| Value | Behavior |
|---|---|
| `Off` | Do not enforce (current behavior). |
| `RequireBlank` | Require a blank line before `break`, `continue`, and `Skip` when they follow another statement in the same block. |

Design questions to answer before implementation:
- Does this share the `Off` short-circuit with `ScopeLeavingMode`, or is it fully independent? Recommendation: fully independent — teams may want blank before `break` but not before `exit`.
- Is `Skip` (report/xmlport-only) covered under the same axis or its own? Recommendation: same axis for simplicity; add a separate mode only if user feedback demands it.
- Reuse the existing sibling-lookup + blank-line helpers (`GetSiblingStatements`, `HasBlankLineBetween`) — no new infrastructure needed.

### `TreatCommentOnlyLinesAsSeparator` (planned, not implemented)

A future `StatementBlockSpacing.TreatCommentOnlyLinesAsSeparator` boolean to decide whether **comment-only lines** between two statements satisfy the "blank line" requirement. Today only true empty lines count; a `//---- section divider` on its own line does not.

Proposed default: `false` (current behavior — only empty lines count). Setting to `true` opts in to the more lenient interpretation, where any non-empty line consisting solely of leading whitespace + a comment token counts as a separator.

Design questions to answer before implementation:
- Which comment kinds count? Line comments (`//`), block comments (`/* */`), XML-doc comments (`///`)? Recommendation: all three, because visual effect is the same.
- Is a multi-line block comment on a single line (`/* foo */`) a separator? Recommendation: yes.
- What about comments **on** a statement line (trailing comment)? Recommendation: no — the statement line still counts as "a statement line", the comment is trivia on it.

Implementation notes: the current check walks `SourceText.Lines` strictly between the two token positions and returns true iff any is whitespace-only. To support this setting, additionally count lines whose sole non-whitespace content is a comment token as blank-equivalent. Estimated effort ~1 h including tests.

### `SkipDirectiveBoundaries` (planned, not implemented)

A future `StatementBlockSpacing.SkipDirectiveBoundaries` boolean to **suppress** FC0007 diagnostics across compiler-directive boundaries (`#region` / `#endregion`, `#pragma`, etc.). Directives visually break the code but do not represent statements. Enforcing blank lines around them can conflict with team conventions that already collapse spacing at region boundaries.

Proposed default: `false` (current behavior — directive-only lines are non-blank, so spacing rules fire across them). Setting to `true` opts in to treating the boundary as a natural separator.

Design questions to answer before implementation:
- Which directives count? `#region` / `#endregion` clearly; `#pragma` is less obviously a "visual break". Recommendation: `#region` / `#endregion` only, with a follow-up if `#pragma` demand emerges.
- Does the setting suppress "before" checks, "after" checks, or both? Recommendation: both — a region boundary breaks the block conceptually in both directions.
- Interaction with `TreatCommentOnlyLinesAsSeparator`: independent — one is about comments, the other about directives.

Implementation notes: requires walking leading/trailing trivia of the surrounding tokens for `RegionDirectiveTrivia` / `EndRegionDirectiveTrivia`. Estimated effort ~2.5 h including tests.