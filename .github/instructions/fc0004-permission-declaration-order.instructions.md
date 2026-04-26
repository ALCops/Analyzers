---
applyTo: 'src/ALCops.FormattingCop/**/PermissionDeclarationOrder*'
---

# FC0004: Permission Declaration Order

## Purpose

Detects `Permissions` property entries that are not sorted alphabetically by (type keyword, object name). Provides a CodeFix that reorders the entries and converts single-line format to multi-line when there are 2+ entries.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | FC0004 |
| Category | Style |
| Severity | Info |
| Help URI | https://alcops.dev/docs/analyzers/formattingcop/fc0004/ |

## Architecture

```
src/ALCops.Common/
└── Permissions/
    └── PermissionSyntaxHelper.cs    # Shared sort logic, multi-line builder

src/ALCops.FormattingCop/
├── Analyzers/
│   └── PermissionDeclarationOrder.cs           # Analyzer (CompilationAction)
└── CodeFixes/
    └── PermissionDeclarationOrderCodeFixProvider.cs  # CodeFix (sort + reformat)
```

### Sort order

Entries are sorted by two keys:
1. **Type keyword** (alphabetical, case-insensitive): `codeunit` < `page` < `query` < `record` < `report` < `table` < `tabledata` < `xmlport`
2. **Object name** (alphabetical, case-insensitive) within the same type

Uses `StringComparison.OrdinalIgnoreCase` for both comparisons. Note that ordinal comparison means `(` < `+` < `0` < `A`, which may differ from culture-specific ordering.

### Analysis flow

1. Iterate all objects via `compilation.GetDeclaredApplicationObjectSymbols()` (including permissionset/permissionsetextension)
2. For each object with a `Permissions` property containing 2+ entries
3. Check if entries are sorted using `PermissionSyntaxHelper.ArePermissionsSorted`
4. Report one diagnostic on the `PropertySyntax` node if not sorted

### Key difference from AC0031/AC0032

- Does NOT skip `permissionset`/`permissionsetextension` objects (AC0031/AC0032 skip them)
- Does NOT skip test codeunits with `TestPermissions = Disabled`
- Analyzes all permission types (codeunit, page, report, table, tabledata, etc.), not just `tabledata`

## Design decisions

| Decision | Rationale |
|---|---|
| `RegisterCompilationAction` | Same pattern as AC0032; iterates all objects in one pass |
| One diagnostic per object, not per entry | The fix reorders the entire list; per-entry diagnostics would be noise |
| Diagnostic on `PropertySyntax` node | Ensures CodeFix can find the node via `FindNode` + ancestor/descendant traversal |
| Type+name sorting applied globally (affects AC0031/AC0032) | `ArePermissionsSorted` was refactored from name-only to type+name; consistent behavior across all permission rules |
| CodeFix always outputs multi-line for 2+ entries | Consistent formatting; single-line permissions are hard to scan |
| Single entry stays single-line | No formatting benefit from multi-line with one entry |
| Permission chars casing preserved | Each entry keeps its original `r`/`R`/`rimd`/`RIMD`/`X` casing |

## CodeFix

The `PermissionDeclarationOrderCodeFixProvider` sorts all entries and reformats to multi-line. Supports FixAll via BatchFixer.

### Scenarios

| Scenario | Behavior |
|---|---|
| Multi-line, unsorted | Reorder entries, preserve multi-line format with matching indentation |
| Single-line, 2+ entries, unsorted | Reorder and convert to multi-line format |
| Single entry | No diagnostic (trivially sorted) |

### Node finding

The CodeFix uses a robust node-finding pattern:
```csharp
var propertySyntax = node as PropertySyntax
    ?? node.FirstAncestorOrSelf<PropertySyntax>()
    ?? node.DescendantNodes().OfType<PropertySyntax>().FirstOrDefault();
```

### Shared helpers used

- `PermissionSyntaxHelper.GetSortedPermissions`: Sorts entries by (type, name)
- `PermissionSyntaxHelper.BuildMultiLinePermissionValue`: Creates multi-line formatted output
- `PermissionSyntaxHelper.GetEntryIndentation`: Detects existing indentation pattern

## Test coverage

**HasDiagnostic (4 cases):** UnsortedTabledata, UnsortedMixedTypes, UnsortedSingleType, UnsortedCodeunit.
**NoDiagnostic (4 cases):** AlreadySorted, SingleEntry, NoPermissionsProperty, SortedTabledata.
**HasFix (4 cases):** ReorderTabledata, ReorderMixedTypes, SingleLineToMultiLine, PreserveCasing.

## Refactoring impact

The `PermissionSyntaxHelper.ArePermissionsSorted` method was changed from name-only to type+name comparison. This affects AC0031's `FindInsertionIndex` (used by its CodeFix to decide alphabetical vs append insertion). The behavioral change is:
- A list sorted by name but not by type is now detected as "unsorted"
- AC0031's CodeFix will append (instead of inserting alphabetically) for such lists
- This is the correct behavior since type+name is the canonical sort order
