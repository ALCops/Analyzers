---
paths:
  - "src/ALCops.LinterCop/**/InterfaceObjectNameGuide*"
  - "src/ALCops.LinterCop.Test/Rules/InterfaceObjectNameGuide/**"
---

# LC0054: InterfaceObjectNameGuide

## Purpose

Interface object names should follow the naming guide: start with a capital `I`, no whitespace
directly after the `I`. When AppSource mandatory affixes are configured, the name may start with
an affix (e.g. `"ABC ICustomer"`); the `I` rule then applies to the remainder after the affix.

Registers `CompilationStartAction` (affixes captured in a `Lazy<string[]>` closure) then `SymbolAction` on `SymbolKind.Interface`; main type `InterfaceObjectNameGuide`.

## Design decisions

| Decision | Rationale |
|---|---|
| Disabled by default (`isEnabledByDefault: false`) | Opt-in naming convention; tests enable it through a ruleset (see Test notes). |
| Affix logic lives in `ALCops.Common.Helpers.MandatoryAffixes`, not in the analyzer | Shared with PC0021 ([#436](https://github.com/ALCops/Analyzers/issues/436)); reuses the SDK's loose semantics via `AppSourceCopConfigurationProvider.GetMandatoryNameAffixes`. |
| `mandatorySuffix` is also a candidate leading affix | Mirrors the SDK's own `RuleIdentifiersMustHaveValidAffixes`: every configured value is valid at either end. |
| Affixes resolved per compilation in a `CompilationStartAction` closure + `Lazy<string[]>` instead of a static field | A static mutable `Affixes` field was a cross-compilation data race; the SDK re-reads AppSourceCop.json on every call, so `Lazy` caches once per compilation. |
| Fast path for names that already start with `I` (or are exactly `"I"`) before any affix lookup | Compliant names need no affix resolution; the length guard removes a former `IndexOutOfRangeException` for a name of exactly `"I"`. |
| Empty remainder after affix strip is reported, not skipped | `RemoveSpecialCharacters(remainder)` can be empty (e.g. `"ABC -"`); reporting is preferred to indexing `[0]` or staying silent. |

## Deliberate non-reports

- Obsolete interface symbols (standard ALCops convention).
- Names whose leading segment is a configured mandatory affix: the `I` rule is applied to the remainder, so `"ABC ICustomer"` is compliant.

## Test notes

- `InterfaceObjectNameGuide.ruleset.json` (action `Warning`) is injected via `RuleSetPath` because the rule is disabled by default.
- Affix cases inject `AppSourceCop.json` through a `MemoryFileSystem`.
