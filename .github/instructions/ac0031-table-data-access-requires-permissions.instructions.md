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

The analyzer is split into a main analyzer class and a shared `Permissions/` module:

```
src/ALCops.ApplicationCop/
├── Analyzers/
│   └── TableDataAccessRequiresPermissions.cs   # Analyzer (callbacks + reporting)
└── Permissions/
    ├── DatabaseOperation.cs                     # Enum: None, Read, Insert, Modify, Delete
    ├── MethodOperationMap.cs                    # Maps method names → DatabaseOperation
    ├── RequiredPermission.cs                    # Record struct holding table + operation + location
    ├── DeclaredPermissionSet.cs                 # Tracks granted ops per table (for future CodeFix)
    └── PermissionResolver.cs                    # Static class: IsCovered(), permission source resolution
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
| `DeclaredPermissionSet` exists but is unused by the analyzer | Prepared for future CodeFix (add missing permissions) and inverted rule (unused permissions) |
| `Rec.Modify()` in table objects detected via explicit Instance path | The AL compiler resolves `Rec.Modify()` with a non-null Instance |
| InherentPermissions attribute parsed via syntax text splitting | The attribute's syntax is well-defined; avoids complex semantic analysis |
| `TestPermissions = Disabled` suppresses diagnostic | Test codeunits with disabled permissions are intentionally testing without permission checks |

## Test coverage

**HasDiagnostic (8 cases):** ProcedureCalls, ProcedureCallsExtended, GetBySystemId, Count, ImplicitSelfCallInTable, XmlPorts, Queries, Reports.
**NoDiagnostic (18 cases):** ProcedureCallsPermissionsProperty, ProcedureCallsPermissionsPropertyFullyQualified, ProcedureCallsInherentPermissionsProperty, ProcedureCallsInherentPermissionsAttribute, PageSourceTable, PageExtensionSourceTable, XmlPortPermissionsProperty, XmlPortInherentPermissions, QueryPermissionsProperty, QueryInherentPermissions, ReportPermissionsProperty, ReportInherentPermissions, XMLPortWithTableElementProps, PermissionsAsObjectId, PermissionPropertyWithPragma, PermissionPropertyWithComment, MultiplePermissionsDifferentType, TestPermissionsDisabled, GetBySystemIdWithPermissions, CountWithPermissions, ImplicitSelfCallWithInherentPermissions.

## Known issues / future work

- **IntegerTable** test case is skipped (commented out)
- **Bare implicit calls** (`Modify()` without `Rec.`) inside table objects may not be detected as invocations; use `Rec.Modify()` pattern
- **CalcFields/CalcSums** are not yet covered (out of scope for initial implementation)
- **CodeFix** to add missing permissions is planned (will use `DeclaredPermissionSet`)
- **Inverted rule** (permissions declared but not needed) is planned as a separate diagnostic

## Related GitHub issues (from LinterCop LC0068)

- #1215: GetBySystemId should be detected as Read
- #1201: TestPermissions = Disabled should suppress diagnostic
- #905: Implicit self-calls (Modify() inside table)
- #923: Pragma in permissions property
- #932: Performance considerations
- #942/#1093/#1207: Extension object limitations
