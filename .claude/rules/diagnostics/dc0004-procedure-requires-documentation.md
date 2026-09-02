---
paths:
  - "src/ALCops.DocumentationCop/**/ProcedureRequiresDocumentation*"
---

# DC0004/DC0006/DC0009/DC0010: ProcedureRequiresDocumentation

## Purpose

Requires XML documentation comments on procedures and events that form the API surface of an
extension. One analyzer (`ProcedureRequiresDocumentation`) emits four diagnostics: DC0004 (public
procedure), DC0006 (internal procedure), DC0009 (integration/business event), DC0010 (internal event).

DC0004 targets procedures **callable from a dependent extension** (cross-extension API surface).
The deciding factor is effective external visibility: object accessibility x procedure
accessibility. Signatures in a public `interface` are part of the API contract and raise DC0004;
internal interfaces route to DC0006, matching internal codeunit behavior.

## Design decisions

| Decision | Rationale |
|----------|-----------|
| DC0006 and DC0010 (internal targets) are disabled by default (opt-in); DC0004 and DC0009 are enabled | Internal procedures and events are not cross-extension API surface |
| Public interface procedures raise DC0004 | Interface signatures are the cross-extension API contract |
| Internal interface procedures raise DC0006 | Consistent with procedures in `Access = Internal` codeunits (issue #438) |
| Public ControlAddIn procedures and events raise DC0004 | Both declarations form the public AL contract exposed by the control add-in |
| Layered containing-object resolution | See SDK pitfall below; preserves requestpage behavior |
| `local` procedures are skipped | Not callable outside the object |
| Test codeunits are skipped | Test methods are not API surface |
| Obsolete members are skipped | Standard cop convention |

## Architecture

- Registers `SyntaxNodeAction` for `MethodDeclaration` and `EventDeclaration`; ControlAddIn events
  use the latter SDK syntax node and bind to `IEventSymbol` rather than `IMethodSymbol`.
- Resolves the containing object with **layered resolution**:
  1. `GetContainingApplicationObjectTypeSymbol()` (primary),
  2. fall back to `GetContainingObjectTypeSymbol()` only when the primary returns null.
- Skips test codeunits, obsolete members, and declarations with XML documentation leading trivia.
- Reports undocumented ControlAddIn procedures and events as DC0004. The dedicated event callback
  is restricted to ControlAddIns so AL publisher events remain routed by the method callback.
- Routes to internal diagnostics when the procedure has the `internal` keyword or the containing
  object's `DeclaredAccessibility` is Internal; events route via
  `IsIntegrationOrBusinessEvent()` / `IsInternalEvent()`.

## SDK pitfall: IApplicationObjectTypeSymbol vs IObjectTypeSymbol

`IInterfaceTypeSymbol` and `IControlAddInTypeSymbol` implement `IObjectTypeSymbol` but **not**
`IApplicationObjectTypeSymbol` (the two interfaces are siblings; neither extends the other, so a
merged variable must be typed `ISymbol`). Consequences:

- `GetContainingApplicationObjectTypeSymbol()` returns **null** for interface/controladdin
  members. Before the #438 fix, the null silently defeated the internal-accessibility check and
  internal interface procedures fell into the public DC0004 branch.
- A naive swap to `GetContainingObjectTypeSymbol()` is **not safe**: the SDK walker returns the
  first `IObjectTypeSymbol` walking up, and `RequestPageTypeSymbol` /
  `RequestPageExtensionTypeSymbol` are `IObjectTypeSymbol` with hardcoded
  `DeclaredAccessibility = Local`. Requestpage procedures would resolve to the requestpage
  instead of the report/xmlport. Hence the layered resolution: application-object walker first,
  object walker only as a null fallback (which in AL only happens for interface/controladdin).
- `ObjectTypeSymbol.DeclaredAccessibility` reads the `Access` property (default Public) for all
  object types, so the fallback gives correct accessibility for interfaces.

## Known issues

None.
