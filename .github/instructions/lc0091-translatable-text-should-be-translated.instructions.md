---
applyTo: 'src/ALCops.LinterCop/**/TranslatableTextShouldBeTranslated*'
---

# LC0091: TranslatableTextShouldBeTranslated

## Purpose

Checks that all translatable texts (captions, tooltips, labels) in AL code have proper translations in the project's XLIFF files for all target languages. Missing translations cause untranslated UI text in localized Business Central environments.

**References:**
- [BusinessCentral.LinterCop LC0091 discussion](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/804) (original rule and known bug)
- [BusinessCentral.LinterCop LC0091 wiki](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0091)
- [MS Docs: Working with Translation Files](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-work-with-translation-files)
- [MS Docs: XLIFF Translation Support](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-xliff-translation-support)

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `LC0091` |
| Category | Design |
| Severity | Warning |
| Enabled by default | true |
| MessageFormat | `Missing translation for '{0}' in language(s): {1}` |
| Version gate | None |

## Design decisions

These decisions were made during the initial design and should be preserved unless explicitly revisited:

| Decision | Choice | Rationale |
|---|---|---|
| Cop placement | LinterCop (LC prefix) | General code quality rule; reuses same ID as original |
| Severity | Warning | Missing translations directly affect user experience |
| Extension root symbol | `ExtensionObjectFoldingUtilities.GetTranslationRootSymbol` SDK API | Fixes the multi-extension bug in the original rule; correctly handles all cases |
| Translation ID generation | `LanguageFileUtilities.GetLanguageSymbolId` / `GetLabelTextConstLanguageSymbolId` | Stable public SDK APIs using FNV hash |
| ManifestHelper exception handling | Catch `FileNotFoundException` and treat as null manifest | `ManifestHelper.GetManifest` loads `Microsoft.Dynamics.Nav.Analyzers.Common` assembly via reflection; this assembly isn't present in test contexts |
| Null manifest behavior | Proceed with analysis (don't skip) | Tests create minimal compilations without manifests; real projects always have manifests |
| XLIFF caching | Load once in CompilationStartAction | Avoid re-parsing per symbol |
| Settings | `LanguagesToTranslate` array in `alcops.json` | Override semantics: when set, these are the available languages (XLIFF files only parsed for matching languages); when unset, discover from XLIFF files |
| Locked labels | Skip (no diagnostic) | Locked labels are intentionally untranslated |
| Locked detection | Syntax-based via `CommaSeparatedIdentifierEqualsLiteralList` | Label sub-properties aren't exposed as semantic symbols |
| Obsolete symbols | Skip (no diagnostic) | Standard ALCops convention |
| Empty/needs-translation targets | Treated as missing | A trans-unit with empty target or `state="needs-translation"` means no usable translation |
| Scope | All translatable elements in one analyzer | Single XLIFF parse pass serves all symbol types |
| netstandard2.1 | Rule is inert (empty stub) | `ExtensionObjectFoldingUtilities` and `GetLabelTextConstLanguageSymbolId` don't exist in the netstandard2.1 SDK; `GetLanguageSymbolId` has a different internal-only signature. Reflection not viable since the classes/methods are absent, not just internal. |

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

## Known issues and workarounds

### ManifestHelper FileNotFoundException

`ManifestHelper.GetManifest(compilation)` loads `Microsoft.Dynamics.Nav.Analyzers.Common` assembly via reflection. In test contexts (minimal compilations without the full SDK runtime), this assembly isn't available, causing a `FileNotFoundException`. The analyzer catches this and treats it as null manifest.

This is NOT an SDK bug; it's a consequence of running analyzers in a test environment with minimal dependencies. The `CompilationWithAnalyzers` pipeline silently swallows exceptions from analyzer callbacks, making this extremely hard to diagnose without explicit try-catch.

### BoundObjectAccess (shared with LC0095)

The SDK's `OperationExtensions.GetSymbol()` can throw `InvalidCastException` for `BoundObjectAccess` instances. This analyzer doesn't use operation-level analysis, so it's not affected.

## Symbol kinds and translatable properties

| Symbol Kind | Properties Checked |
|---|---|
| Table, TableExtension, XmlPort, Enum, EnumValue, Report, Profile, PermissionSet | Caption |
| Field | Caption, ToolTip |
| Page, PageExtension, RequestPage, RequestPageExtension, Query | Caption + flattened controls (Caption, ToolTip, OptionCaption) + flattened actions (Caption, ToolTip) |
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

## Test coverage

### HasDiagnostic (6 cases)
| Test case | Scenario |
|---|---|
| LocalLabel | Local label variable without translation |
| GlobalLabel | Global label variable without translation |
| TableFieldCaption | Table field with untranslated caption |
| EnumValueCaption | Enum value with untranslated caption |
| PageControlToolTip | Page control with untranslated tooltip |
| ReportLabel | Report label without translation |

### HasDiagnosticWithLanguagesToTranslateNoXliff (1 case)
| Test case | Scenario |
|---|---|
| LocalLabel | LanguagesToTranslate=["da-DK"], no XLIFF files. Settings declare required languages without XLIFF files. |

### HasDiagnosticWithLanguagesToTranslatePartialXliff (1 case)
| Test case | Scenario |
|---|---|
| LocalLabel | LanguagesToTranslate=["da-DK","de-DE"], only da-DK XLIFF. Settings add de-DE as required language. |

### NoDiagnostic (2 cases)
| Test case | Suppression reason |
|---|---|
| LockedLabel | Label with Locked=true |
| LockedReportLabel | Report label with Locked=true |

### NoDiagnosticNoXliff (1 case)
| Test case | Suppression reason |
|---|---|
| NoXliffFiles | No XLIFF files present in file system, no LanguagesToTranslate setting |

## Test infrastructure

Tests use `MemoryFileSystem` (from the SDK) injected via the `FileSystem` property on `AnalyzerTestFixtureConfig` (added to RoslynTestKit). Each HasDiagnostic and NoDiagnostic test provides an empty XLIFF file (with the target language but no trans-units), causing all translatable elements to be reported as missing.

The `CreateFixtureWithEmptyXliff()` helper creates a fixture with:
- A `MemoryFileSystem` containing `Translations/TestApp.da-DK.xlf` with an empty body
- The `TranslatableTextShouldBeTranslated` analyzer

The `CreateFixtureWithoutXliff()` helper creates a fixture with:
- A `MemoryFileSystem` containing no files
- The `TranslatableTextShouldBeTranslated` analyzer

### Settings injection for tests

Tests that need `LanguagesToTranslate` use `ALCopsSettingsProvider.SetSettings("", settings)` to inject settings for the empty workspace path returned by `MemoryFileSystem.GetDirectoryPath()`. A `[TearDown]` calls `ALCopsSettingsProvider.ClearCache()` to avoid cross-test pollution.

## Phase 2 roadmap (not yet implemented)

- **Translated NoDiagnostic test**: Test case where translation exists and is properly translated
- **Multiple languages test**: Test with multiple XLIFF files, some translated, some not
- **Page extension controls test**: Test page extension with added controls
- **Table extension fields test**: Test table extension with added fields
- **Multi-extension folding test**: Test multiple extensions on the same target (the bug fix scenario)
- **ObsoleteField NoDiagnostic test**: Test that obsolete symbols are skipped
