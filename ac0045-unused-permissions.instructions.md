# AC0045: Unused Permissions (Inverse of AC0031)

## Goal

Create a new diagnostic rule that detects **declared permissions that are not needed** by any code in the object. This is the inverse of AC0031 (TableDataAccessRequiresPermissions), which detects table data access not covered by permissions. AC0045 detects permissions that have no corresponding table data access.

The diagnostic ID AC0045 is a placeholder. Check `DiagnosticIds.cs` for the next available ID before implementation.

## Example

```AL
codeunit 50100 MyCodeunit
{
    Permissions = tabledata Customer = rimd,    // ← AC0045: 'i', 'm', 'd' are unused (only Read is needed)
                  tabledata "Sales Header" = r;  // ← AC0045: no code accesses Sales Header at all

    procedure DoSomething()
    var
        Cust: Record Customer;
    begin
        Cust.FindFirst(); // Read only
    end;
}
```

## Shared infrastructure in ALCops.Common/Permissions/

The AC0031 rewrite moved all permission logic to `ALCops.Common/Permissions/`. This module is designed for reuse by the inverse rule:

| File | Purpose | Reuse for AC0045 |
|---|---|---|
| `DatabaseOperation.cs` | Enum: None, Read, Insert, Modify, Delete | Direct reuse |
| `MethodOperationMap.cs` | Maps AL method names (Find, Insert, Modify, Delete, etc.) to `DatabaseOperation` | Direct reuse: same method-to-operation mapping |
| `RequiredPermission.cs` | Record struct: Table + VariableType + Operation + Location | Direct reuse: collect all required permissions |
| `DeclaredPermissionSet.cs` | Tracks granted RIMD per table | May need extension: track which chars are *used* |
| `PermissionResolver.cs` | `IsCovered()` checks if a required permission is covered by declarations | **Inverse needed**: collect all *declared* permissions, then diff against *required* |
| `PermissionSyntaxHelper.cs` | Syntax helpers for multi-line format, sorted insertion, etc. | Reuse for CodeFix (removing entries) |
| `PermissionTableNameResolver.cs` | Namespace resolution for table names in CodeFix | Reuse for CodeFix |

### PermissionResolver resolution order (AC0031)

AC0031 checks these sources in order when determining if a required permission is covered:

1. **Page SourceTable exemption**: All CRUD on the page's own source table is always exempt
2. **Table-level `InherentPermissions` property**: Permission granted on the table definition itself
3. **Method-level `[InherentPermissions]` attribute**: Permission granted per-method
4. **Object-level `Permissions` property**: The `Permissions = tabledata ...` property

For the inverse rule (AC0045), only source #4 (object-level `Permissions` property) matters. Sources #1-#3 are "implicit" permissions that don't appear in the `Permissions` property, so they can't be "unused" in the same way.

## Architecture approach

### Key difference from AC0031

AC0031 uses **per-invocation analysis** (RegisterOperationAction on each method call). It checks: "does this single call have permission?" and reports immediately if not.

AC0045 requires **whole-object analysis**: collect ALL required permissions across the entire object, then compare against ALL declared permissions. You can't determine if a permission is unused by looking at one call site.

### Recommended analysis strategy

```
1. Register a CompilationAction or SymbolAction on the object level (codeunit, report, xmlport, query)
2. For the object:
   a. Parse the Permissions property → get all declared tabledata entries
   b. Walk all code in the object → collect all required permissions (table + operation)
   c. For each declared permission entry:
      - If the table is not accessed at all → report "entire entry unused"
      - If the table is accessed but some RIMD chars aren't needed → report "specific chars unused"
```

### What to collect as "required permissions"

Reuse the same detection logic as AC0031's four analysis callbacks:

| Source | What it detects | Operation |
|---|---|---|
| `AnalyzeInvocation` | `Record.Find()`, `Record.Insert()`, etc. | Per method name via `MethodOperationMap` |
| `AnalyzeReportDataItem` | Report DataItem referencing a table | Read |
| `AnalyzeQueryDataItem` | Query DataItem referencing a table | Read |
| `AnalyzeXmlPortNode` | XmlPort table node | Read/Insert/Modify based on Direction + Auto* properties |

Consider extracting the "collect required permissions" logic into a shared helper in `ALCops.Common/Permissions/` so both AC0031 and AC0045 can use it. AC0031 currently reports per-invocation, so this may require refactoring AC0031 to share the collection phase, or AC0045 can duplicate the collection independently.

### Exemptions to carry over from AC0031

- **System tables** (ID > 2,000,000,000): Skip
- **Temporary records**: Skip (no real DB access)
- **Test codeunits with `TestPermissions = Disabled`**: Skip
- **Obsolete symbols**: Skip
- **Page SourceTable**: The page's own source table implicitly requires RIMD, so `Permissions` entries for it are NOT unused
- **InherentPermissions**: If a method has `[InherentPermissions(..., Database::"Customer", 'r')]`, that doesn't mean the object-level `Permissions` entry for Customer Read is unused. The object-level entry may still be needed for other methods without the attribute.

### Edge cases to consider

1. **Permissions inherited from base objects**: A page extension might declare permissions for tables accessed in the base page. These are not "unused" but the analyzer can't see the base page's code. Consider skipping extension objects initially (AC0031 CodeFix already skips `ApplicationObjectExtensionSyntax`).

2. **Permissions for indirect access**: A codeunit might declare permissions for tables accessed by called codeunits. The analyzer can only see direct code, not cross-object calls. This is a known limitation. Consider making the severity Info (not Warning) to account for this.

3. **Permission sets referenced via PermissionSet objects**: Out of scope. Only analyze the object's `Permissions` property.

4. **CalcFields/CalcSums**: These access FlowField source tables indirectly. AC0031 doesn't cover these yet either. Track as a known limitation.

5. **Multiple variables of the same table type**: A codeunit might have `Cust1: Record Customer` and `Cust2: Record Customer`. The required operations should be unioned across all variables of the same table.

## Diagnostic properties

| Property | Suggested value |
|---|---|
| ID | AC0045 (placeholder, check `DiagnosticIds.cs`) |
| Category | Design |
| Severity | Info (not Warning, because of indirect access limitations) |
| Help URI | `https://alcops.dev/docs/analyzers/applicationcop/ac0045/` |

### Message format options

Two scenarios need different messages:

1. **Entire entry unused**: `Permission 'tabledata {TableName}' is declared but no code accesses this table.`
2. **Specific chars unused**: `Permission '{UnusedChars}' on 'tabledata {TableName}' is declared but not needed. Only '{UsedChars}' is required.`

## CodeFix

### Scenario 1: Remove entire unused permission entry

Remove the `tabledata "Sales Header" = r` entry from the Permissions list. Reuse `PermissionSyntaxHelper` for format-aware removal (preserve multi-line/single-line style).

### Scenario 2: Reduce permission chars

Change `tabledata Customer = rimd` to `tabledata Customer = r`. Reuse `PermissionSyntaxHelper.NormalizePermissionString()` for canonical ordering.

### Scenario 3: Remove entire Permissions property

If ALL entries are unused, remove the entire `Permissions = ...;` property. This is the simplest case syntactically.

### CodeFix data passing (analyzer → CodeFix)

Use the `CodeFixProperties` record pattern (see `codefix-development.instructions.md`):

```csharp
#if NET8_0_OR_GREATER
private sealed record CodeFixProperties(string TableName, string UnusedChars, string UsedChars)
{
    public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties) { ... }
}
#endif
```

The analyzer should pass:
- `TableName`: The table name as it appears in the Permissions property
- `UnusedChars`: The permission chars to remove (e.g., "imd")
- `UsedChars`: The permission chars to keep (e.g., "r"), empty if entire entry is unused

## Test scenarios

### HasDiagnostic (permission IS unused)

| Test case | Description |
|---|---|
| EntireEntryUnused | `Permissions = tabledata Customer = r` but no code accesses Customer |
| PartialCharsUnused | `Permissions = tabledata Customer = rimd` but only Read used |
| MultipleUnusedEntries | Multiple tabledata entries, some used, some not |
| SingleCharUnused | `tabledata Customer = ri` but only Read used (single 'i' unused) |
| NoCodeInCodeunit | Empty codeunit with Permissions declared |
| UnusedOnReport | Report with permissions for table not in any DataItem |
| UnusedOnXmlPort | XmlPort with permissions for table not accessed |
| UnusedOnQuery | Query with permissions for table not in any DataItem |

### NoDiagnostic (permission IS needed)

| Test case | Description |
|---|---|
| AllPermissionsUsed | All declared RIMD chars have corresponding code |
| PageSourceTable | Permission for the page's own SourceTable (implicitly needed) |
| InherentPermissionsOnTable | Table has InherentPermissions, but object-level entry still used by other paths |
| TemporaryRecord | Temporary record, permissions should be ignored entirely |
| SystemTable | System table (ID > 2B), no diagnostic |
| TestCodeunitDisabled | Test codeunit with TestPermissions = Disabled |
| ReadFromFind | Basic `Record.FindFirst()` matches `= r` |
| InsertUsed | `Record.Insert()` matches `= i` |
| ModifyUsed | `Record.Modify()` matches `= m` |
| DeleteUsed | `Record.Delete()` matches `= d` |
| MultipleMethodsSameTable | Multiple different operations on same table, all covered |
| ReportDataItemRead | Report DataItem counts as Read |
| QueryDataItemRead | Query DataItem counts as Read |
| XmlPortImport | XmlPort import direction uses Insert+Modify |
| XmlPortExport | XmlPort export direction uses Read |

### HasFix (CodeFix scenarios)

| Test case | Description |
|---|---|
| RemoveEntireEntry | Remove one unused tabledata entry from multi-entry list |
| RemoveEntireEntryLastItem | Remove the last entry (handle trailing comma) |
| ReduceChars | Change `= rimd` to `= r` |
| RemoveEntireProperty | All entries unused, remove entire `Permissions = ...;` |
| RemoveEntryMultiLine | Multi-line format, remove one entry, preserve formatting |
| RemoveEntrySingleLine | Single-line format, remove one entry |

## Lessons learned from AC0031

These are patterns and pitfalls discovered during the AC0031 rewrite that apply directly:

### Enum-based comparisons, not string comparisons

Never use `string.Equals(property.Name, "Subtype")`. Always use:
- `EnumProvider.PropertyKind.Subtype` for property kinds
- `GetEnumPropertyValue<CodeunitSubtypeKind>()` for property values
- `EnumProvider.SyntaxKind.*` for syntax kinds
- See `analyzer-development.instructions.md` for the full list

### SeparatedSyntaxList.Insert() trivia bug

`SeparatedSyntaxList<T>.Insert()` creates comma separators with **default trivia** (space only). For multi-line permission lists, the comma needs trailing `\n` trivia. The fix: after `Insert()`, find the separator missing newline trivia and replace it with a template from an existing separator. See `PermissionSyntaxHelper.InsertIntoMultiLineList()`.

This applies to the CodeFix for AC0045 as well if you need to modify entries in-place.

### PropertySyntax has no PropertyKind enum

At the **syntax** level, `PropertySyntax` only has `.Name.Identifier.ValueText` (string). The `PropertyKind` enum is only on `IPropertySymbol` (semantic/symbol level). In CodeFixes that work with syntax trees, use `nameof(PropertyKind.Permissions)` for compile-time safety.

### Test fixture naming

Use names that sort correctly for alphabetical tests. Avoid MyTableOne/Two/Three (Three < Two alphabetically). Use Alpha/Bravo/Charlie or similar.

### ApprovalTests masking real errors

`RoslynTestKit.Verify.CodeAction` tries to load `ApprovalTests` assembly for diff reporting. If missing, `FileNotFoundException` is thrown BEFORE the actual test error. If CodeFix tests fail with assembly errors, temporarily add `ApprovalTests 3.0.0` NuGet to see real diffs, then remove it.

### netstandard2.1 compatibility

- Records need `#if NETSTANDARD2_1` / `#if NET8_0_OR_GREATER` dual definitions
- `System.Text.Json` unavailable on netstandard2.1 (use `Newtonsoft.Json`)
- `System.Collections.Immutable` needs explicit NuGet reference
- See `netstandard21-compatibility.instructions.md`

## Files to create/modify

### New files

| File | Purpose |
|---|---|
| `src/ALCops.ApplicationCop/Analyzers/UnusedPermissions.cs` | Analyzer |
| `src/ALCops.ApplicationCop/CodeFixes/UnusedPermissions.cs` | CodeFix |
| `src/ALCops.ApplicationCop.Test/Rules/UnusedPermissions/UnusedPermissions.cs` | Test class |
| `src/ALCops.ApplicationCop.Test/Rules/UnusedPermissions/HasDiagnostic/*/current.al` | Test fixtures |
| `src/ALCops.ApplicationCop.Test/Rules/UnusedPermissions/NoDiagnostic/*.al` | Test fixtures |
| `src/ALCops.ApplicationCop.Test/Rules/UnusedPermissions/HasFix/*/current.al` + `expected.al` | Test fixtures |
| `.github/instructions/ac0045-unused-permissions.instructions.md` | Rule instruction file |
| `../alcops.dev/content/docs/analyzers/ApplicationCop/AC0045.md` | Documentation page |

### Modified files

| File | Change |
|---|---|
| `src/ALCops.ApplicationCop/DiagnosticIds.cs` | Add `UnusedPermissions = "AC0045"` |
| `src/ALCops.ApplicationCop/DiagnosticDescriptors.cs` | Add descriptor |
| `src/ALCops.ApplicationCop/ALCops.ApplicationCopAnalyzers.resx` | Add Title, MessageFormat, Description, CodeAction strings |
| `ALCops.Common/Permissions/` | Possibly extract shared "collect required permissions" helper |

## Open questions to discuss before implementation

1. **Should the analyzer scan the whole object in one pass, or use per-invocation collection with a final symbol-level check?** A CompilationEndAction could aggregate per-invocation results, but this adds complexity. A single SymbolAction on the object that walks all descendants may be simpler.

2. **How to handle extension objects?** The extension can't see what the base object accesses. Options: skip extensions entirely (simplest), or only flag permissions for tables the extension itself declares variables for.

3. **Should we refactor AC0031 to share the "collect required permissions" logic, or let AC0045 have its own collection?** Sharing reduces duplication but risks coupling. AC0031's per-invocation model is different from AC0045's whole-object model.

4. **Should we report one diagnostic per unused char, or one diagnostic per unused entry?** Per-entry with the full list of unused chars seems cleaner (fewer diagnostics, one CodeFix per entry).

5. **What about permissions for tables accessed via Codeunit.Run or Event subscribers?** These are indirect and the analyzer can't trace them. Accept this as a known limitation and use Info severity.
