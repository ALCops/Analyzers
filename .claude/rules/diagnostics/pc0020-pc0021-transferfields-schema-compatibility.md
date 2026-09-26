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
| Invocation path reports field-level diagnostics only when the call's (source, target) pair is not a curated relation in that direction (source = argument record, target = receiver), plus one summary diagnostic at the invocation site | Curated pairs are already reported field-by-field on the extension fields themselves by the relation path. The lookup is by pair, not by either table alone: a table that appears somewhere in the list says nothing about an unrelated call |
| On a curated pair whose two tables are both Microsoft's, mismatches where both fields are Microsoft-owned are dropped before reporting (no summary, no field-level diagnostic). A field declared in the analyzed module is never Microsoft-owned; a dependency tableextension field is Microsoft-owned only when that extension is Microsoft's | Microsoft ships and supports these transfers with fields that differ by name or type (Purchase Header ↔ Purchase Header Archive fields 151 and 5043, Tracking Specification ↔ Reservation Entry fields 31 and 900); the developer cannot rename Base App fields, and a pragma on the call would also hide collisions on their own extension fields ([#418](https://github.com/ALCops/Analyzers/issues/418)). Both sides must be Microsoft-owned: once one side was added by the developer or a third party the collision is new and outside Microsoft's support. This deliberately differs from the field pragma, which is an explicit user suppression and works from either side |
| Ownership test is `ISymbol.IsMicrosoftObject()` (Common): namespace root `Microsoft` or `System`, else module publisher `Microsoft` | Both namespace roots are reserved for Microsoft (AS0008/PTE0021) and the namespace is already read for the relation lookup; the publisher fallback covers pre-namespace Base App versions. Tables that only share a curated name in a non-Microsoft module are not the Base App tables and stay reported |
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
- Microsoft-owned field pairs on a curated pair of Microsoft tables. Not suppressed: a pair that is curated only in the reverse direction, and non-Microsoft tables that carry a curated name.

## Known issues

- Platform-parity affix matching can over-strip coincidental substrings (`Customer` with affix `MER` → `Custo`); when the paired same-ID field's core genuinely collides, PC0021 stays silent. Accepted as a narrow false negative for SDK parity ([#436](https://github.com/ALCops/Analyzers/issues/436)).
- Relation-path coverage only applies to the curated `TransferFieldsRelations.TableRelations` list, which carries BC version ranges (`MinVersion`/`MaxVersion`). Those ranges are stored but never read: a relation counts as curated on every BC version.
- The suppression of a Microsoft dependency tableextension's field on a curated pair cannot be covered by a fixture (it needs a second module); it is verified by reasoning only.
- A third-party dependency's tableextension field that collides on a curated Microsoft pair is still reported in every app that calls `TransferFields` on that pair, although the consuming app cannot change it.

## SDK facts

- The compiler suppresses obsolete diagnostics inside `Subtype = Upgrade`/`Install` codeunits (`Binder.IsUpgradeOrInstallCode`), which is why upgrade code referencing removed tables compiles and reaches this analyzer.
- Outside upgrade/install code, in-module references to removed tables are compile errors (`WRN_ERR_ObsoleteStateObsolete` reported as error).
- `GetMandatoryNameAffixes(Compilation)` bypasses the SDK's module-spec config cache and re-reads AppSourceCop.json each call.
- Affix semantics come from `RuleIdentifiersMustHaveValidAffixes.VerifyAffixIsUsed`: any configured affix, either end, `StringComparison.OrdinalIgnoreCase`, no word boundary.

## Test notes

- Invocation-path fixtures for removed tables must be upgrade codeunits (see SDK facts).
- Affix fixtures (`Affix_*`) inject an `AppSourceCop.json` via `MemoryFileSystem`; this requires `Microsoft.Dynamics.Nav.Analyzers.Common.dll` as a `Private=True` reference in the test csproj (ALCops.Common references it with `Private=False`).
- Tableextension fixtures are gated on runtime 13.0 and `this` receiver fixtures on 14.0.
- Microsoft ownership is exercised two ways: a `namespace Microsoft.*` fixture for BC24+ tables, and namespace-less tables under a fixture whose `ProjectInfoCustomizer` sets the module publisher to `Microsoft` (`WithProjectDefinition`); the harness default publisher is `Default Publisher`. One AL file is one module with one namespace, so a fixture cannot mix a Microsoft table with a non-Microsoft extension; own-module extension fields are keyed on the declaring module instead.
