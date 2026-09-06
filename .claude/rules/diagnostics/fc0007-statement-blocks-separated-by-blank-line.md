---
paths:
  - "src/ALCops.FormattingCop/**/StatementBlocksSeparatedByBlankLine*"
  - "src/ALCops.FormattingCop.Test/Rules/StatementBlocksSeparatedByBlankLine/**"
---

# FC0007: StatementBlocksSeparatedByBlankLine

## Purpose

Reports missing blank lines around statement blocks: before/after control-flow constructs (`if`, `case`, `repeat`, `while`, `for`, `foreach`) and before scope-leaving statements (`exit`, built-in `Error(...)` and `FieldError(...)`). It is highly opinionated and therefore disabled by default; enable it explicitly and configure it via `alcops.json`.

Registers syntax-node actions from CompilationStart for control-flow statements, ExitStatement and InvocationExpression. Standalone calls are bound to operations and classified by `ALCops.Common.FlowTerminatingBuiltIns`; main type `StatementBlocksSeparatedByBlankLine`.

## Design decisions

| Decision | Rationale |
|---|---|
| Disabled by default | Spacing preferences vary widely; teams should opt in. |
| Settings live in a nested `StatementBlockSpacing` object (`StatementBlockSpacingSettings.cs`) with real C# enums serialized as strings (`JsonStringEnumConverter` on net8+, `StringEnumConverter` on netstandard2.1) | Keeps `ALCopsSettings` slim, gives type-safe tri-state options, and human-readable JSON matches the other settings; both converters are case-insensitive. |
| Blank-line check inspects `SourceText.Lines` strictly between the two token positions and requires at least one whitespace-only line, rather than comparing token line numbers | A line-number diff would count a comment-only or directive line as a separator, contradicting the intended semantics. |
| Sibling `StatementSyntax` nodes are taken from the parent, not from a `BlockSyntax` | Statement sequencing also occurs in contexts without a `BlockSyntax`, for example `repeat`. |
| Each statement gap has exactly one configuration-aware diagnostic owner: an adjacent block owns its "before" gap only when that check actually runs, otherwise the previous block's "after" check or the scope-leaver's "before" check does | Avoids duplicate diagnostics (e.g. a block followed by `exit`) while still reporting next to one-liners or when `ControlFlowBefore` is disabled. |
| Scope-leaving calls are the shared `FlowTerminatingBuiltIns` set (`Dialog.Error`, `Table.FieldError`, `FieldRef.FieldError`), including incomplete calls whose receiver binds to those types | One semantic definition shared with PC0038 and LC0089; the exact class-and-method match keeps user-defined `Error`/`FieldError` procedures out, and accepting the invalid binding avoids flicker while a call is being typed. |
| The diagnostic names the terminating call that was found (`Error()`, `FieldError()`) | The message must match the statement the developer is looking at, not always say `Error()`. |
| Callbacks capture the compilation from CompilationStart; standalone invocations use GetOperation after cheap syntax checks | Settings and CM0001 must share one snapshot. The NAV SDK exposes no RegisterOperationAction on CompilationStart and supplies a different compilation through semantic-model contexts. Binding the invocation retains the same semantic classifier without relying on callback order. |

## Deliberate non-reports

- The first statement in a block and the first statement directly owned by a control-flow construct.
- One-liner statements (the whole statement on a single line) unless `OneLinerMode = All`; `if X then Y` rarely benefits from surrounding blank lines.
- `else` that shares its line with the previous token (`if X then Y else Z`), even with `ElseChainBeforeMode = RequireBlank`.
- An `exit`, `Error(...)` or `FieldError(...)` used directly as an `if` branch: branch statements are not siblings in a statement list, so only the containing `if` is governed (by `ControlFlowBefore`/`ControlFlowAfter` and, for one-line guards, `OneLinerMode`).
- Loop-control statements (`break`, `continue`, `Skip`): only `exit` and the built-in `Error`/`FieldError` terminators are scope-leavers.
- User-defined procedures named `Error` or `FieldError`.
- Blank lines between the branches of a `case` statement; only the spacing around the whole `case` block is enforced.

## Known issues

- Comment-only lines between statements are not separators; only whitespace-only lines count, so a `//---- divider` line still yields a diagnostic.
- Compiler-directive lines (`#region`/`#endregion`, `#pragma`) count as non-blank interior lines and do not satisfy the blank-line requirement.
- A collectible `Error(ErrorInfo)` inside an `ErrorBehavior::Collect` scope is treated as scope-leaving although execution continues; inherited from the shared classifier (see PC0038's Known issues).

## Settings

All properties sit under the nested `StatementBlockSpacing` object in `alcops.json`; enum values are deserialized case-insensitively and `alcops.schema.json` is the source of truth for the value lists.

| Setting | Default | Effect |
|---|---|---|
| `ControlFlowBefore` | `true` | Require a blank line before control-flow blocks. |
| `ControlFlowAfter` | `true` | Require a blank line after control-flow blocks; skipped for an adjacent control-flow sibling only when that sibling's active "before" check owns the gap. |
| `ScopeLeavingMode` | `ExitAndError` | Which scope-leaving statements (`exit`, built-in `Error`/`FieldError`) require a blank line before them; `ErrorOnly` limits it to the built-in terminators, `Off` disables. |
| `ElseChainBeforeMode` | `Off` | `RequireBlank` requires a blank line before `else` / `else if` (one-line `else` exempt). |
| `OneLinerMode` | `None` | `All` includes one-liner statements in the spacing checks. |

## Test notes

- The rule is `isEnabledByDefault: false`, so the test class injects `StatementBlocksSeparatedByBlankLine.ruleset.json`.
- Settings variants are named `alcops.json` snippets injected via `MemoryFileSystem` and selected per `TestCase` (`null` = defaults).
- Two regression fixtures exercise the settings provider: a malformed enum value must fall back to defaults silently, and `"StatementBlockSpacing": null` must be normalized to defaults so the analyzer does not NRE ([#328](https://github.com/ALCops/Analyzers/issues/328)).
- An incomplete `FieldError` call is tested through a fixture created with `ThrowsWhenInputDocumentContainsError = false` (`HasDiagnosticInDocumentWithErrors`), and a message assertion checks that the diagnostic names `FieldError()`.
