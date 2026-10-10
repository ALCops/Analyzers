---
paths:
  - "src/ALCops.ApplicationCop/**/TableDataAccessRequiresPermissions*"
  - "src/ALCops.ApplicationCop.Test/Rules/TableDataAccessRequiresPermissions/**"
---

# AC0031: Table Data Access Requires Permissions

## Purpose

Detects table data access (reads, inserts, modifies, deletes) that is not covered by any declared permission source. Reports an Info-level diagnostic so developers add the `Permissions` property to the containing object.

Registers `RegisterOperationAction` on `InvocationExpression` and `RegisterSymbolAction` on `ReportDataItem`, `QueryDataItem` and `XmlPortNode`; main type `TableDataAccessRequiresPermissions`, with detection and coverage resolution shared in `ALCops.Common/Permissions` (`RequiredPermissionDetector`, `PermissionResolver`).

## Design decisions

| Decision | Rationale |
|---|---|
| `DatabaseOperation` is a simple enum, not `[Flags]` | Each method call maps to exactly one operation; matches the AppSourceCop pattern. |
| Namespace-aware table matching is primary, object ID matching secondary | Namespaces are the modern AL convention; IDs keep pre-namespace code working. |
| The `[InherentPermissions]` attribute is parsed by splitting its syntax text | The attribute's syntax is well-defined; semantic analysis would add complexity for no gain. |
| `Next` counts as a Read ([#466](https://github.com/ALCops/Analyzers/issues/466)) | `Next()` fetches the next row in the object that calls it, and permissions do not flow through the call stack, so `repeat ... until Rec.Next() = 0` needs `r` even when another object positioned the set with `FindSet`. No repeat/until shape analysis: a stand-alone `Next()` / `Next(-1)` counts the same. |
| `CalcSums` counts as a Read on the receiver ([#578](https://github.com/ALCops/Analyzers/issues/578)) | `CalcSums` is a SQL aggregate over the receiver table, run in the calling object, so it needs `r` there; it is a plain `MethodOperationMap` entry. It stays out of `RecordMethodClassification.ReadMethods` for the same reason as `Next`. |
| `DataTransfer` executors require permissions: `CopyFields` needs `r` on the source and `m` on the destination, `CopyRows` needs `r` and `i` ([#465](https://github.com/ALCops/Analyzers/issues/465)) | The tables come from the `SetTables(Database::X, Database::Y)` call that reaches the executor in flow order within the same method or trigger body (a later `SetTables` replaces an earlier one; branches merge as a union; loop bodies are visited twice so a back-edge `SetTables` is seen). The flow walk is `DataTransferTableResolver`, shared with AC0032 through `RequiredPermissionDetector.TryGetFromDataTransfer` so the two rules stay symmetric; the AC0032 doc holds the pairing details. |
| `DataTransfer` detection lives in a separate `DataTransferOperations` set, not `MethodOperationMap` | `MethodOperationMap` is keyed on record receivers; see the AC0032 doc. |
| Diagnostics anchor on the executor, not on `SetTables` | The executor performs the database work and is where a developer looks; it also keeps every permission one transfer needs on a single location, which the CodeFix merges into one `Permissions` entry. |
| An object-level `AccessByPermission = tabledata X = <mask>` on a page or report covers the chars in its mask for table X (case-insensitive); the `table X = X` execute form and other tables are not covered | The property grants nothing at runtime: it hides the object from users who lack the mask, so an object that declares it is built for users who hold that permission themselves and does not rely on indirect access through the object. The Base App never pairs it with a `Permissions` entry for the same table (report 299 "Delete Invoiced Sales Orders" is the canonical shape), and an Info-level rule must not ask for a redundant declaration next to an explicit one. Only the mask chars count, so `RD` still leaves a `Modify` reported. |

## Deliberate non-reports

- Accesses covered by any permission source: the page's own `SourceTable` (all CRUD), table-level `InherentPermissions`, a method-level `[InherentPermissions]` attribute, or the object's `Permissions` property.
- Accesses on the table named in the object-level `AccessByPermission = tabledata` mask of a page or report, for the chars in that mask (see Design decisions).
- Test codeunits with `TestPermissions = Disabled`: they intentionally run without permission checks.
- `permissionset` and `permissionsetextension` objects: declaring permissions is their purpose, and skipping them saves work.
- Temporary tables in every form (`temporary` keyword, `TableType = Temporary`, report/xmlport `UseTemporary`): per Microsoft docs they never touch the database. Detection is centralized in the `IRecordTypeSymbol.IsTemporary()` / `ITableTypeSymbol.IsTemporary()` extensions. Page `SourceTableTemporary` is already covered by the SourceTable exemption.
- `DataTransfer` executors that cannot be resolved: no `SetTables` reaches the executor on any path, a reaching `SetTables` has a table argument that is not a `Database::X` literal, or the receiver is neither a plain identifier nor `this.<variable>`. Guessing a table would demand a permission the developer cannot verify; AC0032 bails out of the whole object in the mirror case.

## Known issues

- `CalcFields` (FlowField reads) is not traced. The read lands on the CalcFormula *source* table, not the receiver (Microsoft Learn, Security considerations: a user needs read permission on both tables), so adding it to `MethodOperationMap` would charge the wrong table; it needs FlowField source resolution.
- The unresolvable-`DataTransfer` silence means a genuinely missing permission there goes unreported.
- The flow walk lets state flow past `exit`, so an executor can be attributed a table from before an early exit. That is conservative for AC0032 but can make AC0031 ask for a permission the code never needs.
- CodeFix: when the object has no properties at all, the new `Permissions` property is not separated from the first member by a blank line.
- `AccessByPermission` coverage is an intent signal, not a runtime guarantee: the mask is OR-evaluated (with `RIMD` the user needs only one of the chars), the gate only applies when the server's UI Elements Removal setting is `LicenseFile` or `LicenseFileAndUserPermissions` (never with `None`), and the object can still be run from code or a `RunObject` action by a user who lacks the mask. Whether an indirect-only permission passes the UI check is unverified. Field-, part- and action-level `AccessByPermission` are not considered.

## SDK facts

- `IRecordTypeSymbol.Temporary` reflects only the `temporary` keyword (`Binder` checks `syntax.Temporary.Kind == TemporaryKeyword`); `TableType = Temporary` must be read from `ITableTypeSymbol.TableType`.
- XmlPort `UseTemporary` makes the node's record `Temporary`, but nodes are not variables, so the check must be made on the node record itself in `GetFromXmlPortNode`.
- `Permissions` and `AccessByPermission` bind through the same `Binder.BindPermissionPropertyValue`, so both values are a `PermissionPropertyValueSyntax` and `ObjectPermissionCovers` reads either. `AccessByPermission` holds a single entry. Outside permission sets the SDK upper-cases the chars, so casing carries no meaning on application objects.
- `AccessByPermission` is registered as an object-level property only for pages and reports (`ObjectParser.GetPageProperties`, `GetReportProperties`); the other registrations are field, action and part properties, which `IApplicationObjectTypeSymbol.GetProperty` does not see. `tabledata X = X` does not compile (AL0195); `table X = X` parses and binds.
- Receiver forms and self-reference symbol shapes: see `receiver-forms.md`.

## Test notes

- The `this` fixtures are gated on runtime 14.0 and the tableextension/pageextension fixtures on 13.0.
- The `IntegerTable` test case is commented out.
- The single-file test framework cannot exercise cross-namespace qualified table names in the CodeFix; both objects must sit in one file.

## CodeFix: TableDataAccessRequiresPermissionsCodeFixProvider

| Decision | Rationale |
|---|---|
| Creates `Permissions = tabledata {Table} = {op};` when the property is absent, merges the missing char in canonical `rimd` order when the table is already listed, and otherwise appends or inserts a new entry | Covers every shape of an existing property with one entry per table. |
| Skipped on extension objects | Extensions cannot declare `Permissions`. |
| Table names use C#-like namespace resolution (`PermissionTableNameResolver`): simple name when in the same namespace or imported via `using`, qualified name otherwise | Produces the shortest name that still resolves. |
| Added chars follow the existing casing (`RM` becomes `RIM`), lowercase for new entries | Keeps a list consistent with itself. |
| `ApplyFix` re-finds the object syntax by kind and name in the current tree | The BatchFixer applies fixes sequentially; captured node references go stale and produced phantom entries and wrong merges. |
| New entries are inserted at their `PermissionEntryComparer` (FC0004) position, but only when the tabledata group is already in order; otherwise appended | Never introduces a new FC0004 violation while leaving an already unordered list alone. The check is flat, so a `#region`-grouped list whose tabledata entries are not globally ordered gets an append. |
| Multi-line insertion copies an existing separator's trailing trivia onto the new comma via `ReplaceToken` | `SeparatedSyntaxList.Insert()` creates bare separators, and the alternative is the internal `SeparatedSyntaxList` constructor. |
| Inserting at index 0 of a multi-line list re-indents the displaced entry | The first entry sits on the `Permissions =` line with no leading indentation. |
| FixAll uses a separate generic resx title (`TableDataAccessRequiresPermissionsFixAllCodeAction`) | FixAll spans several tables and permissions, so the title cannot name one. |
