---
paths:
  - "src/ALCops.PlatformCop/**/TranslationFilesMustBeWellFormed*"
  - "src/ALCops.PlatformCop.Test/Rules/TranslationFilesMustBeWellFormed/**"
---

# PC0039: TranslationFilesMustBeWellFormed

## Purpose

Reports each malformed or unreadable translation file for the current app. A broken translation file can prevent translations from being applied across the extension.

Registers `RegisterCompilationAction`; uses the compiler's XLIFF file discovery and parses each file as XML.

## Design decisions

| Decision | Rationale |
|---|---|
| PlatformCop, category Usage, severity Warning, enabled by default | A malformed translation file can make the platform ignore translations; teams can elevate the warning to an error in their ruleset. |
| Check XML well-formedness only | Schema and XLIFF semantic validation are outside this rule's scope. |
| Report every malformed or unreadable discovered file | Reporting all failures avoids fixing one file only to discover another on the next build. |
| Report at `Location.None` and include file path and error details in the message | XLIFF files are external to the AL compilation and cannot be given an AL source span. XML parse errors include line and column. |
| Use a streaming `XmlReader` and prohibit DTD processing | Well-formedness checking does not need a DOM or entity expansion, so streaming avoids loading entire files and DTDs cannot expand entities. |
| Stay silent when translation generation is disabled or no translation directory exists | Preserves compiler-feature and no-translations behavior when a manifest is present. |
| Continue when the manifest is unavailable, using an empty app name | Matches `TranslatableTextShouldBeTranslated`; translation validation should not depend on resolving `app.json`. |
| Report I/O or access errors while discovering translation files | An inaccessible translation directory must not escape the compilation action as an analyzer exception. |
| No version gate | `LanguageFileUtilities.GetXliffLanguageFiles` was present in each installed netstandard2.1, net8.0, and net10.0 SDK assembly. |
| No CodeFix and not configurable | This diagnostic reports malformed external XML; it cannot safely infer the intended correction. |

## Deliberate non-reports

- Well-formed XML that is invalid according to the XLIFF schema or contains semantically invalid translations.
- Missing translation folders or no discovered translation files.
- Translation files when a resolved manifest's compiler features do not enable translation-file generation.

## Known issues

- The diagnostic has no source location; the IDE may associate it with the project/app manifest. The message names the external translation file and gives the parser's line and column when available.
