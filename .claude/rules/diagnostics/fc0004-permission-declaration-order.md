---
paths:
  - "src/ALCops.FormattingCop/**/PermissionDeclarationOrder*"
  - "src/ALCops.Common/Permissions/PermissionEntryComparer.cs"
  - "src/ALCops.Common/Permissions/NaturalStringComparer.cs"
  - "src/ALCops.Common/Permissions/PermissionRegionGroup.cs"
---

# FC0004: PermissionDeclarationOrder

## Purpose

Detects `Permissions` property entries that are not in the order the AZ AL Dev Tools "Sort Permissions" command produces (issue #245, "Option B", chosen by community vote). Provides a CodeFix that reorders the entries in place, preserving layout and `#region` blocks, and converts single-line lists to multi-line.

## Design decisions

| Decision | Rationale |
|---|---|
| Mirror AZ AL Dev Tools verbatim, quirks included | The ecosystem has three incompatible orders (AL SDK generator, AZ, old FC0004) and Microsoft's own apps are inconsistent. The community picked AZ so that "Sort Permissions" and FC0004 never disagree. Every rule below was read from `anzwdev/al-code-outline` (`PermissionComparer`, `AlphanumComparatorFast`, `NullableStringComparer`, `SyntaxNodesGroupsTree`), not from its documentation. |
| No `alcops.json` switch | Breaking change accepted; AZ order is *the* order. Released as `feat(FC0004)` with the break called out. |
| Unknown type keyword (`system`, anything future) sorts as `table` | AZ's `GetTypePriority` returns 0 for keywords outside its map, so `system` entries interleave with the table group by name. Deliberately mirrored; it is arguably an AZ bug, but diverging would create FC0004 hits on AZ-sorted code. |
| Sort key = raw `ObjectReference.Identifier` text; outer quotes removed only when the whole text is quoted | AZ's `DecodeName` over `Identifier.ToString()`: `"My Table"` → `My Table`, but `MyNs."My Table"` stays verbatim (namespace dominates and the quote character takes part in the compare — `MyNs."Zulu Table"` < `MyNs.Alpha` because `"` sorts before letters). AZ also strips the *leading* quote of `"MyNs".Item`, leaving `MyNs".Item`; that mangling is not reproduced (divergence 3). The matching key AC0031 uses (`GetObjectNameFromPermission`, unquoted `Ns.Name`) is a different concern and is unchanged. |
| `InvariantCultureIgnoreCase` with spaces stripped for name chunks | This is what makes the original report (`Post. Appr.` before `Post Inv.`) sorted: `Post.Appr…` < `PostInv…` because `.` < `I`. A deliberate exception to the "use `SemanticFacts` comparers for AL identifiers" rule; CA1304/1305/1307/1309/1310 are already `none` in `.editorconfig`. ICU drift across OS versions is accepted as a known limitation. |
| Natural digit runs compared without parsing; length tie-break ignores spaces | Divergence 1: `int.Parse` in `AlphanumComparatorFast` throws on runs above `int.MaxValue`; ours strips leading zeros and compares length-then-ordinal, giving AZ's answer whenever AZ does not throw. Divergence 2: AZ tie-breaks on raw lengths after comparing chunks with spaces stripped, which makes `x 1`, `x1y`, `x1z` non-transitive (a sort that never converges — the fix output would be flagged again); we tie-break on space-stripped lengths so the `IComparer` contract holds. |
| Wildcard `tabledata * = RIMD` gets an empty key and sorts last | `ObjectReference` is null for the `*` form (`PermissionSyntax.AsteriskToken`). AZ would NRE here; an empty key is what its `NullableStringComparer` does for empties. |
| `#region` handling is a tree, not flat runs | Mirrors `SyntaxNodesGroupsTree`: a group emits its own entries first, then its child regions. Consequence: a root entry placed *after* a region is moved above it (`RootEntryAfterRegion` fixture). Single-entry regions stay groups (AZ default `alOutline.sortSingleNodeRegions = false`; the setting is not replicated). |
| Any other directive (`#if`, `#pragma`, …) or unbalanced regions → no diagnostic, no fix | AZ refuses to sort such lists. An `#endregion` placed after the `;` is outside the property and therefore "unbalanced" — also no diagnostic, same as AZ. |
| Diagnostic = "the fix would move an entry" | `TryBuildRegionTree` sorts the tree once (keys precomputed per entry); `NeedsReordering` flattens it and compares each entry's source index with its output position, so analyzer and CodeFix can never disagree, and equal keys (stable sort) never fire. |
| CodeFix keeps slots, moves entries | Position *i* receives the *i*-th sorted entry but keeps the indentation and comments that were at position *i*; separators are untouched; region directives are re-emitted where their group now starts/ends; trailing `#endregion` trivia left after the last entry is prepended to the `;`. Comments attached to a slot therefore stay positional (`PreserveCommentSlots`), and the issue's `Permissions =\n    entry` layout survives. Newlines the fix has to insert reuse an end-of-line trivia found in the list, so CRLF files stay CRLF. |
| Empty `#region … #endregion` pairs stay with the entry that carried them | An empty group has nothing to sort; as a tree node it would be flattened after the entries and drift to the end of the list (`EmptyRegionStaysInPlace`). |
| Single-line lists still become multi-line | Unchanged behaviour via `BuildMultiLinePermissionValue`. Used only when the list has no newline separators and no directives. |
| `RegisterCompilationAction`, one diagnostic per object on the `PropertySyntax` | Unchanged from the original rule. |

## Architecture

```
src/ALCops.Common/Permissions/
├── NaturalStringComparer.cs      # AZ AlphanumComparatorFast + NullableStringComparer
├── PermissionEntryComparer.cs    # AZ PermissionComparer: type priority + sort key + compare
├── PermissionRegionGroup.cs      # AZ SyntaxNodesGroup: #region tree, Sort, Flatten
└── PermissionSyntaxHelper.cs     # TryBuildRegionTree (sorts), NeedsReordering, ReorderPreservingLayout,
                                  # FindInsertionIndex (group-scoped), GetSortedPermissions

src/ALCops.FormattingCop/
├── Analyzers/PermissionDeclarationOrder.cs                # TryBuildRegionTree → NeedsReordering → report
└── CodeFixes/PermissionDeclarationOrderCodeFixProvider.cs # multi-line or directives → ReorderPreservingLayout,
                                                           # else BuildMultiLinePermissionValue
```

### Sort order

1. Type priority: `table` 0, `tabledata` 1, `codeunit` 2, `page` 3, `query` 4, `report` 5, `xmlport` 6, anything else 0.
2. Two entries that are both priority 0/1 compare by name; on an identical name `table` precedes `tabledata`. Otherwise different priorities order by priority, equal priorities by name.
3. Names: split into maximal digit / non-digit runs; digit vs digit numerically, anything else `InvariantCultureIgnoreCase` with spaces removed; all runs equal → shorter original string first; empty keys last.
4. Regions: each group sorted independently; output = group's own entries, then child groups in order.

### Where the sort key comes from

`PermissionEntryComparer.GetSortKey` reads `ObjectReference.Identifier.ToString()` — the whole `CodeExpressionSyntax` node (`IdentifierNameSyntax` or `QualifiedNameSyntax`; object ids are rejected by the compiler, AL0653), without outer trivia — then trims, strips outer quotes when the text starts with `"`, and unescapes `""`.

## Known issues

- Comments are positional, not attached: a `// …` line above an entry stays at that slot when the entry moves, so a comment describing an entry (or a region's header comment) can end up above a different entry — even outside the region when a root entry is pulled above it. Attaching comments to entries instead would break the "slot keeps its layout" rule for the common uniform-indent case; accepted.
- `#endregion` on the line after the `;` (a common hand-written layout) is outside the property → the list is treated as unbalanced and never checked. Same as AZ, which does not pass the closing token for separated lists.
- When a region group ends up last in output order, its `#endregion` is emitted right before the `;`, which then sits on its own line (`ReorderRootEntryAboveRegion` fixture). AZ produces the same text.
- Culture comparison depends on the ICU version of the machine running `alc`; exotic punctuation may order differently between machines.
- AC0031's insertion is region-unaware; in a region-grouped list whose tabledata entries are not globally in order it appends instead of inserting by name.

## CodeFix: PermissionDeclarationOrderCodeFixProvider

Supports FixAll via `BatchFixer` (one diagnostic per property, no shared-ancestor conflicts).

| Scenario | Behavior |
|---|---|
| Multi-line (any separator followed by a newline) or directives present | `ReorderPreservingLayout`: entries move, slots keep their trivia, regions re-emitted |
| Single-line, 2+ entries | `GetSortedPermissions` + `BuildMultiLinePermissionValue` (18-space or detected indentation) |
| Non-region directive / unbalanced regions | No diagnostic is reported; the fix also bails out defensively |

### Node finding

```csharp
var propertySyntax = node as PropertySyntax
    ?? node.FirstAncestorOrSelf<PropertySyntax>()
    ?? node.DescendantNodes().OfType<PropertySyntax>().FirstOrDefault();
```

## Effect on AC0031

`FindInsertionIndex` takes the `PermissionSyntax` that will be inserted and positions it with `PermissionEntryComparer` inside its own group (table/tabledata), requiring only that group to be in order. So a `tabledata` entry added by the AC0031 fix lands by natural name (after a `table X` with the same name) even in lists that still keep codeunit/page entries first — the fix never adds a *new* FC0004 violation. The old string-based overload and `ArePermissionsSorted` had no callers left and were removed. AC0032 uses no ordering helper.
