---
applyTo: 'src/ALCops.PlatformCop/**/TransferFields*'
---

# PC0020 / PC0021: TransferFields schema compatibility

One analyzer (`Analyzers/TransferFieldsSchemaCompatibility.cs`) reports two diagnostics:

| ID | Descriptor | Meaning |
|---|---|---|
| PC0020 | `TransferFieldsTypeMismatch` | Same field ID, incompatible types between source and target table |
| PC0021 | `TransferFieldsNameMismatch` | Same field ID, different field names |

Category: Usage. Severity: Warning. Help URIs: `platformcop/pc0020`, `platformcop/pc0021`.

## Architecture

Two analysis paths:

1. **Invocation path** (`AnalyzeInvocation`, `RegisterOperationAction` on InvocationExpression):
   matches built-in `TransferFields` calls, resolves source table from argument 0 and target table
   from `invocation.Instance` (or the containing table object for `Rec.TransferFields(...)`).
   Reports at field level when the pair is NOT in the curated relation list, and always emits a
   summary diagnostic at the invocation site.
2. **Relation path** (`AnalyzeTableExtension`, `RegisterSymbolAction` on TableExtension): for
   extensions whose base table appears as Source in the curated `TransferFieldsRelations.TableRelations`
   list (e.g. Customer → Contact), compares extension-added fields on both sides and reports at
   field level on both extension fields.

Effective fields = base table fields + all tableextension `AddedFields` across modules (cached per
`Compilation` via `ConditionalWeakTable`). `TransferFields(_, InitPrimaryKeyFields: false)` excludes
PK fields; a constant-`true` third argument (`SkipFieldsNotMatchingType`) suppresses analysis.

## Design decisions

| Decision | Rationale |
|---|---|
| Filter removed **fields** in `BuildFieldMapById` via `IsRemoved()` | Issue #148: removed fields don't participate in TransferFields at runtime |
| Skip invocation analysis when **either** source or target table `IsRemoved()` | Issue #435: upgrade code transfers from removed tables; a removed target makes the call dead code |
| Skip relation-path extensions that are removed or whose base table `IsRemoved()` | Issue #435: non-removed fields on removed tables were still compared |
| Use `IsRemoved()` (Removed/Moved), NOT `IsObsolete()` | `ObsoleteState = Pending` tables/fields still participate at runtime and must keep firing |
| Field-level `#pragma warning disable` honored on either side | Checked via `IsEitherFieldSuppressed` against field syntax directives |
| Enum→Integer, Code→Text, Integer→BigInteger/Decimal treated as compatible | Safe implicit conversions performed by the platform |

## SDK behavior notes

- The AL compiler suppresses obsolete diagnostics entirely inside `Subtype = Upgrade` / `Install`
  codeunits (`Binder.IsUpgradeOrInstallCode`, nav-sdk-source `Binder.cs`). This is why upgrade code
  referencing removed tables compiles cleanly and reaches this analyzer.
- Outside upgrade/install code, in-module references to removed tables are compile errors
  (`WRN_ERR_ObsoleteStateObsolete` reported as error), so invocation-path test fixtures for removed
  tables MUST use an upgrade codeunit.

## Test coverage

Rules folders: `Rules/TransferFieldsTypeMismatch/` and `Rules/TransferFieldsNameMismatch/` in
`ALCops.PlatformCop.Test` (both use the same analyzer, asserting their own diagnostic ID).

**TransferFieldsTypeMismatch HasDiagnostic (17 cases):** InvocationRecWithCodeunit, InvocationRecWithPage, InvocationRecWithTable, InvocationRecWithTablexRec, InvocationSkipFieldsNotMatchingType, InvocationWithInitPrimaryKeyFieldsIsTrue, InvocationWithReturnValue, InvocationWithVarGlobals, InvocationWithVarLocalAndGlobal, InvocationWithVarLocals, InvocationWithVarParam, InvocationWithTableExtension, Invocation_SourceTableObsoleteStatePending, TableExt_Multiple_SameBase, TableExtension, TableExtensionTypeWithType, TableExtensionTypeWithTypeLength.
**TransferFieldsTypeMismatch NoDiagnostic (18 cases):** BuiltInInvocation, Invocation_ObsoleteStateRemoved, Invocation_Pragma, Invocation_SourceTableObsoleteStateRemoved, Invocation_TargetTableObsoleteStateRemoved, InvocationCodeToText, InvocationSkipFieldsNotMatchingType, InvocationWithInitPrimaryKeyFieldsIsFalse, InvocationWithTableExtension, InvocationWithType, InvocationWithTypeLength, TableExt_BothObsoleteStateRemoved, TableExt_ObsoleteStateRemoved, TableExt_Paired_Extension_Pragma, TableExt_Paired_SingleTableExt, TableExt_SourceBaseTableObsoleteStateRemoved, TableExt_TargetBaseTableObsoleteStateRemoved, TableExt_Unpaired.
**TransferFieldsNameMismatch HasDiagnostic (16 cases):** InvocationRecWithCodeunit, InvocationRecWithPage, InvocationRecWithTable, InvocationRecWithTablexRec, InvocationSkipFieldsNotMatchingType, InvocationWithInitPrimaryKeyFieldsIsTrue, InvocationWithReturnValue, InvocationWithVarGlobals, InvocationWithVarLocalAndGlobal, InvocationWithVarLocals, InvocationWithVarParam, InvocationWithTableExtension, Invocation_SourceTableObsoleteStatePending, TableExt_Multiple_SameBase, TableExtension, TableExt_NamespaceCasingMismatch.
**TransferFieldsNameMismatch NoDiagnostic (14 cases):** BuiltInInvocation, Invocation_ObsoleteStateRemoved, Invocation_Pragma, Invocation_SourceTableObsoleteStateRemoved, Invocation_TargetTableObsoleteStateRemoved, InvocationSkipFieldsNotMatchingType, InvocationWithInitPrimaryKeyFieldsIsFalse, InvocationWithTableExtension, TableExt_ObsoleteStateRemoved, TableExt_Paired_Extension_Pragma, TableExt_Paired_SingleTableExt, TableExt_SourceBaseTableObsoleteStateRemoved, TableExt_TargetBaseTableObsoleteStateRemoved, TableExt_Unpaired.

## Known issues

- `TransferFieldsRelations.TableRelations` is a curated static list with BC version ranges
  (`MinVersion`/`MaxVersion`); relation-path coverage only applies to listed pairs.
- No CodeFix.
