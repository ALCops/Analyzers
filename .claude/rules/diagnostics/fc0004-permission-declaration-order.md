---
paths:
  - "src/ALCops.FormattingCop/**/PermissionDeclarationOrder*"
  - "src/ALCops.FormattingCop.Test/Rules/PermissionDeclarationOrder/**"
  - "src/ALCops.Common/Permissions/PermissionEntryComparer.cs"
  - "src/ALCops.Common/Permissions/NaturalStringComparer.cs"
  - "src/ALCops.Common/Permissions/PermissionRegionGroup.cs"
---

# FC0004: PermissionDeclarationOrder

## Purpose

Detects `Permissions` property entries that are not in the order the AZ AL Dev Tools "Sort Permissions" command produces ("Option B" in [#245](https://github.com/ALCops/Analyzers/issues/245), chosen by community vote). Provides a CodeFix that reorders the entries in place, preserving layout and `#region` blocks, and converts single-line lists to multi-line.

Registers `RegisterCompilationAction`, one diagnostic per object on the `PropertySyntax`; main type `PermissionDeclarationOrder` (sorting lives in `ALCops.Common/Permissions`).

## Design decisions

| Decision | Rationale |
|---|---|
| Mirror AZ AL Dev Tools verbatim, quirks included | The ecosystem has three incompatible orders (AL SDK generator, AZ, old FC0004) and Microsoft's own apps are inconsistent; the community picked AZ so that "Sort Permissions" and FC0004 never disagree. Every rule was read from `anzwdev/al-code-outline` source, not its documentation. |
| No `alcops.json` switch | Breaking change accepted; AZ order is *the* order. |
| Unknown type keyword (`system`, anything future) sorts as `table` | AZ's `GetTypePriority` returns 0 outside its map. Arguably an AZ bug, but diverging would create FC0004 hits on AZ-sorted code. |
| Sort key is the raw `ObjectReference.Identifier` text with outer quotes removed only when the whole text is quoted; AZ's mangling of `"MyNs".Item` into `MyNs".Item` is not reproduced | Mirrors AZ's `DecodeName`: `MyNs."Zulu Table"` sorts before `MyNs.Alpha` because the quote character takes part in the compare. The key AC0031 uses for matching (`GetObjectNameFromPermission`) is a different concern. |
| `InvariantCultureIgnoreCase` with spaces stripped for name chunks, a deliberate exception to the "use `SemanticFacts` comparers" rule | This is what puts `Post. Appr.` before `Post Inv.` (`.` < `I`), the original report. CA1304/1305/1307/1309/1310 are already `none` in `.editorconfig`. |
| Digit runs compared without parsing (strip leading zeros, length then ordinal); length tie-break ignores spaces | AZ's `int.Parse` throws above `int.MaxValue`, so ours gives AZ's answer whenever AZ does not throw. AZ tie-breaks on raw lengths after stripping spaces, which makes `x 1`, `x1y`, `x1z` non-transitive and a sort that never converges; we keep the `IComparer` contract. |
| Wildcard `tabledata * = RIMD` gets an empty key and sorts last | `ObjectReference` is null for the `*` form; AZ would NRE here, and an empty key is what its `NullableStringComparer` does for empties. |
| `#region` handling is a tree, not flat runs; single-entry regions stay groups | Mirrors AZ's `SyntaxNodesGroupsTree`: a group emits its own entries first, then its child regions, so a root entry placed after a region is moved above it. AZ's `sortSingleNodeRegions` setting is not replicated. |
| Diagnostic means "the fix would move an entry": the tree is sorted once and each entry's source index is compared with its output position | Analyzer and CodeFix can never disagree, and equal keys (stable sort) never fire. |
| AC0031's insertion helper (`FindInsertionIndex`) is group-scoped: it positions the new `tabledata` entry with `PermissionEntryComparer` inside its own table/tabledata group only | The AC0031 fix never introduces a new FC0004 violation, even in lists that still keep codeunit/page entries first. |

## Deliberate non-reports

- Lists containing any non-region directive (`#if`, `#pragma`, ...) or unbalanced regions: AZ refuses to sort them, so no diagnostic and no fix.
- An `#endregion` on the line after the `;` (a common hand-written layout) is outside the property, so the list counts as unbalanced and is never checked; AZ behaves the same because it does not pass the closing token for separated lists.
- Entries with equal sort keys in any relative order: the sort is stable, so they are never reported.

## Known issues

- Comments are positional, not attached: a `// ...` line above an entry stays at its slot when the entry moves, so a comment describing an entry (or a region's header comment) can end up above a different entry, even outside the region. Attaching comments to entries would break the "slot keeps its layout" rule for the common uniform-indent case; accepted.
- When a region group ends up last, its `#endregion` is emitted right before the `;`, which then sits on its own line. AZ produces the same text.
- Culture comparison depends on the ICU version of the machine running `alc`; exotic punctuation may order differently between machines.
- AC0031's insertion is region-unaware: in a region-grouped list whose tabledata entries are not globally in order it appends instead of inserting by name.

## SDK facts

- `PermissionSyntax.ObjectReference` is null for the wildcard form; the `*` is exposed as `PermissionSyntax.AsteriskToken`.
- `ObjectReference.Identifier` is an `IdentifierNameSyntax` or `QualifiedNameSyntax`; object ids in `Permissions` entries are rejected by the compiler (AL0653).

## CodeFix: PermissionDeclarationOrderCodeFixProvider

| Decision | Rationale |
|---|---|
| FixAll via `BatchFixer` | One diagnostic per property, so there are no shared-ancestor conflicts. |
| Slots stay, entries move: position *i* receives the *i*-th sorted entry but keeps the indentation and comments that were at position *i*; separators are untouched; region directives are re-emitted where their group now starts/ends; trailing `#endregion` trivia after the last entry is prepended to the `;` | Comments attached to a slot stay positional and the issue's `Permissions =\n    entry` layout survives. Newlines the fix inserts reuse an end-of-line trivia found in the list, so CRLF files stay CRLF. |
| Empty `#region ... #endregion` pairs stay with the entry that carried them | An empty group has nothing to sort; as a tree node it would be flattened after the entries and drift to the end of the list. |
| Single-line lists (no newline separators, no directives) are rewritten as multi-line via `BuildMultiLinePermissionValue` | Unchanged behaviour from the original rule. |
| The fix bails out defensively on non-region directives or unbalanced regions | Mirrors the analyzer, which never reports such lists. |
