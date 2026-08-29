---
paths:
  - "src/ALCops.ApplicationCop/**/TableDataAccessRequiresPermissions*"
---

# AC0031: Table Data Access Requires Permissions

## Purpose

Detects table data access (reads, inserts, modifies, deletes) that is not covered by any declared permission source. Reports an Info-level diagnostic so developers add the `Permissions` property to the containing object.

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
| `Next` is a Read (issue #466) | `Next()` advances the server-side cursor and fetches the next row, so it reads the database in the object that calls it. Permissions do not flow through the call stack, which means a set positioned by another object's `FindSet` still needs `r` in the iterating object. Consequence: AC0031 now reports `repeat … until Rec.Next() = 0` in objects that never call `FindSet` themselves - a true positive that was previously missed. No repeat/until shape analysis: a stand-alone `Next()` / `Next(-1)` counts the same. |
| `DataTransfer` executors require permissions (issue #465) | `CopyFields` needs `r` on the source and `m` on the destination, `CopyRows` needs `r` and `i`; the tables come from the `SetTables(Database::X, Database::Y)` call on the same variable that reaches the executor in flow order, within the same method or trigger body: a later `SetTables` replaces an earlier one, so `SetTables; CopyRows; SetTables; CopyFields` charges each executor only for its own pair, and when branches configure the variable differently the merge is the union of their pairs. `break` paths merge into the state after the loop, and loop bodies are visited twice so a `SetTables` written after an executor still reaches it through the back edge. The pairing lives in `DataTransferTableResolver`. The receiver may be written bare or as `this.<variable>`; both resolve to the same variable, so the two forms match each other. Resolution lives in `RequiredPermissionDetector.TryGetFromDataTransfer` so AC0031 and AC0032 stay symmetric. Detection is a separate `DataTransferOperations` set rather than `MethodOperationMap`, which is keyed on record receivers — see the AC0032 rule doc. |
| Diagnostics anchor on the executor, not on `SetTables` | The executor is the statement that performs the database work and the one a developer would look at; anchoring there also keeps every permission a single transfer needs on one location, which the CodeFix already merges into one `Permissions` entry. |
| Unresolvable `DataTransfer` is silent | When no `SetTables` reaches the executor on any path, a reaching `SetTables` has a table argument that is not a `Database::X` literal, or the receiver is neither a plain identifier nor `this.<variable>`, AC0031 reports nothing. Guessing a table would produce a permission the developer cannot verify; the mirror-image case in AC0032 bails out of the whole object for the same reason. |
| Temporary tables never require permissions (all implementations) | Per Microsoft docs, temporary tables never touch the database, so no permission is required regardless of implementation. Detection is centralized in the `IRecordTypeSymbol.IsTemporary()` / `ITableTypeSymbol.IsTemporary()` extensions (`ALCops.Common.Extensions`). It covers: the `temporary` keyword (`IRecordTypeSymbol.Temporary`), `TableType = Temporary` on the table object (`ITableTypeSymbol.TableType`), report/xmlport `UseTemporary`. `IRecordTypeSymbol.Temporary` reflects ONLY the `temporary` keyword (`Binder` uses `syntax.Temporary.Kind == TemporaryKeyword`), so the `TableType = Temporary` case needs the explicit `TableType` check. XMLPort `UseTemporary` makes the node record `Temporary`, but `GetFromXmlPortNode` must check it explicitly (it does not go through a variable). Page `SourceTableTemporary` is already covered by the page SourceTable exemption. |

## Architecture

The analyzer uses a shared `Permissions/` module in `ALCops.Common`:

```
src/ALCops.Common/
└── Permissions/
    ├── DatabaseOperation.cs                     # Enum: None, Read, Insert, Modify, Delete
    ├── MethodOperationMap.cs                    # Maps record method names → DatabaseOperation
    ├── DataTransferOperations.cs                # DataTransfer executors → (source op, destination op)
    ├── DataTransferTableResolver.cs             # Flow-sensitive SetTables ↔ executor pairing per body
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
- Read: Find, FindFirst, FindLast, FindSet, Get, GetBySystemId, IsEmpty, Count, Next
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

## Known issues

- **IntegerTable** test case is skipped (commented out)
- **Bare implicit calls** (`Modify()` without `Rec.`) inside table objects may not be detected as invocations; use `Rec.Modify()` pattern
- **CalcFields/CalcSums** are not yet covered (out of scope for initial implementation)
- **CodeFix: blank line formatting** When creating a new Permissions property on an object that has no properties, no blank line is inserted between the new property and the first member (trigger/procedure)
- **CodeFix: cross-namespace test** The single-file test framework cannot test qualified table name resolution; both objects must be in the same file
- **DataTransfer with an unresolvable `SetTables`** (none reaching the executor in the same body, or with non-literal table arguments) is not reported at all, so a genuinely missing permission there is missed. The flow walk accepts one imprecision: `exit` does not terminate a path, so state from before an early exit still flows forward. That over-attributes, which is conservative for AC0032 but can make AC0031 ask for a permission the code never needs
- **Inverted rule** (permissions declared but not needed) is planned as a separate diagnostic

## CodeFix: TableDataAccessRequiresPermissionsCodeFixProvider

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
