---
paths:
  - "src/ALCops.PlatformCop/**/TransferFields*"
---

# PC0020 / PC0021: TransferFieldsSchemaCompatibility

## Purpose

One analyzer (`Analyzers/TransferFieldsSchemaCompatibility.cs`) reports two diagnostics:

| ID | Descriptor | Meaning |
|---|---|---|
| PC0020 | `TransferFieldsTypeMismatch` | Same field ID, incompatible types between source and target table |
| PC0021 | `TransferFieldsNameMismatch` | Same field ID, different field names |

## Design decisions

| Decision | Rationale |
|---|---|
| Filter removed **fields** in `BuildFieldMapById` via `IsRemoved()` | Issue #148: removed fields don't participate in TransferFields at runtime |
| Skip invocation analysis when **either** source or target table `IsRemoved()` | Issue #435: upgrade code transfers from removed tables; a removed target makes the call dead code |
| Skip relation-path extensions that are removed or whose base table `IsRemoved()` | Issue #435: non-removed fields on removed tables were still compared |
| Use `IsRemoved()` (Removed/Moved), NOT `IsObsolete()` | `ObsoleteState = Pending` tables/fields still participate at runtime and must keep firing |
| Field-level `#pragma warning disable` honored on either side | Checked via `IsEitherFieldSuppressed` against field syntax directives |
| Enum→Integer, Code→Text, Integer→BigInteger/Decimal treated as compatible | Safe implicit conversions performed by the platform |
| Mandatory affixes stripped before name comparison (PC0021 only) | Issue #436: AppSource `mandatoryPrefix`/`mandatorySuffix`/`mandatoryAffixes` force extension field names to differ from the paired field. `AreFieldNamesEquivalent` compares raw names first, then affix-stripped effective names via `MandatoryAffixes.StripAffixes` (loose SDK semantics: any affix, either end; whitespace trimmed after strip) |
| Affix stripping only for TableExtension fields declared in the current module | Fields on own (non-extension) tables carry the affix on the table object, not the fields; dependency extensions have their own unknown affixes. Checked via `field.ContainingSymbol is ITableExtensionTypeSymbol` + `field.Location` in the compilation's syntax-tree paths |
| Coincidental glued affix substrings can over-strip (accepted SDK-parity limitation) | Issue #436 revisit: affix matching mirrors the platform (`RuleIdentifiersMustHaveValidAffixes.VerifyAffixIsUsed`, `StringComparison.OrdinalIgnoreCase`, no word boundary), so a field like `Customer` satisfies glued affix `MER` and strips to `Custo`. When the paired same-ID field's core genuinely collides (e.g. `CustoMER`→`Custo`), PC0021 is suppressed (a narrow false negative). Case-sensitive matching and word-boundary hardening were rejected: they diverge from the platform and cause false positives on legitimately glued affixes |
| Affix list cached per `Compilation` via `ConditionalWeakTable` (`AffixesCache`) | The SDK's `GetMandatoryNameAffixes(Compilation)` re-reads AppSourceCop.json on every call (it bypasses the SDK's module-spec config cache) |

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

## SDK behavior notes

- The AL compiler suppresses obsolete diagnostics entirely inside `Subtype = Upgrade` / `Install`
  codeunits (`Binder.IsUpgradeOrInstallCode`, nav-sdk-source `Binder.cs`). This is why upgrade code
  referencing removed tables compiles cleanly and reaches this analyzer.
- Outside upgrade/install code, in-module references to removed tables are compile errors
  (`WRN_ERR_ObsoleteStateObsolete` reported as error), so invocation-path test fixtures for removed
  tables MUST use an upgrade codeunit.

## Known issues

- Affix fixtures (`Affix_*`) inject an `AppSourceCop.json` via `MemoryFileSystem`; this requires `Microsoft.Dynamics.Nav.Analyzers.Common.dll` as a `Private=True` reference in the test csproj (ALCops.Common references it with `Private=False`).

- `TransferFieldsRelations.TableRelations` is a curated static list with BC version ranges
  (`MinVersion`/`MaxVersion`); relation-path coverage only applies to listed pairs.
- No CodeFix.
