---
paths:
  - "src/ALCops.ApplicationCop/**/GlobalLanguageImplementTranslationHelper*"
  - "src/ALCops.ApplicationCop/Analyzers/BuiltInMethodImplementThroughCodeunit.cs"
  - "src/ALCops.ApplicationCop.Test/Rules/GlobalLanguageImplementTranslationHelper/**"
---

# AC0005: GlobalLanguageImplementTranslationHelper

## Purpose

Detects calls to the `GlobalLanguage(...)` built-in that set the language, which should go through the `"Translation Helper"` codeunit (`SetGlobalLanguageById`, `SetGlobalLanguageToDefault`) so the Base Application can restore the language consistently. Reading the language (`GlobalLanguage()` without arguments) is not reported.

Registers `RegisterOperationAction` on `InvocationExpression`; main type `BuiltInMethodImplementThroughCodeunit`, which also reports AC0004 (`Confirm`).

## Design decisions

| Decision | Rationale |
|---|---|
| Only built-in `GlobalLanguage` calls with at least one argument are reported | The getter has no Translation Helper counterpart; a user procedure named `GlobalLanguage` is not the built-in (`MethodKind.BuiltInMethod`). |

## Deliberate non-reports

- `GlobalLanguage()` without arguments (reading the current language).
- Obsolete code (`IsObsolete()`).

## Known issues

- The namespace of `"Translation Helper"` is looked up by name across modules (compiling module first, then one hit per referenced module). Two codeunits with that name in different namespaces of one module are not told apart; harmless for the Base Application codeunit.

## Test notes

- The `HasFixWithNamespace` fixtures carry a second `/*EOD*/` document that declares `codeunit 53 "Translation Helper"` in `namespace System.IO`, so the fix can resolve the namespace from the compilation; the `CodeunitNotResolvable` case deliberately omits it. They are gated at AL 12.0, the first SDK with namespaces.

## CodeFix: GlobalLanguageImplementTranslationHelperCodeFixProvider

| Decision | Rationale |
|---|---|
| A literal `1033` becomes `SetGlobalLanguageToDefault()`; any other argument goes to `SetGlobalLanguageById` | 1033 (en-US) is the application default; other expressions are passed through unchanged. |
| Reuses an existing local, then global, `Codeunit "Translation Helper"` variable; otherwise declares a local `TranslationHelper` | Avoids a duplicate declaration when the object already has one. |
| The `using` namespace is resolved from the compilation (`GetApplicationObjectNamespaceWithReflection`), not hardcoded | The codeunit's namespace is `System.IO` today and can move; an unresolved codeunit or one in the global namespace gets the variable without a `using`. |
| The `using` is added only when the file declares a `namespace`, and skipped when the file's own namespace is the target or the namespace is already imported (case-insensitive) | Files without a namespace resolve globally; AL treats `System.IO` and `system.io` as the same name. |
| Placement mirrors the SDK's internal `NamespaceActionUtilities` (lonely using on its own lines, sorted insertion, index-0 blank-line move), but sorts with `SemanticFacts.NameComparer` instead of ordinal | The SDK sorter is `internal`; the AL name comparer matches CodeCop's case-insensitive using-order rule. |
| When any existing using carries a preprocessor directive the new using is appended after the last one, copying its trailing trivia | Inserting between `#if`/`#endif` lines could place the using inside a conditional block. Same fallback as the SDK. |
| The helper builds the name with `SyntaxFactoryHelper.NamespaceName` instead of `SyntaxFactory.ParseQualifiedName` | `ParseQualifiedName` is missing at the netstandard2.1 SDK floor; avoiding it lets every binary add the `using`. |
| New lines use `Environment.NewLine` | Same as the SDK's own fix; tests normalise line endings. |
| `SupportsFixAll` is false | Two diagnostics in one method would each add the variable and the `using`; enabling FixAll needs a custom `FixAllProvider`. |
