---
applyTo: 'src/ALCops.ApplicationCop/**/TableDataAccessRequiresPermissions*'
---

# AC0031: Table Data Access Requires Permissions

## Purpose

Detects table data access (reads, inserts, modifies, deletes) that is not covered by any declared permission source. Reports an Info-level diagnostic so developers add the `Permissions` property to the containing object.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | AC0031 |
| Category | Design |
| Severity | Info |
| Help URI | https://alcops.dev/docs/analyzers/applicationcop/ac0031/ |

## Architecture

The analyzer uses a shared `Permissions/` module in `ALCops.Common`:

```
src/ALCops.Common/
└── Permissions/
    ├── DatabaseOperation.cs                     # Enum: None, Read, Insert, Modify, Delete
    ├── MethodOperationMap.cs                    # Maps method names → DatabaseOperation
    ├── RequiredPermission.cs                    # Record struct holding table + operation + location
    ├── DeclaredPermissionSet.cs                 # Tracks granted ops per table
    ├── PermissionResolver.cs                    # Static class: IsCovered(), permission source resolution
    ├── PermissionSyntaxHelper.cs                # Shared helpers for multi-line/sorted insertion
    └── PermissionTableNameResolver.cs           # C#-like namespace resolution for table names

src/ALCops.ApplicationCop/
├── Analyzers/
│   └── TableDataAccessRequiresPermissions.cs    # Analyzer (callbacks + reporting)
└── CodeFixes/
    └── TableDataAccessRequiresPermissions.cs    # CodeFix (add missing permissions)
```

### PermissionResolver

Central resolution logic checking permission sources in priority order:
1. **Page SourceTable exemption** (all CRUD exempt on page's own source table)
2. **Table-level `InherentPermissions` property**
3. **Method-level `[InherentPermissions]` attribute**
4. **Object-level `Permissions` property**

Table matching uses namespace-aware name matching (primary) and object ID matching (secondary).

### MethodOperationMap

Maps AL built-in record methods to `DatabaseOperation`:
- Read: Find, FindFirst, FindLast, FindSet, Get, GetBySystemId, IsEmpty, Count
- Insert: Insert
- Modify: Modify, ModifyAll, Rename
- Delete: Delete, DeleteAll

### Analyzer callbacks

| Callback | Trigger | Operation |
|---|---|---|
| `AnalyzeInvocation` | `OperationKind.InvocationExpression` | From MethodOperationMap |
| `AnalyzeReportDataItem` | `SymbolKind.ReportDataItem` | Read |
| `AnalyzeQueryDataItem` | `SymbolKind.QueryDataItem` | Read |
| `AnalyzeXmlPortNode` | `SymbolKind.XmlPortNode` | Depends on Direction + AutoSave/AutoReplace/AutoUpdate |

## Design decisions

| Decision | Rationale |
|---|---|
| `DatabaseOperation` is a simple enum, not `[Flags]` | Each method call maps to exactly one operation; matches AppSourceCop pattern |
| Namespace-aware matching is primary, object ID is secondary | Namespaces are the modern AL convention; IDs provide backwards compatibility |
| `PermissionResolver` is static | No state needed; all inputs passed as parameters |
| `DeclaredPermissionSet` exists but is unused by the analyzer | Prepared for future inverted rule (unused permissions) |
| `Rec.Modify()` in table objects detected via explicit Instance path | The AL compiler resolves `Rec.Modify()` with a non-null Instance |
| InherentPermissions attribute parsed via syntax text splitting | The attribute's syntax is well-defined; avoids complex semantic analysis |
| `TestPermissions = Disabled` suppresses diagnostic | Test codeunits with disabled permissions are intentionally testing without permission checks |
| Skip permissionset/permissionsetextension objects | These objects declare permissions as their core purpose, not code that accesses tables; skipping improves performance |

## CodeFix

The `TableDataAccessRequiresPermissionsCodeFixProvider` adds missing permissions. It supports FixAll.

### Scenarios

| Scenario | Behavior |
|---|---|
| No `Permissions` property | Creates `Permissions = tabledata {Table} = {op};` |
| Table already listed | Merges the missing char in canonical `rimd` order |
| Table not listed, single-line format | Appends `, tabledata {Table} = {op}` (or inserts alphabetically if sorted) |
| Table not listed, multi-line format | Appends with `\n` + matching indentation (or inserts alphabetically if sorted) |
| Extension objects | CodeFix is skipped (extensions cannot declare Permissions) |

### Table name resolution

Uses C#-like namespace resolution (`PermissionTableNameResolver`):
- Same namespace or imported via `using`: simple name (`MyTable`)
- Different namespace, not imported: qualified name (`MyNamespace.MyTable`)

### Multi-line insertion

`SeparatedSyntaxList.Insert()` creates default comma separators without newline trivia. The `InsertIntoMultiLineList` helper fixes this by using `ReplaceToken` to copy trailing trivia from an existing separator onto the newly created one.

### Design decisions (CodeFix-specific)

| Decision | Rationale |
|---|---|
| Passes TableName, TableNamespace, PermissionChar via `ImmutableDictionary` properties | Standard CodeFix data passing pattern |
| Permission chars preserve existing casing convention | If existing permissions use uppercase (e.g. `RM`), added chars match (`RIM`). Defaults to lowercase for new entries. |
| `ApplyFix` re-finds ObjectSyntax by kind+name from current tree | BatchFixer applies fixes sequentially; using captured node references causes stale-reference bugs (phantom entries, wrong merges) |
| Sorted detection uses case-insensitive string comparison | AL identifiers are case-insensitive |
| Multi-line separator fix via `ReplaceToken` | Avoids need for internal `SeparatedSyntaxList` constructor |
| `FixAllTitle` uses a separate generic resx string (`TableDataAccessRequiresPermissionsFixAllCodeAction`) | FixAll applies across multiple permissions/tables, so the title must not reference a specific permission or table |
| `insertIndex == 0` gets special trivia handling in multi-line lists | First entry sits on the `Permissions = ` line with no leading indentation; displaced entries need indentation added |

## Test coverage

**HasDiagnostic (10 cases):** ProcedureCalls, ProcedureCallsExtended, GetBySystemId, Count, ImplicitSelfCallInTable, ThisKeywordSelfCallInTable, XmlPorts, Queries, Reports, DottedTableName.
**NoDiagnostic (21 cases):** ProcedureCallsPermissionsProperty, ProcedureCallsPermissionsPropertyFullyQualified, ProcedureCallsInherentPermissionsProperty, ProcedureCallsInherentPermissionsAttribute, PageSourceTable, PageExtensionSourceTable, XmlPortPermissionsProperty, XmlPortInherentPermissions, QueryPermissionsProperty, QueryInherentPermissions, ReportPermissionsProperty, ReportInherentPermissions, XMLPortWithTableElementProps, PermissionsAsObjectId, PermissionPropertyWithPragma, PermissionPropertyWithComment, MultiplePermissionsDifferentType, TestPermissionsDisabled, GetBySystemIdWithPermissions, CountWithPermissions, ImplicitSelfCallWithInherentPermissions.
**HasFix (9 cases):** AddNewPermissionsProperty, AddNewTableEntry, MergePermissionChar, MergeCanonicalOrder, AddEntryMultiLine, AddEntrySingleLine, AddEntryAlphabetical, AddEntryAppend, AddEntryAlphabeticalFirst.

## Known issues / future work

- **IntegerTable** test case is skipped (commented out)
- **Bare implicit calls** (`Modify()` without `Rec.`) inside table objects may not be detected as invocations; use `Rec.Modify()` pattern
- **CalcFields/CalcSums** are not yet covered (out of scope for initial implementation)
- **CodeFix: blank line formatting** When creating a new Permissions property on an object that has no properties, no blank line is inserted between the new property and the first member (trigger/procedure)
- **CodeFix: cross-namespace test** The single-file test framework cannot test qualified table name resolution; both objects must be in the same file
- **Inverted rule** (permissions declared but not needed) is planned as a separate diagnostic