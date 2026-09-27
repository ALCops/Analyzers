---
paths:
  - "src/ALCops.ApplicationCop/**/RequiredInterfaceImplementation*"
  - "src/ALCops.ApplicationCop.Test/Rules/RestClientRequiresHttpClientHandler/**"
  - "src/ALCops.ApplicationCop.Test/Rules/TelemetryRequiresTelemetryLogger/**"
---

# AC0033 / AC0034: RequiredInterfaceImplementation

## Purpose

One analyzer reports two diagnostics: AC0033 `RestClientRequiresHttpClientHandler` (an object declares a `Codeunit "Rest Client"` variable but no codeunit in the app implements `"Http Client Handler"`, so the System Application's default handler sends the request and the permission scope, outgoing web-service telemetry and test mocking belong to the System Application) and AC0034 `TelemetryRequiresTelemetryLogger` (an object declares a `Codeunit Telemetry` or `"Feature Telemetry"` variable but no codeunit in the app implements `"Telemetry Logger"`, so `Telemetry Loggers Impl.` finds no logger for the publisher, logs `0000G7K` and drops the message).

Registers `RegisterCompilationStartAction` with an inner `RegisterSymbolAction` on Codeunit, Table, TableExtension, Page, PageExtension, Report, ReportExtension, Query and XmlPort; main type `RequiredInterfaceImplementation` with a static pair table.

**References:** [ALCops/Analyzers discussion #405](https://github.com/ALCops/Analyzers/discussions/405) (Rule 3 there); System Application `Telemetry/src/Logging/TelemetryLoggersImpl.Codeunit.al` (`GetTelemetryLogger`) and the `<remarks>` of `Telemetry.Codeunit.al`.

## Design decisions

| Decision | Rationale |
|---|---|
| One analyzer with a hard-coded pair table (trigger codeunits → required interface → descriptor) | Both rules are the same "uses X, implements no Y" check; a future pair is one row plus one ID. Two separate analyzers would duplicate the walk and the index. |
| Existence check only: some codeunit implements the interface | Tracking whether each variable is passed to `Initialize`/`Create` (or the logger registered via `OnRegisterTelemetryLogger`) needs a per-procedure flow walk; that is a separate follow-up rule, not this one. |
| Coverage counts codeunits declared in the compiling module only (`GetDeclaredApplicationObjectSymbols`), obsolete and test codeunits included | Dependency implementations would need a cross-module scan and make the result depend on what happens to be referenced; a shared-library architecture suppresses via ruleset or pragma. |
| Trigger codeunit matched on object id AND name, per id/name pair (2350 "Rest Client"; 8711 Telemetry; 8703 "Feature Telemetry") | Id alone matches unrelated same-id objects outside the System Application; name alone matches any same-named codeunit. Namespace or module checks were rejected so stubbed or namespaced System Application builds still match. |
| Interface matched by name only (`SemanticFacts.IsSameName`) | The implementing codeunit's `ImplementedInterfaces` points at the dependency interface; comparing module or namespace adds nothing a real app would get wrong. |
| Codeunits with `Subtype = Test` or `TestRunner` are never reported | Test code uses the Rest Client or Telemetry against mocks and is not shipped as the publisher's runtime code. |
| One diagnostic per rule per object, at the object name | The fix is app-wide (add one implementation), so per-variable diagnostics would repeat the same message; an object using both trigger kinds without either implementation gets two diagnostics. |
| The symbol walk runs before the index is consulted | Most objects use neither codeunit; the lazily built index is only forced when an object actually uses one. |
| Warning, enabled by default, `Category.Design` | The app works but attributes its calls or loses its telemetry silently; every ApplicationCop rule uses Design. |
| No settings | Nothing to tune: the pairs are fixed platform facts and the suppression cases are covered by ruleset/pragma. |
| No CodeFix | A generated handler or logger codeunit has no deterministic file, name, id or namespace. |
| No version gate | Every SDK member used is available at ns2.0 / AL 12.0 (`reference/` tables in nav-sdk-docs). |

## Deliberate non-reports

- `Codeunit::"Rest Client"` object references without a variable (`Codeunit.Run(...)`): no instance is used.
- `array[n] of Codeunit X`, `List of [Codeunit X]` and interface-typed variables: the variable type is not the codeunit symbol.
- Implementations in a dependency app (current-compilation-only coverage).
- Whether a handler is actually passed to `Initialize`/`Create`, or a logger actually registered in `OnRegisterTelemetryLogger`: existence only.
- Obsolete objects (`IsObsolete()`) and test / test-runner codeunits.

## Known issues

- A same-named interface from a different module counts as coverage (name-only interface match); accepted.
- One implementation anywhere in the app satisfies every object, even if only some `Rest Client` instances receive it.

## SDK facts

- For `Codeunit "Rest Client"`, `IVariableSymbol.Type` is the `ICodeunitTypeSymbol` itself, no wrapper (`Binder.BindSubTypedDataType`); referenced-app codeunits are `ReferenceCodeunitTypeSymbol : CodeunitTypeSymbol`; unresolved types are error type symbols that fail the cast. Only Page/TestPage variables get wrapper types. (Verified in `../nav-sdk-source` `Binder.cs`, net10.0 and v12.0.)
- `IContainerSymbol.GetMembers()` returns members declared on the container itself; base or related table members are not included, and extension symbols return what the extension adds (docs: nav-sdk-docs `docs/20-symbols/object-type-symbols.md`, `docs/20-symbols/symbol-hierarchy.md`). Controls return controls, actions and triggers; actions return actions and triggers; fields return triggers; changes return added controls, actions, dataitems or triggers; report dataitems return triggers, columns and dataitems; xmlport nodes return child nodes and triggers (`Source*Symbol.cs` in `../nav-sdk-source`).
- The walk descends only into Field, Control, Action, Change, ReportDataItem, XmlPortNode, RequestPage and RequestPageExtension. Keys are excluded because in a tableextension they enumerate the target table's fields, which would attribute base-table field triggers to the extension.
- `Compilation.GetDeclaredApplicationObjectSymbols()` returns the current module's objects only (`Compilation.cs`).

## Test notes

- `TableExtensionTriggerLocal` and `PageExtensionActionTriggerLocal` are gated on 13.0: AL 12 rejects extensions whose target is declared in the same module (AL0334).
- Fixtures declare their own stubs with the real System Application ids (2350, 8711, 8703); the harness sets no id ranges, so ids outside 50000-99999 compile.
- Report dataitems and page source tables use an inline `table 50100`; system tables are not available in the harness.
