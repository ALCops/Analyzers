---
paths:
  - "src/ALCops.TestAutomationCop/**/GlobalMethodRequiresTestAttribute*"
  - "src/ALCops.TestAutomationCop.Test/Rules/GlobalMethodRequiresTestAttribute/**"
---

# TA0001: GlobalMethodRequiresTestAttribute

## Purpose

Reports a global procedure in a `Subtype = Test` codeunit that is neither a test procedure nor a UI handler. A test codeunit should expose only what the test runner calls; shared setup and helper logic belongs in a local procedure or a library codeunit.

Registers `RegisterSymbolAction` on `SymbolKind.Method`; main type `GlobalMethodRequiresTestAttribute`.

**References:** [#658](https://github.com/ALCops/Analyzers/issues/658) (interface implementations, AL 18.0 parameterized tests), [#676](https://github.com/ALCops/Analyzers/issues/676) (global event subscribers, open), [#511](https://github.com/ALCops/Analyzers/issues/511) (BC29 uptake).

## Design decisions

| Decision | Rationale |
|---|---|
| Exempt `[Test]` and the fixed list of handler attributes (`ConfirmHandler`, `MessageHandler`, `HttpClientHandler`, ...) by `AttributeKind` | The test runner resolves handlers by name through `[HandlerFunctions]`, so they must be global; an explicit list keeps a new attribute reported until someone decides it belongs, instead of exempting every attributed method |
| Exempt methods that implement a member of an interface the codeunit declares in `implements`, through the Common `MethodImplementsInterfaceMethod()` helper | An interface member must be a global procedure, so the code cannot follow the rule's advice (make it local or add `[Test]`). This covers the AL 18.0 parameterized-test shapes, where a test codeunit implements the system `ITestDataSource` (`ListTestCases`, `GetTestCase`) or `ITestHandler` (before/after hooks), and every user-defined interface alike, because the check does not depend on which interface it is |
| Match the interface member by signature (name, parameter count, `var`-ness, parameter types, return kind), not by name alone | An overload of an interface member name with a different signature is an ordinary helper and stays reported |
| Severity `Info`, enabled by default | The rule is a test-design convention; nothing breaks at runtime |

## Deliberate non-reports

- Local procedures, triggers, and any procedure outside a `Subtype = Test` codeunit.
- Procedures that implement a member of a directly implemented interface (see Design decisions).

## Known issues

- Global `[EventSubscriber]` procedures in test codeunits are reported. BC w1-29 has 16 global against 470 local subscribers in test codeunits; whether to exempt them or keep reporting is open in [#676](https://github.com/ALCops/Analyzers/issues/676).
- Interface inheritance is not followed: a codeunit that implements `IDerived extends IBase` still gets a report on the procedures that implement `IBase` members. `ICodeunitTypeSymbol.ImplementedInterfaces` lists the declared interfaces only, and the shared helper does not walk `IInterfaceTypeSymbol.GetAllExtendedInterfaces()`. Fixing it belongs in the Common helper, which other rules share, so it is left as a gap until a real case shows up.

## SDK facts

- `ICodeunitTypeSymbol.ImplementedInterfaces` lists the interfaces named in `implements` only; inherited ones come from `IInterfaceTypeSymbol.GetAllExtendedInterfaces()`, and `GetMembers()` on an interface returns its own members only (docs: nav-sdk-docs `docs/20-symbols/object-type-symbols.md`; verified against SDK 30.0.44.47290 in `../nav-sdk-source`, `Symbols/CodeunitTypeSymbol.cs`, `Symbols/InterfaceTypeSymbol.cs`). The compiler itself uses the internal `CodeunitTypeSymbol.GetAllImplementedInterfaces()`.
- `[TestDataSource]` sits on the `[Test]` method, which must be public, return nothing, and take one `ITestContext` parameter; the codeunit named as data source must implement `ITestDataSource`, recognised only when it comes from the system module (`Symbols/TestDataSourceAttributeValidator.cs:52-78,93-103`, SDK 18.0 GA). `TestHandlers` is an 18.0 codeunit property that depends on `Subtype(Test)` (`InternalSyntax/ObjectParser.cs:271`).

## Test notes

- There is no `ITestDataSource`/`ITestHandler` fixture: the test harness has no system module, so `implements ITestDataSource` fails with AL0185 ("Interface 'ITestDataSource' is missing") even on the 18.0+ SDK. A user-defined interface covers the same code path, because the exemption does not depend on which interface is implemented.
