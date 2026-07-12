---
applyTo: 'src/ALCops.FormattingCop/**/PermissionValuesShouldBeLowercase*'
---

# FC0006: Permission Values Should Be Lowercase

## Purpose

Detects uppercase permission values (e.g. `RIMD`, `Rimd`) in the `Permissions` property of application objects. Casing has no runtime effect there; both cases grant the same indirect permission, but uppercase wrongly suggests direct permissions are granted (a semantic that only exists in permissionset objects and the `InherentPermissions` property). Provides a CodeFix that lowercases all permission values in the property. Origin: discussion #383.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | FC0006 |
| Category | Style |
| Severity | Info |
| Help URI | https://alcops.dev/docs/analyzers/formattingcop/fc0006/ |

## Architecture

```
src/ALCops.FormattingCop/
├── Analyzers/
│   └── PermissionValuesShouldBeLowercase.cs           # Analyzer (SyntaxNodeAction)
└── CodeFixes/
    └── PermissionValuesShouldBeLowercaseCodeFixProvider.cs  # CodeFix (lowercase tokens)
```

### Analysis flow

1. `RegisterSyntaxNodeAction` on `EnumProvider.SyntaxKind.PermissionPropertyValue` (rare node; cheap pre-filter, covers requestpages nested in reports/xmlports which `GetDeclaredApplicationObjectSymbols()` cannot reach)
2. Skip obsolete symbols via `ctx.IsObsolete()`
3. Skip when any ancestor is a `PermissionSet`/`PermissionSetExtension` object (casing is semantic there: uppercase = direct, lowercase = indirect)
4. Flag when any `PermissionSyntax.Permissions` token text contains an uppercase char
5. Report one diagnostic per property on the `PropertySyntax` node (parent of the value node)

## SDK facts (verified against nav-sdk-source and empirically)

- The `Permissions` property exists on: Codeunit, Table, Page, Report, RequestPage, XmlPort, Query, PermissionSet, PermissionSetExtension (`PropertyInfoLookup`). Extension objects do not support it.
- **The object-level `Permissions` property only accepts `tabledata` entries.** Non-`tabledata` entries (`codeunit X = X` etc.) are rejected with `AL0104: Syntax error, 'tabledata' expected`, despite Microsoft Learn docs showing such examples. Only permissionset objects accept other object types (parsed via `ParsePermissionSetPermissionListPropertyValue`).
- Both object-level and permissionset permission lists produce `PermissionPropertyValueSyntax` (same `SyntaxKind.PermissionPropertyValue`), hence the explicit permissionset ancestor exclusion.
- `InherentPermissions` produces a different node kind (`InherentPermissionsPropertyValue`), so it is naturally excluded by the registration.
- The parser uppercases permission values before validation (`GetPermissionValuesTokenWithError`), so any casing parses cleanly.

## Design decisions

| Decision | Rationale |
|---|---|
| Syntax-node action on `PermissionPropertyValue` | Rare node, no compilation-wide iteration; reaches nested requestpage properties |
| One diagnostic per property, not per entry | Matches FC0004; the fix lowercases the whole list |
| Diagnostic on `PropertySyntax` node | Ensures CodeFix can find the node via `FindNode` + ancestor/descendant traversal (same as FC0004) |
| Exclude permissionset(extension) via syntax ancestors | Deterministic, no semantic model lookup needed |
| Skip obsolete objects | Standard convention (`ctx.IsObsolete()`) |
| `ContainsUppercase` is `internal static` on the analyzer | Shared with the CodeFix within the assembly |

## CodeFix

`PermissionValuesShouldBeLowercaseCodeFixProvider` collects all `Permissions` tokens containing uppercase chars and replaces them via `root.ReplaceTokens` with `SyntaxFactory.Identifier(text.ToLowerInvariant()).WithTriviaFrom(original)`. Entry order, formatting, and trivia are preserved. Supports FixAll via BatchFixer.

## Test coverage

**HasDiagnostic (9 cases):** CodeunitUppercase, CodeunitMixedCase, TableUppercase, PageUppercase, ReportUppercase, XmlPortUppercase, QueryUppercase, RequestPageUppercase, MultipleEntriesOneUppercase.
**NoDiagnostic (6 cases):** LowercaseCodeunit, PermissionSetUppercase, PermissionSetExtensionUppercase, InherentPermissionsUppercase, NoPermissionsProperty, ObsoleteCodeunit.
**HasFix (3 cases):** LowercaseAllValues, MixedCaseValue, MultipleEntries.
