---
paths:
  - "src/ALCops.LinterCop/**/TranslatableTextShouldBeTranslated*"
  - "src/ALCops.LinterCop.Test/Rules/TranslatableTextShouldBeTranslated/**"
---

# LC0091: TranslatableTextShouldBeTranslated

## Purpose

Checks that all translatable texts (captions, tooltips, labels) in AL code have proper translations in the project's XLIFF files for all target languages. Missing translations cause untranslated UI text in localized Business Central environments.

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
| Settings read through `GetSettings(workspacePath, fileSystem)` | Reads `alcops.json` from the `IFileSystem` first, eliminating shared mutable state so settings-dependent tests are parallel-safe. |
| Locked detection is syntactic (`CommaSeparatedIdentifierEqualsLiteralList`) | Label sub-properties are not exposed as semantic symbols. |
| Empty target or `state="needs-translation"` counts as missing | Neither is a usable translation. |
| Analysis views read via reflection (`FlattenedAnalysisViews` / `AddedAnalysisViewsFlattened`) | The properties exist only in the net10.0+ SDK; reflection avoids a compile-time dependency. |
| net8.0-only: netstandard2.1 compiles an empty stub | `ExtensionObjectFoldingUtilities` and `GetLabelTextConstLanguageSymbolId` are absent there and `GetLanguageSymbolId` is internal with a different signature; there is nothing to reflect into. |

## Deliberate non-reports

- Locked labels: intentionally untranslated.
- Obsolete symbols (standard ALCops convention).
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
- The netstandard2.1 SDK has no `ExtensionObjectFoldingUtilities`, no `GetLabelTextConstLanguageSymbolId`, and only an internal `GetLanguageSymbolId(Symbol, Boolean, Boolean)`.

## Test notes

- Every test starts with `RequireMinimumVersion("16.0")` (net8.0-only APIs); analysis-view cases are additionally gated to the net10.0 SDK, and namespace cases enable `TranslationsWithNamespaces` reflectively so the project compiles on SDKs lacking the enum member.
- Fixtures use a `MemoryFileSystem`: an empty `Translations/TestApp.da-DK.xlf` makes every translatable element missing; a no-file variant covers the no-XLIFF exit; `alcops.json` is injected the same way for `LanguagesToTranslate` cases, so no `TearDown`/`ClearCache` is needed.
- Legacy-runtime fixtures inject `app.json` with `"runtime": "5.1"` and `"features": ["TranslationFile"]`; without the feature the analyzer short-circuits and hides the regression. They also need `Microsoft.Dynamics.Nav.Analyzers.Common.dll` in the test bin (`<Reference Private="True">` in the test csproj) for `ManifestHelper`.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `LanguagesToTranslate` | unset | When set, these are the available languages and only matching XLIFF files are parsed; when unset, languages are discovered from the XLIFF files. |
