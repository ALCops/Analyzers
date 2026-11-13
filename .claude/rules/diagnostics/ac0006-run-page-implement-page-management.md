---
paths:
  - "src/ALCops.ApplicationCop/**/RunPageImplementPageManagement*"
  - "src/ALCops.ApplicationCop.Test/Rules/RunPageImplementPageManagement/**"
---

# AC0006: Use "Page Management" codeunit instead of Page.Run

## Purpose

Detects `Page.Run(...)` and `Page.RunModal(...)` calls that can be replaced with the `"Page Management"` codeunit's methods (`PageRun`, `PageRunModal`, `PageRunAtField`). The CodeFix refactors the call and adds the necessary variable declaration and `using` directive.

Registers `RegisterOperationAction` on `InvocationExpression`; main type `RunPageImplementPageManagement`.

## Design decisions

| Decision | Rationale |
|---|---|
| Only a literal `0` or a `Page::MyPage` option access is accepted as the page argument | Any other expression (variables, procedure results) cannot be mapped to a Page Management method statically. |
| For the `Page::X` form the record argument must be one of the well-known BC tables in the `SupportedRecords` dictionary | Page Management only knows how to open cards and lists for those tables; anything else would be a wrong suggestion. |

## Deliberate non-reports

- Calls whose returned `Action` is consumed: the Page Management methods do not return it.
- `Page.EnqueueBackgroundTask`, although it is a `Page` built-in with two or more arguments.
- Calls whose page or record argument falls outside the two decisions above.

## Known issues

- The namespace of `"Page Management"` is looked up by name across modules (compiling module first, then one hit per referenced module). Two codeunits with that name in different namespaces of one module are not told apart, and a same-named codeunit declared by the compiling app wins over the Base Application one, so the using would import the app's namespace. Accepted: such a clash already makes the unqualified `Codeunit` reference ambiguous for the developer.

## Test notes

- The `HasFixWithNamespace` fixtures carry a second `/*EOD*/` document that declares `codeunit 700 "Page Management"` in `namespace Microsoft.Utilities`, so the fix can resolve the namespace from the compilation; the `CodeunitNotResolvable` case deliberately omits it. They are gated at AL 12.0, the first SDK with namespaces.

## CodeFix: RunPageImplementPageManagementCodeFixProvider

| Decision | Rationale |
|---|---|
| Reuses an existing local, then global, `Codeunit "Page Management"` variable; otherwise declares a local `PageManagement` | Avoids a duplicate declaration when the object already has one. |
| The `using` namespace is resolved from the compilation (`GetApplicationObjectNamespaceWithReflection`), not hardcoded | An unresolved codeunit or one in the global namespace gets the variable without a `using`. |
| The `using` is added only when the file declares a `namespace`, and skipped when the file's own namespace is the target or the namespace is already imported (case-insensitive) | Files without a namespace resolve globally; AL treats `Microsoft.Utilities` and `microsoft.utilities` as the same name. |
| Placement, sorting, the directive fallback (append after the last using, or before the first using when a directive such as `#endif` follows the last one) and netstandard2.1 support come from the shared `CompilationUnitSyntaxExtensions.AddUsingDirectiveIfNeeded` | Same helper and rationale as AC0005; see `ac0005-global-language-implement-translation-helper.md` (CodeFix). |
