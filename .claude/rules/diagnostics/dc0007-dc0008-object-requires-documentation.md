---
paths:
  - "src/ALCops.DocumentationCop/Analyzers/ObjectRequiresDocumentation*"
  - "src/ALCops.DocumentationCop.Test/Rules/ObjectRequiresDocumentation/**"
---

# DC0007/DC0008: ObjectRequiresDocumentation

## Purpose

Requires XML documentation comments on public and internal application objects. DC0007 applies to public objects; DC0008 applies to internal objects and is disabled by default.

Registers `RegisterSymbolAction` on application object symbol kinds; main type `ObjectRequiresDocumentation`.

## Design decisions

| Decision | Rationale |
|---|---|
| Determine documentation from the declaration's leading trivia | The symbol API can return an empty XML comment for a `Profile` even when the source contains a valid `///` comment. The rule checks the source declaration directly so all registered object kinds use the same behavior. |
| Keep `Profile` in the analyzed symbol kinds | Profiles are application objects and undocumented profiles must still receive DC0007; the documented-profile regression case must not be fixed by excluding the entire symbol kind. |

## Deliberate non-reports

- Obsolete objects, following the standard cop convention.
- Test codeunits, because they are not part of an extension's public documentation surface.

## Known issues

- Interfaces and control add-ins are registered but do not implement `IApplicationObjectTypeSymbol`, so the analyzer currently skips their object-level documentation checks. This is a separate SDK type-hierarchy limitation.

## Test notes

- The test class injects `ObjectRequiresDocumentation.ruleset.json` to enable DC0008, which is disabled by default.
- The profile regression fixtures include a namespace, a quoted profile name, a documentation comment, and a local RoleCenter page so the AL input is self-contained.
