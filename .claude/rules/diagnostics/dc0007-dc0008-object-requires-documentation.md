---
paths:
  - "src/ALCops.DocumentationCop/**/ObjectRequiresDocumentation*"
  - "src/ALCops.DocumentationCop.Test/**/ObjectRequiresDocumentation/**"
---

# DC0007/DC0008: ObjectRequiresDocumentation

## Purpose

Requires XML documentation comments on public (DC0007) and internal (DC0008) objects that form
the extension's API surface.

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Interfaces raise DC0007 or DC0008 according to their `Access` property | Interface definitions are cross-extension API contracts, consistent with the procedure-level rule. |
| Control add-ins raise DC0007 | A control add-in can be referenced through `usercontrol` and needs object-level API documentation, even though its JavaScript-implemented procedures are exempt from DC0004/DC0006. |
| Control add-ins do not raise DC0008 | AL does not allow an `Access` property on control add-ins, so they are always public. |
| Test codeunits are exempt | Test code is not part of an extension API surface. |

## Architecture

- Registers a symbol action for all supported top-level AL object kinds.
- Uses `IObjectTypeSymbol`, not `IApplicationObjectTypeSymbol`, because interface and control-add-in
  symbols implement only the former. The test-codeunit exemption remains conditional on the
  application-object interface.