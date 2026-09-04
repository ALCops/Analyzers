---
paths:
  - "src/ALCops.DocumentationCop/**/ProcedureRequiresDocumentation*"
  - "src/ALCops.DocumentationCop.Test/Rules/ProcedureRequiresDocumentation/**"
---

# DC0004/DC0006/DC0009/DC0010: ProcedureRequiresDocumentation

## Purpose

Requires XML documentation comments on procedures and events that form the API surface of an extension. One analyzer emits four diagnostics: DC0004 (public procedure), DC0006 (internal procedure), DC0009 (integration/business event), DC0010 (internal event). DC0004 targets procedures callable from a dependent extension; the deciding factor is effective external visibility, object accessibility combined with procedure accessibility.

Registers `RegisterSyntaxNodeAction` on `MethodDeclaration`, plus a second syntax action on `EventDeclaration` for control add-in events; main type `ProcedureRequiresDocumentation`.

**References:** [#451](https://github.com/ALCops/Analyzers/issues/451) (interfaces and control add-ins).

## Design decisions

| Decision | Rationale |
|---|---|
| DC0006 and DC0010 (internal targets) are disabled by default; DC0004 and DC0009 are enabled | Internal procedures and events are not cross-extension API surface. |
| Public interface procedures raise DC0004 | Interface signatures are the cross-extension API contract. |
| Internal interface procedures raise DC0006 | Consistent with procedures in `Access = Internal` codeunits ([#438](https://github.com/ALCops/Analyzers/issues/438)). |
| Public control add-in procedures and events raise DC0004 | Both declarations form the AL contract a `usercontrol` consumer sees, even though the implementation is JavaScript. |
| Control add-in events get their own `EventDeclaration` syntax action, restricted to control add-in containers | Control add-in events are `EventDeclarationSyntax` bound to `IEventSymbol`, not methods, so the method callback never sees them; restricting the callback keeps AL publisher events in codeunits routed through the method callback and DC0009/DC0010. |
| Layered containing-object resolution: `GetContainingApplicationObjectTypeSymbol()` first, `GetContainingObjectTypeSymbol()` only when that returns null | A plain swap to the object walker would resolve requestpage procedures to the requestpage (hardcoded `Local` accessibility) instead of the report/xmlport; in AL the null fallback only happens for interface and control add-in members. |

## Deliberate non-reports

- `local` procedures: not callable outside the object.
- Procedures in test codeunits: test methods are not API surface.
- Obsolete members, following the standard cop convention.
- Declarations that already carry XML documentation leading trivia.

## SDK facts

- The interface / control add-in hierarchy pitfall is covered in `.claude/rules/symbol-resolution.md`. Rule-specific consequence: `IApplicationObjectTypeSymbol` and `IObjectTypeSymbol` are siblings (neither extends the other), so a variable holding either must be typed `ISymbol`.
- `ObjectTypeSymbol.DeclaredAccessibility` reads the `Access` property (default Public) for all object types, so the object-walker fallback yields correct accessibility for interfaces.
- Control add-in events are `EventDeclarationSyntax` nodes whose declared symbol is an `IEventSymbol`; `SyntaxKind.EventDeclaration` is reached through `EnumProvider`.

## Test notes

- The test class injects `ProcedureRequiresDocumentation.ruleset.json` to enable DC0006 and DC0010 (`isEnabledByDefault: false`).
