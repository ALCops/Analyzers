---
paths:
  - "src/ALCops.ApplicationCop/**/RequiredInterfaceImplementation*"
  - "src/ALCops.ApplicationCop.Test/Rules/RestClientRequiresHttpClientHandler/**"
  - "src/ALCops.ApplicationCop.Test/Rules/TelemetryRequiresTelemetryLogger/**"
---

# AC0033 / AC0034: RequiredInterfaceImplementation

## Purpose

One analyzer reports two diagnostics: AC0033 `RestClientRequiresHttpClientHandler` (an object declares a `Codeunit "Rest Client"` variable but no codeunit in the app implements `"Http Client Handler"`, so the System Application's default handler, or a handler in a dependency app, sends the request) and AC0034 `TelemetryRequiresTelemetryLogger` (an object declares a `Codeunit Telemetry` or `"Feature Telemetry"` variable but no codeunit in the app implements `"Telemetry Logger"`, so `Telemetry Loggers Impl.` finds no logger for the publisher, logs `0000G7K` and drops the message, unless another app from the publisher supplies the logger).

AC0033 is opt-in. Its intent is that every app sends its HTTP calls from its own extension, regardless of its dependencies. In a non-production environment the runtime blocks an extension's HTTP requests unless **Allow HttpClient Requests** is turned on for that extension on the **Extension Settings** page, and a production-to-sandbox copy starts with outbound calls blocked until the setting is turned on per extension. Only when the app's own handler sends the request does that app's setting govern the call. Outgoing web-service telemetry and test mocking follow the same extension and are secondary benefits.

AC0034 is opt-in as well. Its intent is that every app ships its own Telemetry Logger, so its telemetry does not depend on which other apps from the publisher are installed; it does not give per-app routing, because the lookup stays per publisher and the first registration wins. That is safe on BC 25.0 and later, where a second registration for the same publisher is silently ignored (first wins), so a logger in every app drops nothing; before 25.0 each duplicate registration logs warning `0000G7J`. It is not the default because Microsoft documents a per-publisher model: one logger per publisher, which may live in any installed app of that publisher, and the analyzer sees only the compiling app, so it cannot tell a missing logger from a logger supplied by a sibling app. See Platform facts.

Registers `RegisterCompilationStartAction` with an inner `RegisterSymbolAction` on Codeunit, Table, TableExtension, Page, PageExtension, Report, ReportExtension, Query and XmlPort; main type `RequiredInterfaceImplementation` with a static pair table.

**References:** [ALCops/Analyzers discussion #405](https://github.com/ALCops/Analyzers/discussions/405) (Rule 3 there); System Application `Telemetry/src/Logging/TelemetryLoggersImpl.Codeunit.al` (`GetTelemetryLogger`) and the `<remarks>` of `Telemetry.Codeunit.al`; Microsoft Learn "Feature telemetry" (exactly one `"Telemetry Logger"` implementation per app publisher); BCTech `samples/AppInsights/AL/FeatureTelemetry` README (the logger may live in any extension from the publisher).

## Design decisions

| Decision | Rationale |
|---|---|
| One analyzer with a hard-coded pair table (trigger codeunits → required interface → descriptor) | Both rules are the same "uses X, implements no Y" check; a future pair is one row plus one ID. Two separate analyzers would duplicate the walk and the index. |
| Existence check only: some codeunit implements the interface | Tracking whether each variable is passed to `Initialize`/`Create` (or the logger registered via `OnRegisterTelemetryLogger`) needs a per-procedure flow walk; that is a separate follow-up rule, not this one. |
| Coverage counts codeunits declared in the compiling module only (`GetDeclaredApplicationObjectSymbols`), obsolete and test codeunits included | Dependency implementations would need a cross-module scan and make the result depend on what happens to be referenced; a shared-library architecture, where a base app sends the calls for its dependents, simply leaves AC0033 off. |
| Trigger codeunit matched on object id AND name, per id/name pair (2350 "Rest Client"; 8711 Telemetry; 8703 "Feature Telemetry") | Id alone matches unrelated same-id objects outside the System Application; name alone matches any same-named codeunit. Namespace or module checks were rejected so stubbed or namespaced System Application builds still match. |
| Interface matched by name only (`SemanticFacts.IsSameName`) | The implementing codeunit's `ImplementedInterfaces` points at the dependency interface; comparing module or namespace adds nothing a real app would get wrong. |
| Codeunits with `Subtype = Test` or `TestRunner` are never reported | Test code uses the Rest Client or Telemetry against mocks and is not shipped as the publisher's runtime code. |
| One diagnostic per rule per object, at the object name | The fix is app-wide (add one implementation), so per-variable diagnostics would repeat the same message; an object using both trigger kinds without either implementation gets two diagnostics. |
| The symbol walk runs before the index is consulted | Most objects use neither codeunit; the lazily built index is only forced when an object actually uses one. |
| AC0033: Info, disabled by default | A shared base layer that sends HTTP calls for its dependent apps is a valid design, and the rule does not see handlers in dependencies. Teams that want per-extension control through Allow HttpClient Requests enable it in their ruleset. |
| AC0034: Info, disabled by default | The logger lookup is per publisher and any installed app from the publisher can supply it, which is not verifiable at compile time (dependency symbols are not scanned). Teams that want self-sufficient apps enable it in their ruleset. |
| `Category.Design` for both | Every ApplicationCop rule uses Design. |
| No settings | Nothing to tune: the pairs are fixed platform facts; both rules are switched on through the ruleset. |
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

## Platform facts

Telemetry runtime behaviour, from the System Application `Telemetry/src/Logging/` sources (identical across localizations; verified against the `be-28.5.54151.54763` snapshot of the BC code history, and the 25.0 change is the diff of `TelemetryLoggersImpl.Codeunit.al` between the `be-23.5.16502.16887` and `be-25.0.23364.24925` snapshots):

- `Telemetry.LogMessage` and `Feature Telemetry` pass the calling app's publisher (`NavApp.GetCallerModuleInfo`). `Telemetry Impl.LogMessageInternal` sets the call-stack publishers, raises `"Telemetry Loggers".OnRegisterTelemetryLogger`, then looks the logger up by publisher string.
- `Register` keys the logger by the registering app's publisher, accepts it only when that publisher is on the call stack, and keeps the first registration per publisher.
- `"Telemetry Loggers"` is a local variable in each `LogMessage` call (not single instance), so the event fires and every subscriber re-registers on every call. Only `Register` is public; there is no query API.
- The logger's own `Session.LogMessage` runs inside the logger app, so that app's `applicationInsightsConnectionString` receives the event; the calling app appears only in the `CallerAppName` custom dimension.
- No registered logger for the publisher: the message is discarded and warning `0000G7K` is logged. Before BC 25.0 a duplicate registration logged warning `0000G7J`; since 25.0 it is ignored silently.
- The Base Application uses these codeunits in many files and implements no logger; the only logger Microsoft ships is `System Telemetry Logger` in the System Application.

## SDK facts

- For `Codeunit "Rest Client"`, `IVariableSymbol.Type` is the `ICodeunitTypeSymbol` itself, no wrapper (`Binder.BindSubTypedDataType`); referenced-app codeunits are `ReferenceCodeunitTypeSymbol : CodeunitTypeSymbol`; unresolved types are error type symbols that fail the cast. Only Page/TestPage variables get wrapper types. (Verified in `../nav-sdk-source` `Binder.cs`, net10.0 and v12.0.)
- `IContainerSymbol.GetMembers()` returns members declared on the container itself; base or related table members are not included, and extension symbols return what the extension adds (docs: nav-sdk-docs `docs/20-symbols/object-type-symbols.md`, `docs/20-symbols/symbol-hierarchy.md`). Controls return controls, actions and triggers; actions return actions and triggers; fields return triggers; changes return added controls, actions, dataitems or triggers; report dataitems return triggers, columns and dataitems; xmlport nodes return child nodes and triggers (`Source*Symbol.cs` in `../nav-sdk-source`).
- The walk descends only into Field, Control, Action, Change, ReportDataItem, XmlPortNode, RequestPage and RequestPageExtension. Keys are excluded because in a tableextension they enumerate the target table's fields, which would attribute base-table field triggers to the extension.
- `Compilation.GetDeclaredApplicationObjectSymbols()` returns the current module's objects only (`Compilation.cs`).

## Test notes

- AC0033 is disabled by default, so its tests enable it through `RestClientRequiresHttpClientHandler.ruleset.json` (`RuleSetPath`). AC0034 is disabled by default too, so its tests use `TelemetryRequiresTelemetryLogger.ruleset.json`. The AC0033 ruleset enables both rules because `BothRulesOneObjectReportsBoth` runs the two-rule object in the AC0033 class. `DisabledWithoutRuleSet` exists in both classes and pins the opt-in default.
- `TableExtensionTriggerLocal` and `PageExtensionActionTriggerLocal` are gated on 13.0: AL 12 rejects extensions whose target is declared in the same module (AL0334).
- Fixtures declare their own stubs with the real System Application ids (2350, 8711, 8703); the harness sets no id ranges, so ids outside 50000-99999 compile.
- Report dataitems and page source tables use an inline `table 50100`; system tables are not available in the harness.
