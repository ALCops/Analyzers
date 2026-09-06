---
paths:
  - "src/ALCops.DocumentationCop/**/ObjectRequiresDocumentation*"
  - "src/ALCops.DocumentationCop.Test/Rules/ObjectRequiresDocumentation/**"
---

# DC0007/DC0008: ObjectRequiresDocumentation

## Purpose

Requires XML documentation comments on objects that form the extension's API surface: DC0007 for public objects, DC0008 for internal ones.

Registers `RegisterSymbolAction` on the top-level object kinds (codeunit, control add-in, enum, interface, page, permission set, profile, query, report, table, xmlport); main type `ObjectRequiresDocumentation`.

**References:** [#451](https://github.com/ALCops/Analyzers/issues/451).

## Design decisions

| Decision | Rationale |
|---|---|
| DC0008 (internal objects) is disabled by default; DC0007 is enabled | Internal objects are not cross-extension API surface. |
| The callback works on `IObjectTypeSymbol`, not `IApplicationObjectTypeSymbol` | Interfaces and control add-ins implement only `IObjectTypeSymbol`; gating on the application-object interface silently dropped both kinds even though they were registered. |
| Interfaces raise DC0007 or DC0008 according to their `Access` property | Interface definitions are cross-extension API contracts, consistent with the procedure-level rule DC0004/DC0006. |
| Control add-ins raise DC0007 and never DC0008 | A control add-in is referenced through `usercontrol` and needs object-level documentation; AL allows no `Access` property on it, so it is always public. |
| The test-codeunit exemption applies only to symbols that also implement `IApplicationObjectTypeSymbol` | Only application objects can be test codeunits; keeping the check on that interface avoids a reflective subtype lookup for interfaces and control add-ins. |

## Deliberate non-reports

- Test codeunits: test code is not part of an extension API surface.
- Objects that already carry XML documentation.

## SDK facts

- `IApplicationObjectTypeSymbol` and `IObjectTypeSymbol` are siblings; the general pitfall is in `.claude/rules/symbol-resolution.md`.
- `ObjectTypeSymbol.DeclaredAccessibility` reads the `Access` property (default Public) for every object kind, so interfaces resolve their accessibility without special handling.

## Test notes

- Fixtures are split by diagnostic and outcome: `PublicHasDiagnostic`, `PublicNoDiagnostic`, `InternalHasDiagnostic`, `InternalNoDiagnostic`, with a test method per folder.
- The test class injects `ObjectRequiresDocumentation.ruleset.json` to enable DC0008 (`isEnabledByDefault: false`).
