---
applyTo: 'src/ALCops.ApplicationCop/**/TableDataAccessUnusedPermissions*'
---

# AC0032: Unused Permissions

## Purpose

Detects `Permissions` property entries that have no corresponding table data access in the object. This is the inverse of AC0031 (which detects missing permissions). Reports an Info-level diagnostic per unused permission entry. Unused permissions are dead code because the `Permissions` property only applies to the object that directly accesses the table (no call-stack inheritance).

## Diagnostic properties

| Property | Value |
|---|---|
| ID | AC0032 |
| Category | Design |
| Severity | Info |
| Help URI | https://alcops.dev/docs/analyzers/applicationcop/ac0032/ |

Two `DiagnosticDescriptor` instances share the same ID but have different message formats:
- `TableDataAccessUnusedPermissionsEntireEntry`: table not accessed at all
- `TableDataAccessUnusedPermissionsPartialChars`: table accessed but not all declared RIMD chars are needed

## Architecture

Uses `RegisterCompilationAction` (whole-object analysis) because `CompilationStartAnalysisContext` does not expose `RegisterOperationAction`.

```
src/ALCops.ApplicationCop/
├── Analyzers/
│   └── TableDataAccessUnusedPermissions.cs           # Analyzer (CompilationAction)
└── CodeFixes/
    └── TableDataAccessUnusedPermissionsCodeFixProvider.cs  # CodeFix (remove entry / reduce chars / remove property)

src/ALCops.Common/
└── Permissions/
    └── RequiredPermissionDetector.cs   # Shared detection logic (also used by AC0031)
```

### Analysis flow

1. Iterate all objects via `compilation.GetDeclaredApplicationObjectSymbols()`
2. Skip test codeunits with `TestPermissions = Disabled`
3. For each object with a `Permissions` property:
   a. Collect all required permissions (invocations, data items, xmlport nodes)
   b. Get page SourceTable context (implicit RIMD exemption)
   c. For each declared `tabledata` entry, check if the table is accessed
4. Report per-entry diagnostics with properties for the CodeFix

### Key methods

| Method | Purpose |
|---|---|
| `AnalyzeCompilation` | Entry point; iterates objects |
| `CollectRequiredPermissions` | Aggregates all table accesses in the object |
| `CollectFromInvocations` | Walks syntax tree for `InvocationExpression` nodes, bridges to `SemanticModel.GetOperation()` |
| `AnalyzePermissionEntry` | Compares one declared entry against collected required permissions |
| `PermissionMatchesTable` | Matches identifier/qualified/objectId syntax against `ITableTypeSymbol` |

### Threading model

Single-threaded. `RegisterCompilationAction` runs once after compilation completes. No `ConcurrentDictionary` needed (unlike the earlier CompilationStart+End design).

## Design decisions

| Decision | Rationale |
|---|---|
| `RegisterCompilationAction` instead of CompilationStart+End | `CompilationStartAnalysisContext` lacks `RegisterOperationAction` |
| Whole-object analysis (not per-invocation collection) | Simpler, no thread-safety concerns, single pass |
| Two descriptors sharing one ID | Same conceptual rule, different message clarity |
| `PermissionMatchesTable` duplicated from `PermissionResolver` | Avoids coupling; operates on syntax nodes, not resolved symbols |
| Page SourceTable exemption | Pages implicitly need RIMD on their source table |
| Temporary records NOT exempted | Declaring permissions on temp-only tables is dead code |
| Skip permissionset/permissionsetextension objects | These objects declare permissions as their core purpose, not as code-access declarations; flagging them is always a false positive |
| `DeclaredPermissionSet` reused for RIMD tracking | Existing type from AC0031's permission module |

## CodeFix

The `TableDataAccessUnusedPermissionsCodeFixProvider` removes or reduces unused permission entries. Supports FixAll.

### Scenarios

| Scenario | Behavior |
|---|---|
| Entire entry unused, other entries remain | Remove the entry from the permission list |
| Entire entry unused, only entry | Remove the entire `Permissions` property |
| Partial chars unused | Replace permission chars with only the used subset |

### Data passing (analyzer -> CodeFix)

`ImmutableDictionary<string, string>` properties on the diagnostic:
- `TableName`: table name as written in the permission declaration
- `UnusedChars`: chars to remove (e.g., "imd")
- `UsedChars`: chars to keep (e.g., "r"); empty string when entire entry is unused

### Node finding

`syntaxRoot.FindNode(span)` may return `PermissionPropertyValueSyntax` instead of `PermissionSyntax` when there is only one entry (identical spans). The code fix handles this by searching descendants:
```csharp
var permissionNode = node as PermissionSyntax
    ?? node.FirstAncestorOrSelf<PermissionSyntax>()
    ?? node.DescendantNodes().OfType<PermissionSyntax>().FirstOrDefault();
```

### Trivia handling

When removing the first entry from a multi-entry list, `SeparatedSyntaxList.Remove` preserves the second entry's leading trivia (newline + indent). The code fix strips this trivia to avoid `Permissions =\n              tabledata ...` artifacts.

## Test coverage

**HasDiagnostic (8 cases):** EntireEntryUnused, PartialCharsUnused, MultipleUnusedEntries, NoCodeInCodeunit, UnusedOnReport, UnusedOnQuery, UnusedOnXmlPort, TemporaryRecord.
**NoDiagnostic (8 cases):** AllPermissionsUsed, PageSourceTable, TestCodeunitDisabled, ReadUsed, ReportDataItemRead, QueryDataItemRead, PermissionSet, PermissionSetExtension.
**HasFix (3 cases):** RemoveEntireEntry, ReduceChars, RemoveEntireProperty.

## Known limitations

1. **Extension objects**: Cannot see base object code; may flag permissions needed by the base as unused
2. **CalcFields/CalcSums**: Indirect table access through FlowFields is not traced
3. **InherentPermissions overlap**: Table-level `InherentPermissions` may make an object-level entry redundant, but the analyzer does not flag this (different concern from unused)
4. **Cross-object calls**: If codeunit A calls codeunit B, and B accesses a table, A's permission for that table appears unused (correct, because permissions don't flow through the call stack)
