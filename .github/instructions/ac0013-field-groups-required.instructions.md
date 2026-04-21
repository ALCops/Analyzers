---
applyTo: 'src/ALCops.ApplicationCop/**/FieldGroupsRequired*'
---

# AC0013: FieldGroupsRequired

## Purpose

Checks that tables used by pages define both `Brick` and `DropDown` field groups. These field groups control how records are displayed in list views and lookup dropdowns in Business Central.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `AC0013` |
| Category | Design |
| Severity | Info |
| Enabled by default | false |
| MessageFormat | `Table '{0}' does not have a '{1}' field group.` |
| Version gate | None |
| netstandard2.1 | Full support (no net8.0-only APIs used) |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Architecture | CompilationStart with pre-computed table set | Cross-object analysis (which tables have pages) must be done once, not per-table. |
| Page discovery | `GetDeclaredApplicationObjectSymbols()` filtered for pages | Original syntax tree walk created 7938 semantic models (one per file). Symbol-based query is O(S) with a single call. |
| Setup tables | Excluded | Tables with a single Code PK field named "Primary Key" are setup tables; field groups are not useful. |
| Temporary tables | Conditional | Only flagged if referenced by a page (temporary tables without pages are skipped). |
| Both field groups checked | Brick and DropDown | Both serve different purposes; missing either is a separate diagnostic. |
| Enabled by default | false | Opt-in rule; not all teams enforce field group conventions. |

## Architecture

### Registration strategy

Uses `RegisterCompilationStartAction` to discover which tables are referenced by pages, then `RegisterSymbolAction` on `Table` symbols.

### Performance-critical design

**Problem:** The original `BuildTablesReferencedByPages()` iterated ALL syntax trees (7938 on the Base App), calling `tree.GetRoot()`, `ctx.Compilation.GetSemanticModel(tree)`, and walking descendants for `PageSyntax`. Creating 7938 semantic models was the dominant cost (4.3s).

**Solution:** Single `GetDeclaredApplicationObjectSymbols()` call filtered for `IPageTypeSymbol` with `NavTypeKind.Page`. Reads `RelatedTable` directly from the page symbol. No syntax tree parsing or semantic model creation needed.

### Analysis flow

1. **CompilationStart**: `BuildTablesReferencedByPages()` builds `HashSet<ITableTypeSymbol>` of all tables that have at least one page
2. **PerSymbol (Table)**:
   - Skip obsolete, skip setup tables
   - Skip temporary tables not referenced by pages
   - Check for `Brick` field group (non-empty)
   - Check for `DropDown` field group (non-empty)
   - Report diagnostic for each missing field group

## Test coverage

### HasDiagnostic (commented out)

Tests are commented out with a TODO about `WithRuleSetPath` in RoslynTestKit. The rule is `isEnabledByDefault: false`, so test fixtures don't trigger it without explicit enablement.

| Test case | Scenario |
|---|---|
| BrickIsMissing | Table without Brick field group |
| DropDownIsMissing | Table without DropDown field group |
| TemporaryTable | Temporary table referenced by a page |

### NoDiagnostic (2 cases)

| Test case | Suppression reason |
|---|---|
| HasBrickAndDropDown | Table has both field groups defined |
| TemporaryTable | Temporary table not referenced by any page |
