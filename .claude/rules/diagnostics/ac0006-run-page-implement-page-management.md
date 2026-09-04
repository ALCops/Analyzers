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

## Test notes

- The `HasFixWithNamespace` cases require AL 16.0 (namespaces).

## CodeFix: RunPageImplementPageManagementCodeFixProvider

| Decision | Rationale |
|---|---|
| Reuses an existing local or global `Codeunit "Page Management"` variable; otherwise declares a local `PageManagement` | Avoids a duplicate declaration when the object already has one. |
| Adds `using Microsoft.Utilities;` only when the file declares a `namespace` | Files without a namespace resolve globally; the `using` would be unnecessary. |
| Skips the `using` when one already exists, compared case-insensitively | Prevents duplicates during FixAll or when the user already imported it; AL treats `Microsoft.Utilities` and `microsoft.utilities` as the same name. |
| Inserts the `using` in alphabetical order through the public `CompilationUnitSyntax.WithUsings` instead of reflecting on the SDK's sorter | `NamespaceActionUtilities` is `internal`; replicating the sorted insertion with public API is enough and keeps existing directives ordered. |
