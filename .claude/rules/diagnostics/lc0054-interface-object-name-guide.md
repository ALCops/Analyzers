---
paths:
  - "src/ALCops.LinterCop/**/InterfaceObjectNameGuide*"
---

# LC0054: InterfaceObjectNameGuide

## Purpose

Interface object names should follow the naming guide: start with a capital `I`, no whitespace
directly after the `I`. When AppSource mandatory affixes are configured, the name may start with
an affix (e.g. `"ABC ICustomer"`); the `I` rule then applies to the remainder after the affix.

## Design decisions

| Decision | Rationale |
|---|---|
| Disabled by default | Tests enable it via a ruleset (`InterfaceObjectNameGuide.ruleset.json`, action `Warning`) |
| Affix logic lives in `ALCops.Common.Helpers.MandatoryAffixes` | Shared with PC0021 (issue #436); loose SDK semantics via `AppSourceCopConfigurationProvider.GetMandatoryNameAffixes` |
| `mandatorySuffix` is a candidate leading affix | Behavior alignment with the SDK's own affix validation (`RuleIdentifiersMustHaveValidAffixes`): every configured value is valid at either end. Previously LC0054 ignored `mandatorySuffix` |
| Affixes resolved per compilation via `CompilationStartAction` closure + `Lazy<string[]>` | Replaced a static mutable `Affixes` field populated by a compilation-start action (cross-compilation data race). The SDK's `GetMandatoryNameAffixes(Compilation)` re-reads AppSourceCop.json, so the `Lazy` caches it per compilation |
| Fast path: name starts with `I` and no whitespace at index 1 (or name is exactly `"I"`) | No affix lookup needed for compliant names. Length guard fixes a pre-existing `IndexOutOfRangeException` for a name of exactly `"I"` |
| Empty-remainder guard after affix strip | `RemoveSpecialCharacters(remainder)` can be empty (e.g. `"ABC -"`); report instead of indexing `[0]` |

## Architecture

1. Skip obsolete symbols; require `IInterfaceTypeSymbol`.
2. Name starts with `I` and is single-character or `name[1]` is not whitespace → compliant, return.
3. `MandatoryAffixes.GetIndexAfterLeadingAffix(name, affixes)` → null ⇒ report.
4. Remainder after the affix has no letters/digits, or its first letter/digit is not `I` ⇒ report.
5. Whitespace directly after the first `I` in the remainder ⇒ report.
