---
applyTo: 'src/ALCops.LinterCop/**/EventSubscriberNamingPattern*'
---

# LC0098 - EventSubscriberNamingPattern

## Purpose

Validates that event subscriber procedure names follow a configurable template derived from the subscribed event's source object name, event name, and optional element name. The template controls both structure and casing in a single configuration line.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | LC0098 |
| Category | Naming |
| Severity | Info |
| Has CodeFix | Yes (with FixAll via `WellKnownFixAllProviders.BatchFixer`) |
| Enabled by default | Yes |
| netstandard2.1 | Full support |

Message: `Event subscriber '{0}' does not match the expected name '{1}'`

## Settings

`SubscriberNamingPattern` (string?, default: `null`) in `alcops.json`.  
When `null` or whitespace, the built-in default `{Event Source}_{Event Name}[_{Element Name}]` is used. This mirrors the identifier the AL Language extension's "Find Event" feature generates verbatim (raw source and element names, quoted when they contain characters that require quoting).

`KnownAcronyms` (List<string>?, default: `null`) in `alcops.json`.  
User-configured acronyms merged into the built-in `AcronymRegistry.DefaultAcronyms` list via `AcronymRegistry.Create`. Case-insensitive lookup; user entries override built-ins on identical upper-invariant key. For duplicate user entries with the same upper-invariant key (e.g. `"Http"` + `"HTTP"`), the registry keeps exactly one canonical form and the *last entry wins*. An entry with mixed casing (e.g. `"Acme"` or `"Lcy"`) serves two purposes: (1) it *defines the preferred casing* when the source word is all-lowercase (`"acme product"` → `AcmeProduct`), and (2) it *adds an accepted variant* alongside the original casing when the source word already carries uppercase (subscriber to `...LCY` → both `...LCY` and `...Lcy` are accepted; the CodeFix still suggests `...LCY`).

## Template syntax

### Token placeholders

The placeholder name determines the token; the casing of the name determines the output style:

| Placeholder | Token | Output style |
|---|---|---|
| `{Event Source}` / `{Event Name}` / `{Element Name}` | respective token | Raw (verbatim, no word splitting / no casing transform) |
| `{EventSource}` / `{EventName}` / `{ElementName}` | respective token | PascalCase |
| `{eventSource}` / `{eventName}` / `{elementName}` | respective token | camelCase |
| `{event_source}` / `{event_name}` / `{element_name}` | respective token | snake_case |
| `{event-source}` / `{event-name}` / `{element-name}` | respective token | kebab-case |

Literal characters between placeholders are emitted as-is (e.g. `_`, `-`, `On`). When a raw token expands to a value that contains characters requiring AL identifier quoting (spaces, `.`, `/`, `&`, `%`, ...), the analyzer's suggested name and the CodeFix output wrap the final identifier in double-quotes.

### Optional groups `[...]`

Content wrapped in `[...]` is emitted **only when all token placeholders inside it resolve to a non-empty value**. This handles the common case where the element name is sometimes absent:

```
{Event Source}_{Event Name}[_{Element Name}]
```

- ElementName = `` → `Sales Header_OnAfterDeleteEvent` (quoted as `"Sales Header_OnAfterDeleteEvent"`)
- ElementName = `No.` → `Sales Header_OnAfterDeleteEvent_No.` (quoted as `"Sales Header_OnAfterDeleteEvent_No."`)

Nested optional groups are not supported; a `[` inside a group is treated as a literal character.

### Word splitting and rendering

Raw-style tokens (`{Event Source}`, `{Event Name}`, `{Element Name}`) are emitted **verbatim**: the input string flows through unchanged with no word splitting, no casing transform, and no acronym-aware rendering. This is the mode used by the built-in default so the analyzer aligns with the identifier produced by the AL Language extension's "Find Event" feature.

All other styles (Pascal, camel, snake, kebab) split token values (e.g. `"Sales Header"`, `"G/L Account"`, `"OnAfterDeleteEvent"`) into words:
1. Split on whitespace and the punctuation characters `_ - . / & ( ) + %`
2. Split on PascalCase/camelCase boundaries (uppercase after lowercase, or uppercase-before-lowercase run)

The `%` delimiter drops the character entirely, so `"Line Discount %"` becomes `LineDiscount` (not `LineDiscountPct` or `LineDiscount%`).

Each word is then rendered per the chosen case style. Pascal / camel-non-first positions go through a renderer that produces exactly **one** canonical (preferred) rendering per word and, optionally, one additional accepted variant. The renderer honours **original casing wins** for the preferred form: as long as the source word carries any uppercase character, the word's original casing is preserved (with only the leading character forced to uppercase). Only when the source word is all-lowercase does the registry drive the preferred form, resolving `vat` → `VAT`, `odata` → `OData`, `acme` → `Acme` (user-pinned), etc.

Additionally, when the source word already carries uppercase (e.g. `LCY`) and `KnownAcronyms` contains an entry with a different casing on the same upper-invariant key (e.g. `Lcy`), that registered variant is added to the **accepted set** alongside the original spelling. The analyzer reports only when `method.Name` matches none of the accepted variants; the CodeFix always suggests the preferred (original-wins) name.

| Word | Preferred rendering | Extra accepted variant |
|---|---|---|
| `ID` (case-insensitive) | `Id` | — |
| Two-letter all-uppercase word (`IO`, `DX`, `AG`, ...) | kept as-is (`IO`) | — |
| Word contains any uppercase character (`VAT`, `Sales`, `Http`, `XYZ`) | `EnsureUpperFirst(word)` — original casing wins | registered acronym variant with a different casing (`LCY` + `Lcy` registered → also accepts `Lcy`) |
| Word is all-lowercase and matches a registered acronym (`vat`, `odata`, `acme`) | canonical casing from registry (`VAT`, `OData`, `Acme`) | — |
| Word is all-lowercase and not registered (`amount`, `header`) | first-upper, remainder as-is (`Amount`, `Header`) | — |

The diagnostic reports whenever `method.Name` does not equal **any** name in the accepted set (ordinal comparison). For a template with N words each carrying an accepted variant, the accepted set is the cross product (bounded by BC-realistic identifiers to ≤ 4 elements). The CodeFix always suggests the preferred rendering (element 0 of the accepted set).

camelCase first word is unconditionally fully lowercased per C# convention (`onAfterValidateEvent`, `httpsEndpoint`, `idBadge`).

Snake and kebab styles are all-lowercase.

### Recognized acronyms

Built-in defaults live in `ALCops.Common.Helpers.AcronymRegistry.DefaultAcronyms` (shared infrastructure, case-insensitive lookup). Curated for Business Central vocabulary; includes web protocols where BC field names commonly reference them:

`API`, `BIC`, `BOM`, `CRM`, `CSV`, `EAN`, `EDI`, `ERP`, `FCY`, `FTP`, `GST`, `GTIN`, `GUID`, `HST`, `HTML`, `HTTP`, `HTTPS`, `IBAN`, `IMAP`, `ISBN`, `ISO`, `JSON`, `KPI`, `LCY`, `MPS`, `MRP`, `OData`, `PDF`, `POS`, `REST`, `RFC`, `RFQ`, `RMA`, `SEPA`, `SMTP`, `SOAP`, `SQL`, `UPC`, `URI`, `URL`, `UoM`, `VAT`, `WIP`, `WMS`, `XML`.

Two-letter uppercase abbreviations are covered by the general 2-letter rule and deliberately excluded from the list. Project-specific additions are supplied via the `KnownAcronyms` setting; user entries override the built-in canonical casing on the same case-insensitive key. `KnownAcronyms` serves two purposes: (1) when the source word is all-lowercase it defines the preferred canonical casing, and (2) when the source word already carries uppercase, a registered variant with a different casing on the same upper-invariant key is additionally *accepted* alongside the original spelling. In neither case does user pinning change the preferred/CodeFix suggestion for an uppercase-carrying source: field `"VAT Amount"` renders `VAT` regardless of user pinning; a subscriber to `OnAfterCalcOverdueBalanceLCY` with `KnownAcronyms: ["Lcy"]` accepts both `...BalanceLCY` and `...BalanceLcy` but the CodeFix always suggests `...BalanceLCY`.

## Architecture

Uses `RegisterCompilationStartAction`:
1. Loads `ALCopsSettings.SubscriberNamingPattern` once per compilation
2. Parses the template into a `List<TemplateSegment>` via `TemplateParser.ParseInto` (recursive descent, handles `[...]` groups)
3. Registers `RegisterSymbolAction` for `Method` symbols

Per-method analysis (`AnalyzeMethod`):
1. Skip if obsolete
2. Build the accepted-name set via `TryBuildAcceptedFor` (returns null when the method is not an event subscriber, when the attribute has fewer than 4 arguments, when `GetReferencedApplicationObject()` cannot resolve the source, or when the event name is empty). The set contains the preferred name at index 0 followed by any additional accepted variants (from `KnownAcronyms`).
3. If `method.Name` equals any name in the accepted set (ordinal), return silently
4. **AL304 guard**: if the preferred name exceeds `MaxAlIdentifierLength` (120), return silently — the analyzer must never suggest a rename that would itself violate AL304
5. **Collision guard**: if any sibling method in `method.ContainingType` already carries the preferred name, or if another event subscriber in the same containing type would compute to the same preferred name, return silently — applying the CodeFix would produce a duplicate-identifier compile error
6. Otherwise report the diagnostic with the preferred name as the suggested spelling

### Inner types

| Type | Role |
|---|---|
| `TemplateSegment` (abstract) | Base for parsed template parts |
| `LiteralSegment` | Fixed text |
| `TokenSegment` | Token placeholder with `TokenKind` and `IdentifierCaseStyle` (shared enum from Common) |
| `ConditionalGroupSegment` | Wraps children; emitted only when all inner tokens are non-empty |
| `TemplateParser` (static) | Recursive descent parser; `ParseInto(ref int pos, insideGroup)` |
| `NameBuilder` (static) | Builds the accepted-name set via `BuildAccepted`; delegates per-word casing (and per-word alternates) to `IdentifierNameRenderer` (Common) and consumes `AcronymRegistry` (Common) rather than owning acronym logic. Retains template-specific concerns only: segment traversal, token-value extraction, non-empty guard for conditional groups, cross-product accumulation across segments. |

Analyzer-level statics (outside `NameBuilder`, since they need access to `IMethodSymbol` and don't belong to template rendering):

| Helper | Role |
|---|---|
| `TryBuildAcceptedFor(method, segments, acronyms)` | Returns the accepted-name set (preferred at [0], optional variants after) for `method` if it is a valid event subscriber (has the `EventSubscriber` attribute with ≥ 4 arguments, resolvable source object, non-empty event name); returns `null` otherwise. Used both for the analyzed method (full set) and for sibling probing in `WouldCollideInContainingType` (element [0] only). |
| `WouldCollideInContainingType(method, preferred, segments, acronyms)` | Walks `method.ContainingType.GetMembers()` and returns `true` when any sibling already carries `preferred`, or when another subscriber sibling would compute to `preferred` (comparing preferred-to-preferred; extra accepted variants never create collisions). Name comparison is **case-insensitive** via `SemanticFacts.IsSameName` — AL rejects duplicate method identifiers regardless of casing, so an only-case-different sibling would still cause the CodeFix to produce a duplicate-identifier compile error. Skips self via `ISymbol.Equals` (safe against AL method overloading). |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| One config line | `SubscriberNamingPattern` string with embedded casing | Avoids separate CaseStyle properties; format of placeholder name defines the style |
| Default template alignment | `{Event Source}_{Event Name}[_{Element Name}]` — the exact identifier the AL Language extension's "Find Event" feature generates | Zero-friction adoption: subscribers created via the tooling default already satisfy the rule without extra configuration. `EventName` stays PascalCase because AL disallows spaces in event identifiers, but the equivalent raw `{Event Name}` placeholder is available for template authors who want visual symmetry. |
| Info severity | Report as `Info`, not `Warning` | Team-wide naming conventions vary. Info surfaces the recommendation without escalating to build warnings in code bases that adopt this rule with existing subscribers in place. |
| `[...]` optional groups | Emit only when all inner tokens are non-empty | General mechanism; handles any combination of conditional segments |
| Skip unresolvable objects | `GetReferencedApplicationObject() == null` → skip | Can't compute EventSource name; avoids false positives on numeric IDs |
| Ordinal comparison (rule enforcement) | `StringComparison.Ordinal` when comparing `method.Name` against the accepted-name set | The rule's purpose is to enforce a specific casing; two spellings that differ only in casing must produce a diagnostic. Note: the collision-guard comparisons in `WouldCollideInContainingType` are **case-insensitive** (`SemanticFacts.IsSameName`) because they model AL identifier semantics, not rule enforcement. |
| PascalCase splitting | Split on uppercase-after-lowercase boundaries | Deterministic decomposition of both space-separated and PascalCase names |
| Strict single-form acceptance | Compare `method.Name` against the accepted-name set (`StringComparison.Ordinal`); typically a single preferred rendering, with additional accepted variants only when `KnownAcronyms` pins an alternate casing for an uppercase-carrying source word | Predictable, unambiguous behavior for the common case: there is exactly one "correct" spelling per event subscriber. The opt-in accepted-set escape hatch lets teams recognize a project-specific acronym casing (e.g. `Lcy`) without giving up the original-wins default (`LCY` remains the CodeFix suggestion). |
| AL304 length guard (120 chars) | Suppress the diagnostic when the canonical name exceeds `MaxAlIdentifierLength = 120` | The reviewer's survey of the full W1 codebase (5,510 subscribers, 24,548 publishers) found only two derived names that would exceed 120 chars — vanishingly rare, but real. Silencing the rule (and blocking the CodeFix) in those cases means LC0098 never moves a violation from itself to [AL304](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/diagnostics/diagnostic-al304). |
| Duplicate-name collision guard | Suppress the diagnostic when a sibling in `method.ContainingType` already carries the canonical name (**case-insensitive** via `SemanticFacts.IsSameName`), or when another event subscriber in the same type would compute to the same canonical name | Two subscribers to the same event in one codeunit is a legal, not uncommon pattern; both would rename to the same name and produce a duplicate-identifier compile error. Case-insensitive comparison mirrors AL identifier semantics (`AL0018 duplicate identifier` fires regardless of casing). Self-identity uses `ISymbol.Equals` (AL allows method overloading, so name-based self-filtering is unsafe). The guard is deliberately conservative: any sibling name clash suppresses, even when signatures technically differ and would compile as overloads — renaming into an overload set changes semantics and confuses readers. |
| Acronym rendering (preferred) | **Original casing wins**: registry drives the preferred casing only when the source word is all-lowercase | Prevents the rule from suggesting an identifier whose acronym casing differs from the source object/field name. Field `"VAT Amount"` always renders `VAT` regardless of `KnownAcronyms` content; only lowercase sources like `"vat amount"` go through the registry and pick up `VAT`/`OData`/`UoM`/... canonical casing. This keeps the analyzer aligned with Microsoft- or partner-owned identifier casing and eliminates the previous surprise where user-added `"Vat"` re-cast the well-known BC field name. |
| Two-letter uppercase words | Always kept uppercase (`IO`, `DX`, `AG`) | C# guideline exception for two-letter abbreviations |
| `ID` special case | Always `Id` (Preserve == Normalize) | C# guideline: `ID` is an abbreviation, not an acronym |
| camelCase first word | Unconditionally lowercased (`onAfter`, `https`, `id`) | C# camelCase convention overrides acronym preservation |
| Acronym registry location | Shared class `ALCops.Common.Helpers.AcronymRegistry` (not analyzer-local) | Registry designed as shared infrastructure for future identifier-generating rules; keeps analyzer focused on template semantics |
| Word-splitter and per-word renderer location | Shared class `ALCops.Common.Helpers.IdentifierNameRenderer` (not analyzer-local) | The splitter (`SplitIntoWords`, `SplitPascalCase`), the per-word casing decision tree (camelCase-first lowercasing, ID exception, 2-letter uppercase, original-casing-wins, all-lowercase registry lookup), and the case-style enum are reusable across any rule that turns natural-language input into an identifier. Analyzer only supplies the template segments and consumes `Render`. |
| User acronym overrides | `KnownAcronyms` (a) defines the preferred casing for all-lowercase source words, and (b) adds accepted variants (opt-in, cross-product across template words) when the source word already carries uppercase. In neither case does user pinning override the preferred/CodeFix suggestion for an uppercase-carrying source word. | The preferred name is always driven by "original casing wins" so Microsoft- or partner-owned identifier casing is never re-cast. `KnownAcronyms` is scoped to two situations: canonicalizing lowercase input (e.g. `"vat amount"` → `VatAmount` when `["Vat"]` is pinned) and acknowledging alternate spellings for real-world identifiers where the source already carries uppercase (e.g. accepting `Lcy` alongside `LCY`). The cross-product is bounded by BC-realistic identifiers (≤ 4 elements) and dedup preserves first-seen order so `RenderAccepted(...)[0]` is always the preferred name. |
| Preferred name in message | Always the canonical rendering | Predictable suggestion identical to what the CodeFix will apply |
| CodeFix strategy | Rename only the subscriber's declaration identifier via `SyntaxNode.ReplaceToken`; do not touch call sites | Subscribers are wired by the `[EventSubscriber]` attribute, not by name; the rare case of a direct call to a subscriber is left to manual follow-up |
| CodeFix data flow | Analyzer emits `PreferredName` in `diagnostic.Properties`; CodeFix consumes it via `CodeFixProperties.TryParse` | Avoids reloading settings, re-resolving the referenced object, and re-running `NameBuilder` in the CodeFix |
| FixAll | `WellKnownFixAllProviders.BatchFixer` with `SupportsFixAll = true` | Every diagnostic carries its own `PreferredName`; the batch fixer applies each rename independently |
| Identifier quoting | Preferred name is passed through `QuoteIdentifierIfNeededWithReflection()` before token creation | Handles kebab-case or otherwise special template outputs safely |
| Not net8.0-only | No net8.0-exclusive SDK APIs used | Full netstandard2.1 support without guards |
| Template placeholder registry | `TemplateParser.KnownPlaceholders` maps placeholder strings to `(TokenKind, IdentifierCaseStyle)` pairs; unknown `{...}` sequences are emitted verbatim | Forward-compatible: adding `{ObjectType}` / `{ObjectId}` (a request from the initial review) or any other future token means adding rows to the dictionary and extending `TokenKind` — no grammar change, no breaking rename of the existing placeholders, and templates written today keep parsing under the extended grammar |

## Known issues and workarounds

### String-literal element names (`'MyField'`)
For table trigger events, `Arguments[3].ValueText` returns the string literal value without quotes (e.g., `"MyField"`). This works correctly with the word splitter.

### Hybrid acronyms in glued input
Words like `UoMSetup` written as a single glued identifier are split by the PascalCase rule into `Uo` + `MSetup` and rendered accordingly. BC field names normally use spaces (`"UoM Setup"`), so this affects only unusual sources. A dedicated splitter pre-pass could be added later.

### LC0092 interaction
LC0092 (NamingPattern) also checks `EventSubscriber` methods via the `EventSubscriber` target. By default it requires `^[A-Z]` (starts with uppercase). The new default LC0098 template produces names starting with the raw source-object name (e.g. `Sales Header_OnAfter...`), so a subscriber whose source object begins with an uppercase letter still satisfies both rules. When the source object name would begin with a lowercase or non-letter character, the identifier is emitted quoted (`"my source_..."`) and LC0092's regex sees the opening `"`. Teams that hit this or use a custom LC0098 template starting with a lowercase token should adjust their LC0092 `EventSubscriber` pattern accordingly.

A subscriber that violates both rules receives two independent diagnostics. This is intentional — the two rules cover different concerns (structural template vs. character-class pattern) and their configurations are decoupled. See also LC0092's own instruction file for the reverse cross-reference.

## Test coverage

**HasDiagnostic (2 cases):** WrongName, WrongNameWithElementName.
**HasDiagnosticPascalTemplate (3 cases):** AcronymWrongCasing, IdAbbreviationUppercase, TwoLetterAcronymNormalized.
**NoDiagnostic (9 cases):** DefaultTemplate, WithElementName, RawEventSourceWithSpace, RawElementNameWithSpace, NotASubscriber, DerivedNameExceedsMaxLength, PreferredNameCollidesWithSibling, PreferredNameCollidesWithSiblingDifferentCase, TwoSubscribersSameEventBothMisnamed.
**NoDiagnosticPascalTemplate (5 cases):** AcronymFromKnownListPreserved, IdAbbreviationNormalized, TwoLetterAcronymPreserved, UnknownAcronymPreserved, ElementNameWithPercent.
**HasDiagnosticWithCustomSettings (1 case):** CustomTemplate.
**HasDiagnosticOptionalGroupTemplate (1 case):** OptionalGroupViolation.
**NoDiagnosticOptionalGroupTemplate (2 cases):** OptionalGroupEmptyElement, OptionalGroupWithElement.
**HasDiagnosticWithCustomAcronyms (1 case):** CustomAcronymPinnedRejectsUppercase.
**NoDiagnosticWithCustomAcronyms (1 case):** CustomAcronymPinnedCanonical.
**HasDiagnosticWithOverriddenDefaultAcronym (1 case):** CustomAcronymOverridesDefault.
**NoDiagnosticWithOverriddenDefaultAcronym (1 case):** CustomAcronymOverridesDefault.
**HasDiagnosticWithLcyAcronym (1 case):** AcronymRejectsThirdVariant.
**NoDiagnosticWithLcyAcronym (2 cases):** AcronymAcceptsOriginalCasing, AcronymAcceptsRegistryVariant.
**HasFix (3 cases):** RenameToDefaultTemplate, RenameWithElementName, RenameToRawWithSpaces.
**HasFixPascalTemplate (1 case):** RenameWithAcronym.

Note: `HasFix` cases exercise a single-instance rename per invocation because `RoslynTestKit.TestCodeFix` does not drive Roslyn's FixAll pipeline. FixAll behavior is guaranteed at the API contract level via `SupportsFixAll = true` and `WellKnownFixAllProviders.BatchFixer`.
