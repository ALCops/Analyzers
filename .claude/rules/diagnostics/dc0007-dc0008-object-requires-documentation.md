---
paths:
  - "src/ALCops.DocumentationCop/**/ObjectRequiresDocumentation*"
  - "src/ALCops.DocumentationCop.Test/Rules/ObjectRequiresDocumentation/**"
---

# DC0007/DC0008: ObjectRequiresDocumentation

## Purpose

Requires XML documentation comments on objects that form the extension's API surface: DC0007 for public objects, DC0008 for internal ones.

Registers `RegisterSymbolAction` on the top-level object kinds (codeunit, control add-in, enum, interface, page, permission set, profile, query, report, table, xmlport) and on the extension kinds (enumextension, pagecustomization, pageextension, permissionsetextension, profileextension, reportextension, tableextension); main type `ObjectRequiresDocumentation`.

**References:** [#451](https://github.com/ALCops/Analyzers/issues/451).

## Design decisions

| Decision | Rationale |
|---|---|
| Keep `Profile` in the analyzed symbol kinds | Profiles are application objects and undocumented profiles must still receive DC0007; the documented-profile regression case must not be fixed by excluding the entire symbol kind. |
| DC0008 (internal objects) is disabled by default; DC0007 is enabled | Internal objects are not cross-extension API surface. |
| The callback works on `IObjectTypeSymbol`, not `IApplicationObjectTypeSymbol` | Interfaces and control add-ins implement only `IObjectTypeSymbol`; gating on the application-object interface silently dropped both kinds even though they were registered. |
| Interfaces raise DC0007 or DC0008 according to their `Access` property | Interface definitions are cross-extension API contracts, consistent with the procedure-level rule DC0004/DC0006. |
| Extension objects are documented like base objects; they always count as public because AL gives them no `Access` property | An extension is part of the app's documented surface even though no other object can reference it by name. Skipping them left every tableextension and pageextension undocumented without any signal. With no `Access` property they resolve to Public, so they raise DC0007 and never DC0008. |
| Control add-ins raise DC0007 and never DC0008 | A control add-in is referenced through `usercontrol` and needs object-level documentation; AL allows no `Access` property on it, so it is always public. |
| The test-codeunit exemption applies only to symbols that also implement `IApplicationObjectTypeSymbol` | Only application objects can be test codeunits; keeping the check on that interface avoids a reflective subtype lookup for interfaces and control add-ins. |
| Symbol XML documentation falls back to declaration leading trivia | Older AL Language SDKs do not expose XML documentation through symbols for every object kind; source trivia reliably represents documentation in those versions. |

## Deliberate non-reports

- Obsolete objects, following the standard cop convention.
- Test codeunits: test code is not part of an extension API surface.
- Objects that already carry XML documentation.

## SDK facts

- `IApplicationObjectTypeSymbol` and `IObjectTypeSymbol` are siblings; the general pitfall is in `.claude/rules/symbol-resolution.md`.
- `ObjectTypeSymbol.DeclaredAccessibility` reads the `Access` property (default Public) for every object kind, so interfaces resolve their accessibility without special handling.
- Every extension and customization symbol (`ApplicationObjectExtensionTypeSymbol`, `PageExtensionBaseTypeSymbol`, `PermissionSetExtensionSymbol`) derives from `ApplicationObjectTypeSymbol : ObjectTypeSymbol`, so it passes the `IObjectTypeSymbol` gate and reaches the same `DeclaredAccessibility` getter; with no `Access` property it returns Public. `IsTestCodeunit()` returns false for them because they are not `ICodeunitTypeSymbol`.

## Test notes

- Fixtures are split by diagnostic and outcome: `PublicHasDiagnostic`, `PublicNoDiagnostic`, `InternalHasDiagnostic`, `InternalNoDiagnostic`, with a test method per folder.
- The test class injects `ObjectRequiresDocumentation.ruleset.json` to enable DC0008 (`isEnabledByDefault: false`).
- Extension fixtures declare their target in the same file, which the compiler accepts only from SDK 13.0, so they are gated with `SkipTestIfVersionIsTooLow(..., "13.0", ...)`; the profileextension fixtures are also gated on `"14.0"` because `Feature.ProfileExtensions` requires runtime Fall2024. Profile extensions and page customizations have their own fixtures so those gates stay narrow. The base objects in these fixtures carry documentation so the rule reports only on the extensions.
- `InternalNoDiagnostic/UndocumentedExtensions.al` pins the always-public decision: undocumented extensions never raise DC0008.
- The profile regression fixtures include a namespace, a quoted profile name, a documentation comment, and a local RoleCenter page so the AL input is self-contained.
