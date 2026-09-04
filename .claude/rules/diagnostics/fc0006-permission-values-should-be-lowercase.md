---
paths:
  - "src/ALCops.FormattingCop/**/PermissionValuesShouldBeLowercase*"
  - "src/ALCops.FormattingCop.Test/Rules/PermissionValuesShouldBeLowercase/**"
---

# FC0006: PermissionValuesShouldBeLowercase

## Purpose

Detects uppercase permission values (`RIMD`, `Rimd`, `X`) in the `Permissions` property of application objects. Casing has no runtime effect there, but uppercase wrongly suggests direct permissions are granted, a semantic that only exists in permissionset objects and the `InherentPermissions` property. Applies to read/insert/modify/delete (`rimd`) and execute (`x`) values alike; a CodeFix lowercases all permission values in the property.

Registers `RegisterSyntaxNodeAction` on `SyntaxKind.PermissionPropertyValue`; main type `PermissionValuesShouldBeLowercase`.

**References:** [discussion #383](https://github.com/ALCops/Analyzers/discussions/383) (execute coverage confirmed there by rvanbekkum)

## Design decisions

| Decision | Rationale |
|---|---|
| Syntax-node action on the rare `PermissionPropertyValue` node instead of compilation-wide object iteration | No compilation-wide work, and it reaches requestpage properties nested in reports/xmlports, which `GetDeclaredApplicationObjectSymbols()` cannot. |
| One diagnostic per property, reported on the `PropertySyntax` node, not per entry | Matches FC0004; the fix lowercases the whole list and locates the node the same way. |
| Permissionset(extension) exclusion via syntax ancestors, not the semantic model | Deterministic and cheap. |
| `AccessByPermission` excluded by parent property name (denylist) | It shares the node kind but is a UI-visibility mask with no direct/indirect semantics, and uppercase is the Microsoft-documented form there. A denylist preserves behaviour for every other property ([#474](https://github.com/ALCops/Analyzers/issues/474)). |
| Execute (`X`) coverage via a generic any-uppercase check instead of enumerating `RIMD` letters | A future SDK that legalizes non-`tabledata` entries at object level is covered automatically. |

## Deliberate non-reports

- `permissionset` and `permissionsetextension` objects: casing is semantic there (uppercase = direct, lowercase = indirect).
- `AccessByPermission` values (see design decisions).
- Obsolete objects (`ctx.IsObsolete()`).
- `InherentPermissions`: a different node kind (`InherentPermissionsPropertyValue`), so it never reaches the analyzer.
- Non-`tabledata` entries with `X` on object-level `Permissions` (`codeunit Foo = X`): parser error recovery drops them entirely, so the analyzer cannot see the token.

## SDK facts

- The `Permissions` property exists on Codeunit, Table, Page, Report, RequestPage, XmlPort, Query, PermissionSet and PermissionSetExtension (`PropertyInfoLookup`); extension objects do not support it.
- The object-level `Permissions` property only accepts `tabledata` entries; other entries are rejected with `AL0104: Syntax error, 'tabledata' expected`, despite Microsoft Learn examples showing them. Only permissionset objects accept other object types (`ParsePermissionSetPermissionListPropertyValue`).
- Object-level and permissionset permission lists both produce `PermissionPropertyValueSyntax`, hence the explicit permissionset ancestor exclusion.
- `AccessByPermission` also produces `PermissionPropertyValueSyntax` (a single entry via `ObjectParser.ParseAccessByPermission`) on table fields, page fields/parts/actions, pages and reports, so the node kind alone does not scope the rule to `Permissions`.
- The parser uppercases permission values before validation (`GetPermissionValuesTokenWithError`), so any casing parses cleanly.
- `tabledata Foo = X` on object-level `Permissions` parses into the tree but carries `AL0195: Invalid permission kind. Expected: 'RIMD'`; non-`tabledata` entries with `X` are dropped by `SkipBadPermissionSyntaxToken` and never form a `PermissionSyntax` node (verified empirically).

## Test notes

- `X` in scope only occurs in code carrying AL0195, so the `*InDocumentWithErrors` test methods set `ThrowsWhenInputDocumentContainsError = false` (same pattern as `ApplicationCop.Test/TestHelper.cs`).

## CodeFix: PermissionValuesShouldBeLowercaseCodeFixProvider

| Decision | Rationale |
|---|---|
| All uppercase `Permissions` tokens in the property are replaced in one `ReplaceTokens` call with `ToLowerInvariant()` identifiers carrying `WithTriviaFrom(original)` | Entry order, formatting and trivia are preserved; one fix per property matches the one-diagnostic-per-property report. |
| FixAll via `BatchFixer` | One diagnostic per property, no overlapping edits. |
