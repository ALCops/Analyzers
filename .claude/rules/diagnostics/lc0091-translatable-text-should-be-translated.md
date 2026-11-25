---
paths:
  - "src/ALCops.LinterCop/**/TranslatableTextShouldBeTranslated*"
  - "src/ALCops.LinterCop.Test/Rules/TranslatableTextShouldBeTranslated/**"
---

# LC0091: TranslatableTextShouldBeTranslated

## Purpose

Checks that all translatable texts (captions, tooltips, labels and the other object-level translatable properties) in AL code have proper translations in the project's XLIFF files for all target languages. Missing translations cause untranslated UI text in localized Business Central environments.

Registers `CompilationStartAction` (XLIFF files parsed once into a `TranslationIndex`) then `SymbolAction` on all translatable symbol kinds; main type `TranslatableTextShouldBeTranslated`, net8.0-only (empty stub under `#if NETSTANDARD2_1`).

**References:**
- [BusinessCentral.LinterCop LC0091 discussion](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/804) (original rule and known bug)
- [BusinessCentral.LinterCop LC0091 wiki](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0091)
- [MS Docs: Working with Translation Files](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-work-with-translation-files)
- [MS Docs: XLIFF Translation Support](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-xliff-translation-support)

## Design decisions

| Decision | Rationale |
|---|---|
| LinterCop, same ID as the original; Severity Warning | Reuse eases migration; missing translations directly affect the user experience. |
| Translation root from the SDK's `ExtensionObjectFoldingUtilities.GetTranslationRootSymbol` instead of AppId comparison | The original rule broke when two extensions in the same app extended the same object; the SDK API matches the compiler's own XLIFF generation exactly. |
| Translation IDs computed through runtime reflection over `LanguageFileUtilities` (internal `GetTranslationFileId` on new SDKs, public 2-param methods on older ones) rather than a direct call | A direct call bakes the parameter count into the call site and throws `MissingMethodException` across SDK versions (see SDK facts); reflection also yields namespace-aware IDs. Reimplementing the hashing was rejected as defeating the goal of using stable SDK APIs. |
| Canonical property-name override (`PropertyKind.ToString()`) only when `manifest.Runtime > Spring2020CU1`, unknown runtime treated as current | On runtime <= 5.1 the compiler hashes the source-cased property name, so an unconditional override produced false positives on legacy apps. |
| Old-SDK fallback temporarily rewrites the symbol's private `name` field via reflection under a per-symbol `ConditionalWeakTable` lock | The public 2-param overloads read `symbol.Name` internally and offer no injection seam; the weak table lets lock objects die with their symbols instead of leaking across compilations. Skipped entirely when the override equals `symbol.Name`. |
| `FileNotFoundException` from `ManifestHelper.GetManifest` is caught and a null manifest lets analysis proceed | The assembly it loads is absent in test contexts, and test compilations have no manifest; real projects always do. |
| One analyzer for every translatable element, XLIFF loaded once per compilation | A single parse pass serves all symbol types instead of re-parsing per symbol. |
| Settings read through the compilation snapshot with the callback's cancellation token | Preserves virtual-file lookup and shares the same effective configuration as CM0001 and the other cops, including after a failed HTTP request. |
| Locked detection is syntactic (`CommaSeparatedIdentifierEqualsLiteralList`) | Label sub-properties are not exposed as semantic symbols. |
| Empty target or `state="needs-translation"` counts as missing | Neither is a usable translation. |
| Analysis views read via reflection (`FlattenedAnalysisViews` / `AddedAnalysisViewsFlattened`) | The properties exist only in the net10.0+ SDK; reflection avoids a compile-time dependency. |
| Obsolete gate mirrors the compiler's `LabelWriterVisitor.IsObsolete` instead of the house `IsObsolete()` check: skip only a Moved containing object, and properties whose symbol is Removed (or a field whose table is Removed) | The compiler still emits trans-units for Pending symbols and for labels inside obsolete objects; skipping them hides units the XLIFF tooling will flag as untranslated. Labels are only ever locked by their own `Locked = true`. |
| Object-level properties of page-like kinds (page, page extension, request page, request page extension, query, page customization) come from one static list (`Caption`, `ToolTip`, `InstructionalText`, `AdditionalSearchTerms`, `EntityCaption`, `EntitySetCaption`, `PromotedActionCategories`, `AboutTitle`, `AboutText`) | The compiler writes a trans-unit for every translatable property an object declares, and each kind accepts a different subset; `GetProperty` returns null for the rest, so one list serves every kind without per-kind tables. |
| The fall-through adds `ToolTip` and `AdditionalSearchTerms` for `Report`, and `ProfileDescription` for `Profile` and `ProfileExtension` | Those are the only other object kinds whose `ObjectParser` property table holds a translatable property besides `Caption`. |
| Every flattened control checks one static list (`Caption`, `ToolTip`, `OptionCaption`, `InstructionalText`, `AboutTitle`, `AboutText`) and every flattened action another (`Caption`, `ToolTip`, `AboutTitle`, `AboutText`) | Field, group and part controls, and actions, action groups, custom actions and file-upload actions each accept a different subset; `GetProperty` returns null for the rest. Controls and actions a page extension or request page extension adds arrive through `AddedControlsFlattened` / `AddedActionsFlattened`; properties set inside `modify(...)` blocks are not checked. |
| `PageCustomization` is registered but only its object-level properties are checked | The compiler emits `PageCustomization <name> - Property <name>` trans-units for its `PromotedActionCategories`, `AboutTitle` and `AboutText` (its own translation root). Its modify blocks and synthesized units are a separate scope. |
| net8.0-only: netstandard2.1 compiles an empty stub | `ExtensionObjectFoldingUtilities` and `GetLabelTextConstLanguageSymbolId` are absent there and `GetLanguageSymbolId` is internal with a different signature; there is nothing to reflect into. |

## Deliberate non-reports

- Locked labels: intentionally untranslated.
- Captions and tooltips of Removed tables and fields, including fields inside a Removed table and table-extension fields whose target table is Removed: the compiler drops their trans-units.
- Everything inside a Moved object, and a Moved field in a live table: the compiler does not visit them (`FieldSymbol` and `TableTypeSymbol` are the only symbols that can be Moved).
- Request page properties in a `reportextension`: `InstructionalText`, `AboutTitle` and `AboutText` inside a report extension's `requestpage` fail with AL0124, so there is nothing to report.
- The `Change Category_<name> - Property Caption` trans-units the compiler synthesizes from `PromotedActionCategories` on page extensions and page customizations (and on pages below runtime 10.0): they have no declaration of their own; the `PromotedActionCategories` property they derive from is reported.
- Compilations whose manifest disables translation file generation (`ShouldGenerateTranslationFile()` false), or with no XLIFF files / no target languages after the `LanguagesToTranslate` filter.

## Known issues

- `CompilationWithAnalyzers` silently swallows callback exceptions, which made the `ManifestHelper` `FileNotFoundException` extremely hard to diagnose; the explicit try-catch is the workaround.
- The old-SDK name mutation is not visible to other analyzers, which may read `symbol.Name` on another thread during the swap. Accepted: bounded to one SDK call and only on SDKs predating `GetTranslationFileId`.
- Merged/synthesized property symbols may not expose the private `name` field (`SourcePropertySymbol.name` is the target). The helper then calls the SDK without mutation and accepts a source-cased ID; mitigated because the canonical branch only runs on runtime > 5.1 and modern SDKs use the mutation-free path.

## SDK facts

- `LanguageFileUtilities.GetLanguageSymbolId(ISymbol, IRootTypeSymbol?)` and `GetLabelTextConstLanguageSymbolId` gained an optional `bool useNamespaces = false` in AL 18.0.38.52553 (`TranslationsWithNamespaces` feature). C# compiles optional defaults into the call site, so a DLL built against 18.0.36 throws `MissingMethodException` at runtime on 18.0.38; a local `ProjectReference` build compiles and runs against one SDK and does not reproduce it.
- Namespace-aware trans-unit IDs come from internal `GetTranslationFileId(name, kind, containingSymbol, isMissingCaption, rootSymbol, useNamespaces)` plus public `UseTranslationsWithNamespaces(ISymbol)`: namespace-prefixed, unhashed segments joined by `" - "`, hashed only when longer than 400 chars.
- `GetTranslationFileId` builds the leaf segment from its `name` argument and never reads the leaf symbol's `Name`; the public 2-param overloads read `symbol.Name` internally.
- `SymbolExtensions.GetTranslationName` uses `PropertyKind.ToString()` on runtimes above 5.1 and the source-cased `symbol.Name` on <= 5.1; an unknown runtime resolves via `GetRuntimeVersionOrCurrent`.
- `GetLabelTextConstLanguageSymbolId` forces `SymbolKind.NamedType`, hence `EnumProvider.SymbolKind.NamedType`.
- `ExtensionObjectFoldingUtilities.GetTranslationRootSymbol`: non-extension objects and customizations return themselves; an extension in the same module as its target folds into the target; multiple extensions on one target fold into the one with the lowest ID.
- `ManifestHelper.GetManifest(compilation)` loads `Microsoft.Dynamics.Nav.Analyzers.Common` via reflection.
- `manifest.CompilerFeatures.ShouldGenerateTranslationFile()` is false unless `app.json` lists `"TranslationFile"` in `features` (mapped by `CompilerFeaturesExtensions.GetCompilerFeature`).
- XLIFF generation (`Translation/LabelWriterVisitor.cs`): `IsObsolete` locks a `Field` when it or its `ContainingSymbol` is `IsObsoleteRemoved`, and any other symbol only when it is `IsObsoleteRemoved` itself; it never walks further up and never reads `IsObsoletePending`. `ShouldSymbolBeVisited` returns false for `IsObsoleteMoved`, so a Moved object's whole subtree is never emitted; `PendingMove` is neither skipped nor locked. `VisitVariable` and `VisitReportLabel` pass no lock flag, so a label is locked only by its own `Locked = true`. `XliffOutputter.WriteLabel` drops locked units (or writes them with `translate="no"` under `GenerateLockedTranslations`).
- A property gets a trans-unit when `PropertyKindExtensions.IsTranslatableProperty` is true and its value is a `BoundLabelPropertyValue` (`LabelWriterVisitor.VisitSymbolWithProperties` / `VisitTranslatableProperty`). The `generateMetadata` flag in `ObjectParser` is not the gate: report dataitem `RequestFilterHeading` has `generateMetadata: false` and is still written.
- Object-level `ToolTip` exists from runtime 18.0 on pages (`ObjectParser.GetPageProperties`) and from runtime 15.0 on reports (`GetReportProperties`). Page extensions cannot set it: the page entry has no extension modifiability. The legacy source-cased path (runtime <= 5.1) cannot apply to object ToolTips: `VersionChecker.IsAllowed` rejects the property below its gate, so such an app does not compile.
- `LabelWriterVisitor` overrides only `DefaultVisit`, `VisitReportLabel`, `VisitVariable` and `VisitMethod`; `DefaultVisit` calls `VisitSymbolWithProperties`, which writes every property with `IsTranslatableProperty()`, then recurses into the members. Object-level properties therefore need no per-kind visitor.
- Extension modifiability (`ObjectParser` property tables): page extensions may set `InstructionalText`, `PromotedActionCategories`, `AdditionalSearchTerms`, `AboutTitle` and `AboutText`, not `EntityCaption`/`EntitySetCaption` (API pages only, `PageType(API)` dependency) or the object `ToolTip`. Page customizations may set `PromotedActionCategories`, `AboutTitle` and `AboutText`. A profile extension may set `Caption` and `ProfileDescription`. A report extension's request page accepts none of the request page object properties (AL0124).
- `PromotedActionCategories` is synthesized as a translation item only when the runtime is below ActionsV2 (10.0) and the property is absent (`ShouldBeArtificiallyGenerated`); an explicit value is an ordinary translatable property on every runtime.
- A page or profile extension in the same module as its target writes its trans-units under the target's ID (`al-object-target` set, the note still says `PageExtension`/`ProfileExtension`); the same property set on both target and extension is a compile error (AL0570). A profile's `Caption` is mandatory (AL0341), so a same-module profile extension can never set `Caption`.
- Control and action translatable properties (`ObjectParser` property tables): field controls take `InstructionalText` (runtime 13.0), `AboutTitle` and `AboutText`; group controls `InstructionalText`, `AboutTitle` and `AboutText`; part controls, actions and action groups `AboutTitle` and `AboutText`; custom actions (runtime 10.0) and file-upload actions (runtime 13.0) `AboutTitle` and `AboutText`. All are extension-modifiable. Their trans-units are `<Object> - Control <name> - Property <name>` or `<Object> - Action <name> - Property <name>`, with groups, parts and action groups using the same `Control`/`Action` segment as fields and actions; a control or action added by a same-module page extension folds into the target page's ID like the object-level properties do.
- `Symbol.IsObsoleteRemoved` is virtual `false`, overridden only by `FieldSymbol`, `KeySymbol`, `TableTypeSymbol`, `TableExtensionTypeSymbol` (forwards to `Target`), `RecordTypeSymbol` and `SynthesizedKeySymbol`; pages, controls, actions, enums, reports and the other object kinds can never be Removed. `FieldSymbol` inherits Removed/Pending from the target table in a table extension.
- No public SDK API exposes "locked by the compiler": `LabelWriterVisitor` is internal and `IsObsolete` private, hence the mirror in `IsLockedByCompiler`.
- `GetContainingObjectTypeSymbol()` returns the symbol itself for an object type and null when the containment chain ends without one.
- The netstandard2.1 SDK has no `ExtensionObjectFoldingUtilities`, no `GetLabelTextConstLanguageSymbolId`, and only an internal `GetLanguageSymbolId(Symbol, Boolean, Boolean)`.

## Test notes

- Every test starts with `RequireMinimumVersion("16.0")` (net8.0-only APIs); analysis-view cases are additionally gated to the net10.0 SDK, and namespace cases enable `TranslationsWithNamespaces` reflectively so the project compiles on SDKs lacking the enum member.
- Page object ToolTip fixtures are skipped below SDK 18.0 (`SkipTestIfVersionIsTooLow`, the property's runtime gate); report object ToolTip (runtime 15.0) needs nothing beyond the 16.0 floor. The translated variants use an XLIFF whose trans-unit IDs are `Page <hash> - Property <hash>` and `Report <hash> - Property <hash>`.
- Object-level property fixtures (`PageObject*`, `PageExtensionObject*`, `PageCustomizationObject*`, `RequestPageObject*`, `QueryObject*`, `ReportObjectAdditionalSearchTerms`, `Profile*ObjectDescription`) come in threes: empty XLIFF, `Locked = true`, and translated against `TranslatedObjectPropertiesXliffContent` (namespaced variant: `NamespaceTranslatedPageObjectAboutTitleXliffContent`). Their trans-unit IDs were copied from the `Translations/*.g.xlf` produced by compiling each fixture as a scratch app (`"features": ["TranslationFile"]`, runtime 16.0, only the System symbols; runtime 18.0 plus `TranslationsWithNamespaces` for the namespaced one); never compute the hashes by hand. None needs a gate above the 16.0 floor.
- Control and action property fixtures (`Page*Control*`, `Page*Action*`, `PageExtensionControl*`, `PageExtensionAction*`, `RequestPageControl*`, `RequestPageAction*`) follow the same three-way pattern against `TranslatedControlAndActionPropertiesXliffContent` (namespaced variant: `NamespaceTranslatedPageControlAboutTitleXliffContent`), with IDs generated the same way. Page extension fixtures give the added control or action its own name: the folded ID would otherwise collide with the page fixture's unit in the shared XLIFF. None needs a gate above the 16.0 floor.
- Fixtures use a `MemoryFileSystem`: an empty `Translations/TestApp.da-DK.xlf` makes every translatable element missing; a no-file variant covers the no-XLIFF exit; `alcops.json` is injected the same way for `LanguagesToTranslate` cases, so no `TearDown`/`ClearCache` is needed.
- Obsolete-state fixtures share the empty-XLIFF fixture. A field with `ObsoleteState` must not be in the primary key (AL0693 fails the fixture), so field-level fixtures declare a separate first field. A tableextension of a Removed table compiles, so the `TableExtensionFieldOnObsoleteRemovedTable` fixture guards the inherited-Removed path.
- Legacy-runtime fixtures inject `app.json` with `"runtime": "5.1"` and `"features": ["TranslationFile"]`; without the feature the analyzer short-circuits and hides the regression. They also need `Microsoft.Dynamics.Nav.Analyzers.Common.dll` in the test bin (`<Reference Private="True">` in the test csproj) for `ManifestHelper`.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `LanguagesToTranslate` | unset | When set, these are the available languages and only matching XLIFF files are parsed; when unset, languages are discovered from the XLIFF files. |
