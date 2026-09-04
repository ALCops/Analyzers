---
paths:
  - "src/ALCops.LinterCop/**/EventSubscriberNamingPattern*"
  - "src/ALCops.LinterCop.Test/Rules/EventSubscriberNamingPattern/**"
---

# LC0098: EventSubscriberNamingPattern

## Purpose

Validates that event subscriber procedure names follow a configurable template derived from the subscribed event's source object name, event name, and optional element name. The template controls both structure and casing in a single configuration line.

Registers `CompilationStartAction` (template parsed once via `TemplateParser`) then `SymbolAction` on `SymbolKind.Method`; main type `EventSubscriberNamingPattern` with `NameBuilder`, sharing `IdentifierNameRenderer` and `AcronymRegistry` from `ALCops.Common`.

## Design decisions

| Decision | Rationale |
|---|---|
| One `SubscriberNamingPattern` string with casing encoded in the placeholder spelling (`{EventSource}`, `{eventSource}`, `{event_source}`, `{event-source}`, raw `{Event Source}`) | Avoids separate CaseStyle properties; the placeholder's own format defines the output style. |
| Default template `{Event Source}_{Event Name}[_{Element Name}]`, with raw tokens emitted verbatim | Exactly the identifier the AL Language extension's "Find Event" generates, so tooling-created subscribers pass without configuration. `EventName` stays PascalCase because AL forbids spaces in event identifiers. |
| Severity Info | Team conventions vary; Info recommends without turning existing subscribers into build warnings. |
| `[...]` optional groups emitted only when every inner token is non-empty | One general mechanism covers any combination of conditional segments (typically the absent element name). |
| Enforcement compares `method.Name` ordinally; the collision guard compares case-insensitively (`SemanticFacts.IsSameName`) | Casing is what the rule enforces, so two spellings differing only in case must differ; collisions model AL identifier semantics, where `AL0018` fires regardless of casing. |
| Strict single preferred rendering; extra accepted variants only when `KnownAcronyms` pins an alternate casing for a source word that already carries uppercase | There is one correct spelling per subscriber in the common case; the opt-in variant set lets teams accept a project casing (`Lcy` beside `LCY`) while the CodeFix keeps suggesting the original-casing form. Cross-product bounded to <= 4 elements; first-seen order keeps the preferred name at index 0. |
| Original casing wins: the registry drives the preferred casing only for all-lowercase source words | Prevents re-casting Microsoft- or partner-owned identifiers (`"VAT Amount"` always renders `VAT`); only `"vat amount"` goes through the registry. |
| Two-letter uppercase words kept as-is (`IO`, `DX`), `ID` always `Id`, camelCase first word fully lowercased | C# capitalization guidelines. |
| `%` in a token value is dropped rather than rendered (`"Line Discount %"` -> `LineDiscount`) | Produces a clean identifier instead of `LineDiscountPct` or an invalid `%`. |
| Word splitter, per-word renderer and acronym registry live in `ALCops.Common` (`IdentifierNameRenderer`, `AcronymRegistry`), not in the analyzer | Reusable by any future rule that turns natural-language input into an identifier; the analyzer keeps only template semantics. |
| `TemplateParser.KnownPlaceholders` maps placeholder strings to `(TokenKind, IdentifierCaseStyle)`; unknown `{...}` sequences are emitted verbatim | Adding `{ObjectType}`/`{ObjectId}` later is a dictionary row, not a grammar change, and existing templates keep parsing. |
| Message always shows the canonical preferred rendering | Predictable, identical to what the CodeFix applies. |
| Independent of LC0092: a subscriber violating both rules receives two diagnostics | Structural template vs character-class pattern with decoupled settings; see `lc0092-naming-pattern.md` for the pattern-side adjustment when the default template yields a quoted identifier. |

## Deliberate non-reports

- Methods that are not event subscribers, whose `[EventSubscriber]` attribute has fewer than 4 arguments, whose event name is empty, or whose source object cannot be resolved (`GetReferencedApplicationObject() == null`, e.g. numeric IDs): the expected name cannot be computed.
- Canonical names longer than `MaxAlIdentifierLength = 120`: renaming would only move the violation to AL304. A survey of W1 (5,510 subscribers, 24,548 publishers) found two such names.
- Collisions: a sibling in `method.ContainingType` already carries the canonical name (case-insensitive), or another subscriber in the same type would compute to it. Two subscribers to one event in a codeunit are legal; renaming both would produce a duplicate-identifier error. Deliberately conservative even when signatures would compile as overloads; self-identity uses `ISymbol.Equals` because AL allows overloading.
- Names matching any accepted variant, and obsolete methods.

## Known issues

- Glued hybrid acronyms such as `UoMSetup` split into `Uo` + `MSetup`; BC field names normally use spaces (`"UoM Setup"`), so only unusual sources are affected. A splitter pre-pass could be added later.
- Nested optional groups are not supported; a `[` inside a group is treated as a literal character.

## SDK facts

- For table trigger events, `Arguments[3].ValueText` returns the string-literal element name without quotes (`MyField`), so it feeds the word splitter directly.

## Test notes

- Custom templates and `KnownAcronyms` are injected as `alcops.json` through a `MemoryFileSystem`.
- `HasFix` tests rename one instance per invocation because `RoslynTestKit.TestCodeFix` does not drive the FixAll pipeline.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `SubscriberNamingPattern` | unset (`{Event Source}_{Event Name}[_{Element Name}]`) | Template of literals, placeholders and `[...]` optional groups; raw tokens are verbatim (quoted when needed), other styles split words on whitespace, `_ - . / & ( ) + %` and Pascal/camel boundaries. |
| `KnownAcronyms` | unset | Merged into `AcronymRegistry.DefaultAcronyms` on a case-insensitive key (user wins, last duplicate wins). Defines the preferred casing for all-lowercase source words and adds an accepted variant for uppercase-carrying ones; never changes the CodeFix suggestion for the latter. |

## CodeFix: EventSubscriberNamingPatternCodeFixProvider

| Decision | Rationale |
|---|---|
| Rename only the declaration identifier via `SyntaxNode.ReplaceToken`; call sites untouched | Subscribers are wired by the `[EventSubscriber]` attribute, not by name; a direct call to a subscriber is rare and left to manual follow-up. |
| `PreferredName` travels in `diagnostic.Properties` and is read via `CodeFixProperties.TryParse` | Avoids reloading settings, re-resolving the referenced object and re-running `NameBuilder` in the fix. |
| Preferred name passed through `QuoteIdentifierIfNeededWithReflection()` before token creation | Kebab-case or otherwise special template output must yield a valid AL identifier. |
| FixAll via `WellKnownFixAllProviders.BatchFixer` (`SupportsFixAll = true`) | Every diagnostic carries its own `PreferredName`, so renames are independent. |
