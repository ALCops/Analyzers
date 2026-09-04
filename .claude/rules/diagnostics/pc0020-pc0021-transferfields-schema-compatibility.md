---
paths:
  - "src/ALCops.PlatformCop/**/TransferFields*"
  - "src/ALCops.PlatformCop.Test/Rules/TransferFieldsNameMismatch/**"
  - "src/ALCops.PlatformCop.Test/Rules/TransferFieldsTypeMismatch/**"
---

# PC0020 / PC0021: TransferFieldsSchemaCompatibility

## Purpose

One analyzer reports two diagnostics: PC0020 `TransferFieldsTypeMismatch` (same field ID, incompatible types between source and target table) and PC0021 `TransferFieldsNameMismatch` (same field ID, different field names).

Registers `RegisterOperationAction` on `InvocationExpression` and `RegisterSymbolAction` on `TableExtension`; main type `TransferFieldsSchemaCompatibility`.

## Design decisions

| Decision | Rationale |
|---|---|
| Two analysis paths: the invocation path compares the tables of each `TransferFields` call, the relation path compares extension-added fields of curated table pairs (`TransferFieldsRelations.TableRelations`, e.g. Customer → Contact) | The relation path catches extension fields that collide on well-known BaseApp transfers even when the call site is not in the analyzed module |
| Invocation path reports field-level diagnostics only for pairs outside the curated list, plus one summary diagnostic at the invocation site | Curated pairs are already reported field-by-field on the extension fields themselves by the relation path |
| Skip via `IsRemoved()` (Removed/Moved), not `IsObsolete()` | `ObsoleteState = Pending` tables and fields still participate at runtime and must keep firing |
| Removed fields, calls where either table is removed, and removed relation-path extensions (or extensions of a removed base table) are excluded | Removed fields do not participate in `TransferFields` at runtime ([#148](https://github.com/ALCops/Analyzers/issues/148)); upgrade code transfers from removed tables and a removed target makes the call dead code ([#435](https://github.com/ALCops/Analyzers/issues/435)) |
| Field-level `#pragma warning disable` on either side suppresses the pair | Checked against both fields' syntax directives, so silencing one side is enough |
| Enum→Integer, Code→Text and Integer→BigInteger/Decimal are compatible | Safe implicit conversions performed by the platform |
| `InitPrimaryKeyFields: false` excludes PK fields; a constant `true` `SkipFieldsNotMatchingType` skips the analysis | Mirrors what the runtime transfers for those arguments |
| PC0021 strips mandatory affixes before comparing names, but only for TableExtension fields declared in the current module | AppSource `mandatoryPrefix`/`mandatorySuffix`/`mandatoryAffixes` force extension field names to differ from the paired field ([#436](https://github.com/ALCops/Analyzers/issues/436)). Own tables carry the affix on the object, not the fields, and dependency extensions have unknown affixes |
| Affix matching mirrors the platform (`OrdinalIgnoreCase`, any affix at either end, no word boundary) | Case-sensitive or word-boundary matching was rejected: it diverges from the platform's `VerifyAffixIsUsed` and false-positives on legitimately glued affixes |
| Affix list cached per `Compilation` via `ConditionalWeakTable` | The SDK's `GetMandatoryNameAffixes(Compilation)` re-reads AppSourceCop.json on every call |

## Deliberate non-reports

- Removed or moved fields, tables and extensions: they do not take part in `TransferFields` at runtime.
- Field pairs suppressed by a field-level pragma on either side.
- Type pairs the platform converts implicitly (Enum→Integer, Code→Text, Integer→BigInteger/Decimal).
- PK fields when `InitPrimaryKeyFields` is `false`; any pair when `SkipFieldsNotMatchingType` is a constant `true`.
- PC0021: names that differ only by a mandatory affix on a same-module extension field.

## Known issues

- Platform-parity affix matching can over-strip coincidental substrings (`Customer` with affix `MER` → `Custo`); when the paired same-ID field's core genuinely collides, PC0021 stays silent. Accepted as a narrow false negative for SDK parity ([#436](https://github.com/ALCops/Analyzers/issues/436)).
- Relation-path coverage only applies to the curated `TransferFieldsRelations.TableRelations` list, which carries BC version ranges (`MinVersion`/`MaxVersion`).

## SDK facts

- The compiler suppresses obsolete diagnostics inside `Subtype = Upgrade`/`Install` codeunits (`Binder.IsUpgradeOrInstallCode`), which is why upgrade code referencing removed tables compiles and reaches this analyzer.
- Outside upgrade/install code, in-module references to removed tables are compile errors (`WRN_ERR_ObsoleteStateObsolete` reported as error).
- `GetMandatoryNameAffixes(Compilation)` bypasses the SDK's module-spec config cache and re-reads AppSourceCop.json each call.
- Affix semantics come from `RuleIdentifiersMustHaveValidAffixes.VerifyAffixIsUsed`: any configured affix, either end, `StringComparison.OrdinalIgnoreCase`, no word boundary.

## Test notes

- Invocation-path fixtures for removed tables must be upgrade codeunits (see SDK facts).
- Affix fixtures (`Affix_*`) inject an `AppSourceCop.json` via `MemoryFileSystem`; this requires `Microsoft.Dynamics.Nav.Analyzers.Common.dll` as a `Private=True` reference in the test csproj (ALCops.Common references it with `Private=False`).
- Tableextension fixtures are gated on runtime 13.0 and `this` receiver fixtures on 14.0.
