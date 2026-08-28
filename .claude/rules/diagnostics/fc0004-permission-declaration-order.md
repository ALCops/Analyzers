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
| Sort key = raw `ObjectReference.Identifier` text with only outer quotes removed | AZ's `DecodeName` over `Identifier.ToString()`: `"My Table"` → `My Table`, but `MyNs."My Table"` stays verbatim (namespace dominates and the quote character takes part in the compare — `MyNs."Zulu Table"` < `MyNs.Alpha` because `"` sorts before letters). The matching key AC0031 uses (`GetObjectNameFromPermission`, unquoted `Ns.Name`) is a different concern and is unchanged. |
| `InvariantCultureIgnoreCase` with spaces stripped for name chunks | This is what makes the original report (`Post. Appr.` before `Post Inv.`) sorted: `Post.Appr…` < `PostInv…` because `.` < `I`. A deliberate exception to the "use `SemanticFacts` comparers for AL identifiers" rule; CA1304/1305/1307/1309/1310 are already `none` in `.editorconfig`. ICU drift across OS versions is accepted as a known limitation. |
| Natural digit runs compared without parsing | The only divergence from AZ: `int.Parse` in `AlphanumComparatorFast` throws on runs above `int.MaxValue`; ours strips leading zeros and compares length-then-ordinal, giving AZ's answer whenever AZ does not throw. |
| Wildcard `tabledata * = RIMD` gets an empty key and sorts last | `ObjectReference` is null for the `*` form (`PermissionSyntax.AsteriskToken`). AZ would NRE here; an empty key is what its `NullableStringComparer` does for empties. |
| `#region` handling is a tree, not flat runs | Mirrors `SyntaxNodesGroupsTree`: a group emits its own entries first, then its child regions. Consequence: a root entry placed *after* a region is moved above it (`RootEntryAfterRegion` fixture). Single-entry regions stay groups (AZ default `alOutline.sortSingleNodeRegions = false`; the setting is not replicated). |
| Any other directive (`#if`, `#pragma`, …) or unbalanced regions → no diagnostic, no fix | AZ refuses to sort such lists. An `#endregion` placed after the `;` is outside the property and therefore "unbalanced" — also no diagnostic, same as AZ. |
| Diagnostic = "the fix would move an entry" | `NeedsReordering` sorts the tree and compares the flattened order with the source order by node identity, so analyzer and CodeFix can never disagree, and equal keys (stable sort) never fire. |
| CodeFix keeps slots, moves entries | Position *i* receives the *i*-th sorted entry but keeps the indentation and comments that were at position *i*; separators are untouched; region directives are re-emitted where their group now starts/ends; trailing `#endregion` trivia left after the last entry is prepended to the `;`. Comments attached to a slot therefore stay positional (`PreserveCommentSlots`), and the issue's `Permissions =\n    entry` layout survives. |
| Single-line lists still become multi-line | Unchanged behaviour via `BuildMultiLinePermissionValue`. Used only when the list has no newline separators and no directives. |
| `RegisterCompilationAction`, one diagnostic per object on the `PropertySyntax` | Unchanged from the original rule. |

## Architecture

```
src/ALCops.Common/Permissions/
├── NaturalStringComparer.cs      # AZ AlphanumComparatorFast + NullableStringComparer
├── PermissionEntryComparer.cs    # AZ PermissionComparer: type priority + sort key + compare
├── PermissionRegionGroup.cs      # AZ SyntaxNodesGroup: #region tree, Sort, Flatten
└── PermissionSyntaxHelper.cs     # TryBuildRegionTree, NeedsReordering, ReorderPreservingLayout,
                                  # ArePermissionsSorted (flat), FindInsertionIndex, GetSortedPermissions

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

- `#endregion` on the line after the `;` (a common hand-written layout) is outside the property → the list is treated as unbalanced and never checked. Same as AZ, which does not pass the closing token for separated lists.
- When a region group ends up last in output order, its `#endregion` is emitted right before the `;`, which then sits on its own line (`ReorderRootEntryAboveRegion` fixture). AZ produces the same text.
- Culture comparison depends on the ICU version of the machine running `alc`; exotic punctuation may order differently between machines.
- AC0031's insertion uses the flat `ArePermissionsSorted` (region-unaware); in a region-grouped list it may append instead of inserting by name.

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

`FindInsertionIndex` now takes the `PermissionSyntax` that will be inserted and positions it with `PermissionEntryComparer`, so a `tabledata` entry added by the AC0031 fix lands inside the table/tabledata block by natural name (after a `table X` with the same name), before codeunit/page/query/report/xmlport — an AC0031 fix never creates an FC0004 hit. The string overload of `FindInsertionIndex` is kept for binary compatibility and delegates. AC0032 uses no ordering helper.
