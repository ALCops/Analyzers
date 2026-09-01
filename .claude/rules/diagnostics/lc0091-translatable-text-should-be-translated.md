---
paths:
  - "src/ALCops.LinterCop/**/TranslatableTextShouldBeTranslated*"
---

# LC0091: TranslatableTextShouldBeTranslated

## Purpose

Checks that all translatable texts (captions, tooltips, labels) in AL code have proper translations in the project's XLIFF files for all target languages. Missing translations cause untranslated UI text in localized Business Central environments.

**References:**
- [BusinessCentral.LinterCop LC0091 discussion](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/804) (original rule and known bug)
- [BusinessCentral.LinterCop LC0091 wiki](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0091)
- [MS Docs: Working with Translation Files](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-work-with-translation-files)
- [MS Docs: XLIFF Translation Support](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-xliff-translation-support)

## Design decisions

These decisions were made during the initial design and should be preserved unless explicitly revisited:

| Decision | Rationale |
|---|---|
| Cop placement: LinterCop (LC prefix) | General code quality rule; reuses same ID as original |
| Severity: Warning | Missing translations directly affect user experience |
| Extension root symbol: `ExtensionObjectFoldingUtilities.GetTranslationRootSymbol` SDK API | Fixes the multi-extension bug in the original rule; correctly handles all cases |
| Translation ID generation: `LanguageFileUtilities` via **runtime reflection** (`GetTranslationFileId` on new SDKs, public 2-param `GetLanguageSymbolId` / `GetLabelTextConstLanguageSymbolId` on older SDKs) | The public methods gained an optional `useNamespaces` param in `18.0.38.52553`; C# bakes optional-param call sites at compile time, so a direct call breaks across SDK versions (see Known issues). Reflection picks the right method at runtime and supports namespace-aware IDs. |
| Canonical property-name override: passed only when `manifest.Runtime > RuntimeVersion.Spring2020CU1` (or runtime unknown → treat as "current") | Mirrors `SymbolExtensions.GetTranslationName`: the compiler uses `PropertyKind.ToString()` on new runtimes but source-cased `symbol.Name` on runtime ≤ 5.1. Unconditional override regresses legacy apps (source-cased hash ≠ canonical hash → false positive). Gate is applied on the LinterCop side; `TranslationIdHelper.ComputeTranslationId(nameOverride: null)` falls through to `symbol.Name`. |
| ManifestHelper exception handling: catch `FileNotFoundException` and treat as null manifest | `ManifestHelper.GetManifest` loads `Microsoft.Dynamics.Nav.Analyzers.Common` assembly via reflection; this assembly isn't present in test contexts |
| Null manifest behavior: proceed with analysis (don't skip) | Tests create minimal compilations without manifests; real projects always have manifests |
| XLIFF caching: load once in CompilationStartAction | Avoid re-parsing per symbol |
| Settings: `LanguagesToTranslate` array in `alcops.json` | Override semantics: when set, these are the available languages (XLIFF files only parsed for matching languages); when unset, discover from XLIFF files |
| Settings loading: `GetSettings(workspacePath, fileSystem)` overload | Reads `alcops.json` from `IFileSystem` first, falls back to string-based cache. Eliminates shared mutable state for test isolation. |
| Locked labels: skip (no diagnostic) | Locked labels are intentionally untranslated |
| Locked detection: syntax-based via `CommaSeparatedIdentifierEqualsLiteralList` | Label sub-properties aren't exposed as semantic symbols |
| Obsolete symbols: skip (no diagnostic) | Standard ALCops convention |
| Empty/needs-translation targets: treated as missing | A trans-unit with empty target or `state="needs-translation"` means no usable translation |
| Scope: all translatable elements in one analyzer | Single XLIFF parse pass serves all symbol types |
| AnalysisView access: reflection via `FlattenedAnalysisViews` / `AddedAnalysisViewsFlattened` | Properties only exist in net10.0+ SDK; reflection avoids compile-time dependency |
| netstandard2.1: rule is inert (empty stub) — **net8.0-only** | `ExtensionObjectFoldingUtilities` and `GetLabelTextConstLanguageSymbolId` don't exist in the netstandard2.1 SDK; `GetLanguageSymbolId` has a different internal-only signature. Reflection not viable since the classes/methods are absent, not just internal. |

## Platform availability

This analyzer is **net8.0-only**. On `netstandard2.1`, the class compiles as an empty stub with no `SupportedDiagnostics` and a no-op `Initialize`. The entire class body is wrapped in `#if NETSTANDARD2_1` (stub) / `#else` (full implementation) / `#endif`.

Three SDK APIs required by this analyzer don't exist in the netstandard2.1 build of `Microsoft.Dynamics.Nav.CodeAnalysis`:
- `ExtensionObjectFoldingUtilities` class (absent)
- `LanguageFileUtilities.GetLabelTextConstLanguageSymbolId` method (absent)
- `LanguageFileUtilities.GetLanguageSymbolId` has a different internal-only signature `(Symbol, Boolean, Boolean)` with no `IRootTypeSymbol?` parameter

Reflection was evaluated and rejected: the classes/methods are entirely absent (not just internal), so there is nothing to reflect into. Reimplementing the translation ID generation and extension object folding logic would defeat the design goal of using stable SDK APIs.

## Architecture

### Registration strategy

Uses `RegisterCompilationStartAction` to load and parse XLIFF files once per compilation, then `RegisterSymbolAction` for all relevant symbol kinds.

### Analysis flow

1. **CompilationStartAction**:
   - Get `IFileSystem` from compilation (exit if null)
   - Get `NavAppManifest` via `ManifestHelper.GetManifest` (catch `FileNotFoundException` for test compatibility)
   - Check `CompilerFeatures.ShouldGenerateTranslationFile()` (skip if explicitly disabled)
   - Load `ALCopsSettings` for `LanguagesToTranslate` filter
   - Build `TranslationIndex` from all XLIFF files (exit if no files or no languages)
   - Register `RegisterSymbolAction` for all relevant symbol kinds

2. **AnalyzeSymbol** (per symbol):
   - Skip obsolete symbols
   - Route to type-specific handler based on `SymbolKind`
   - For each translatable property/label:
     - Check locked status (skip if locked)
     - Get translation root via `ExtensionObjectFoldingUtilities.GetTranslationRootSymbol`
     - Generate translation ID via `LanguageFileUtilities`
     - Look up in translation index
     - Report diagnostic with missing language list

### TranslationIndex

Inner class using primary constructor syntax (net8.0-only, since the entire class body is inside `#else` / `#endif`). Stores:
- `AvailableLanguages`: all languages found in XLIFF files (after filter)
- `Index`: `Dictionary<string, HashSet<string>>` mapping trans-unit ID to set of languages that are missing

When a translation ID is not found in the index at all, ALL available languages are considered missing.

## Symbol kinds and translatable properties

| Symbol Kind | Properties Checked |
|---|---|
| Table, TableExtension, XmlPort, Enum, EnumValue, Report, Profile, PermissionSet | Caption |
| Field | Caption, ToolTip |
| Page, PageExtension, RequestPage, RequestPageExtension, Query | Caption + flattened controls (Caption, ToolTip, OptionCaption) + flattened actions (Caption, ToolTip) + flattened analysis views (Caption, ToolTip) |
| LocalVariable, GlobalVariable | Label type only (skip non-Label, skip Locked) |
| ReportLabel | The label itself (skip Locked) |

## Extension object folding

The original BusinessCentral.LinterCop rule had a bug: when multiple extension objects extend the same target within the same app, it used AppId comparison to determine the translation root. This broke when two extensions in the same app both extended the same object.

The fix: use `ExtensionObjectFoldingUtilities.GetTranslationRootSymbol(ISymbol)`, a public SDK API that:
- For non-extension objects: returns the object itself
- For customization objects: returns the extension itself
- For extension objects in same module as target: folds into the target
- For multiple extensions on the same target: picks the extension with the lowest ID as the root

This matches the AL compiler's XLIFF generation behavior exactly.

## Known issues

### ManifestHelper FileNotFoundException

`ManifestHelper.GetManifest(compilation)` loads `Microsoft.Dynamics.Nav.Analyzers.Common` assembly via reflection. In test contexts (minimal compilations without the full SDK runtime), this assembly isn't available, causing a `FileNotFoundException`. The analyzer catches this and treats it as null manifest.

This is NOT an SDK bug; it's a consequence of running analyzers in a test environment with minimal dependencies. The `CompilationWithAnalyzers` pipeline silently swallows exceptions from analyzer callbacks, making this extremely hard to diagnose without explicit try-catch.

### BoundObjectAccess (shared with PC0030)

The SDK's `OperationExtensions.GetSymbol()` can throw `InvalidCastException` for `BoundObjectAccess` instances. This analyzer doesn't use operation-level analysis, so it's not affected.

### SDK 18.0.38.52553: optional-parameter break on translation-ID methods (version drift)

`LanguageFileUtilities.GetLanguageSymbolId(ISymbol, IRootTypeSymbol?)` and `GetLabelTextConstLanguageSymbolId(ISymbol, IRootTypeSymbol?)` gained an optional `bool useNamespaces = false` parameter in AL `18.0.38.52553` (part of the `TranslationsWithNamespaces` compiler feature). C# compiles optional-parameter defaults into the **call site**, so the analyzer DLL — built once against the lowest net10.0 SDK (`18.0.36.x`, 2-param) and, in CI, run against `18.0.38.52553` (binary reference, `ContinuousIntegrationBuild=true`) — referenced a 2-param overload that no longer exists at runtime → `MissingMethodException` → no `LC0091` emitted (all `HasDiagnostic` tests failed; `NoDiagnostic` trivially passed). A local `ProjectReference` build compiles+runs against the same SDK and does **not** reproduce it; it is a compile-vs-runtime version drift.

**Workaround:** compute all translation IDs through runtime reflection (see the `Translation ID computation (SDK version compat)` region in the analyzer). No arity is baked into the call site:
- New SDKs (feature present): internal `GetTranslationFileId(name, kind, containingSymbol, isMissingCaption, rootSymbol, useNamespaces)` + public `UseTranslationsWithNamespaces(ISymbol)`. This produces namespace-aware trans-unit IDs (namespace-prefixed, unhashed segments joined by `" - "`, hashed only when > 400 chars), matching the compiler and avoiding false positives on namespace-enabled projects.
- Older SDKs (methods absent): fall back to the public 2-param methods.

`EnumProvider.SymbolKind.NamedType` was added for the label-const path (mirrors `GetLabelTextConstLanguageSymbolId`, which forces `SymbolKind.NamedType`).

### Runtime ≤ 5.1: canonical property-name override must be gated

The fix for the source-casing false positive (`Tooltip` vs `ToolTip`) forwarded a canonical `nameOverride: propertyKind.ToString()` unconditionally. That regresses apps with `manifest.Runtime ≤ Spring2020CU1` (5.1): on legacy runtimes the compiler hashes the **source-cased** `property.Name` (per `SymbolExtensions.GetTranslationName`), so the analyzer's canonical hash never matched the emitted XLIFF trans-unit ID → false-positive "missing translation".

**Workaround:** compute `useCanonicalPropertyName = manifest?.Runtime is null || manifest.Runtime > RuntimeVersion.Spring2020CU1` at `CompilationStartAction`, thread it into `AnalyzeSymbol`/`AnalyzePageLikeSymbol`/`ReportTranslatableProperty`, and pass `nameOverride: null` (which lets the SDK use `symbol.Name`) on legacy runtimes. Unknown runtime defaults to "current", matching the SDK's `GetRuntimeVersionOrCurrent`.

Regression tests (`HasDiagnosticLegacyRuntime`, `NoDiagnosticLegacyRuntime`) inject an `app.json` with `"runtime": "5.1"` and `"features": ["TranslationFile"]` into the `MemoryFileSystem`. The `features` entry is required — without `TranslationFile` (mapped from the raw string by `CompilerFeaturesExtensions.GetCompilerFeature`), `manifest.CompilerFeatures.ShouldGenerateTranslationFile()` returns `false` and the analyzer short-circuits before hashing anything, hiding the regression. The tests also require `Microsoft.Dynamics.Nav.Analyzers.Common.dll` in the test bin (added as `<Reference Private="True">` in the test csproj) so `ManifestHelper.GetManifest` can resolve the runtime-loaded assembly.

### Canonical-name override mechanism (`TranslationIdHelper.ComputeTranslationId`)

The `nameOverride` parameter is applied differently on new vs old SDKs; both paths are exercised by the same call site in the analyzer:

- **New SDK (`GetTranslationFileId` present)**: the SDK method declares a `name` string parameter and builds the leaf trans-unit segment from that string — the leaf symbol's `Name` is never read. The override is applied by passing `nameOverride ?? symbol.Name` as the `name` argument. No symbol mutation. This is the preferred path.
- **Old SDK fallback (public 2-param `GetLanguageSymbolId` / `GetLabelTextConstLanguageSymbolId`)**: these overloads accept only `(ISymbol, IRootTypeSymbol?)` and read `symbol.Name` internally, so there is no seam to inject an override. The helper temporarily rewrites the symbol's private `name` field via reflection (`FieldInfo.SetValue`) for the duration of the SDK call, then restores it in a `finally`. The field is cached per symbol type in a `ConcurrentDictionary<Type, FieldInfo?>` (`_nameFieldCache`) to keep reflection off the hot path.

**Per-symbol locking:** the mutation window is serialized through a `ConditionalWeakTable<ISymbol, object>` (`_symbolLocks`) so no other call inside this helper reads the symbol during the swap. The weak-table binding lets the lock object be collected together with the symbol, avoiding a static-cache leak across compilations.

**Caveats:**
- Cross-analyzer races are **not** covered: other analyzers do not know about this lock and may read `symbol.Name` on a background thread while the field is temporarily overridden. Impact is bounded to a single method call in the same process and only occurs on SDKs older than the one that added `GetTranslationFileId`. If the override equals `symbol.Name` we skip the mutation entirely; the fast path is a plain reflected call.
- The mutation targets `SourcePropertySymbol.name` (source-declared properties). Merged/synthesized property symbols may not expose the same private field. `_nameFieldCache` stores a nullable `FieldInfo`; when null the helper falls back to invoking the SDK method without mutation, accepting that the resulting ID will match the source-cased name (correct for legacy runtimes, potentially wrong for canonical-name callers on legacy SDKs — mitigated in practice because the canonical-override branch is only taken on runtime > 5.1, and modern SDKs use the mutation-free `GetTranslationFileId` path).

## Test infrastructure

Tests use `MemoryFileSystem` (from the SDK) injected via the `FileSystem` property on `AnalyzerTestFixtureConfig` (added to RoslynTestKit). Each HasDiagnostic and NoDiagnostic test provides an empty XLIFF file (with the target language but no trans-units), causing all translatable elements to be reported as missing.

The `CreateFixtureWithEmptyXliff()` helper creates a fixture with:
- A `MemoryFileSystem` containing `Translations/TestApp.da-DK.xlf` with an empty body
- The `TranslatableTextShouldBeTranslated` analyzer

The `CreateFixtureWithoutXliff()` helper creates a fixture with:
- A `MemoryFileSystem` containing no files
- The `TranslatableTextShouldBeTranslated` analyzer

### Settings injection for tests

Tests that need `LanguagesToTranslate` provide an `alcops.json` file through the `MemoryFileSystem`, matching the pattern used for XLIFF files. The analyzer calls `ALCopsSettingsProvider.GetSettings(workspacePath, fileSystem)`, which reads `alcops.json` from the `IFileSystem` before falling back to the string-based cache. This eliminates shared mutable state, making settings-dependent tests fully parallel-safe.

Two fixture helpers handle settings scenarios:
- `CreateFixtureWithSettings(settingsContent)`: `MemoryFileSystem` with only `alcops.json` (no XLIFF files)
- `CreateFixtureWithXliffAndSettings(settingsContent)`: `MemoryFileSystem` with both `Translations/TestApp.da-DK.xlf` and `alcops.json`

Static byte arrays define the settings JSON:
- `SettingsWithDaDK`: `{"LanguagesToTranslate": ["da-DK"]}`
- `SettingsWithDaDKAndDeDE`: `{"LanguagesToTranslate": ["da-DK", "de-DE"]}`

No `[TearDown]`, `SetSettings`, or `ClearCache` calls are needed. Each test creates its own isolated `MemoryFileSystem` instance.

## Roadmap

- **Translated NoDiagnostic test**: Test case where translation exists and is properly translated
- **Multiple languages test**: Test with multiple XLIFF files, some translated, some not
- **Page extension controls test**: Test page extension with added controls
- **Table extension fields test**: Test table extension with added fields
- **Multi-extension folding test**: Test multiple extensions on the same target (the bug fix scenario)
- **ObsoleteField NoDiagnostic test**: Test that obsolete symbols are skipped
