---
paths:
  - "src/ALCops.LinterCop/**/TranslationFilesMustBeWellFormed*"
  - "src/ALCops.LinterCop.Test/Rules/TranslationFilesMustBeWellFormed/**"
---

# LC0100: TranslationFilesMustBeWellFormed

## Purpose

Reports each malformed or unreadable translation file for the current app. A broken translation file can prevent translations from being applied across the extension.

Registers `RegisterCompilationAction`; uses the compiler's XLIFF file discovery and parses each file as XML.

## Design decisions

| Decision | Rationale |
|---|---|
| LinterCop, category Style, severity Error, enabled by default | Agreed with the rule owner; failed translation loading affects the whole extension. |
| Check XML well-formedness only | Schema and XLIFF semantic validation are outside this rule's scope. |
| Report every malformed or unreadable discovered file | Reporting all failures avoids fixing one file only to discover another on the next build. |
| Report at `Location.None` and include file path and error details in the message | XLIFF files are external to the AL compilation and cannot be given an AL source span. XML parse errors include line and column. |
| Stay silent when translation generation is disabled or no translation directory exists | Preserves the compiler-feature and no-translations behavior of the source rule. |
| No version gate | `LanguageFileUtilities.GetXliffLanguageFiles` was present in each installed netstandard2.1, net8.0, and net10.0 SDK assembly. |
| No CodeFix and not configurable | Decided with the rule owner. |

## Deliberate non-reports

- Well-formed XML that is invalid according to the XLIFF schema or contains semantically invalid translations.
- Missing translation folders or no discovered translation files.
- Translation files when the app's compiler features do not enable translation-file generation.

## Known issues

- The diagnostic has no source location; the IDE may associate it with the project/app manifest. The message names the external translation file and gives the parser's line and column when available.
