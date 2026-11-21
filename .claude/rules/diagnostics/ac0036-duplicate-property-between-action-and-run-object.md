---
paths:
  - "src/ALCops.ApplicationCop/**/DuplicatePropertyBetweenActionAndRunObject*"
  - "src/ALCops.ApplicationCop.Test/Rules/DuplicatePropertyBetweenActionAndRunObject/**"
---

# AC0036: DuplicatePropertyBetweenActionAndRunObject

## Purpose

Reports an action's `Caption`, `ToolTip`, `AboutTitle` or `AboutText` when its page or report `RunObject` target declares the same value. The action inherits the target's value when it sets none of its own, so the copy is redundant and puts a second trans-unit for the same text into the XLIFF file.

Registers `RegisterSymbolAction` on `Action` and `Change` (modify blocks); main type `DuplicatePropertyBetweenActionAndRunObject`.

## Design decisions

| Decision | Rationale |
|---|---|
| Whole rule gated on runtime 18.0 (`Fall2026OrGreater`) | Chosen over per-property gating and over no gate: runtime 18.0 introduced page `ToolTip` inheritance, and one gate keeps the rule aligned with that release. It hides `Caption` and About* duplicates on apps below 18.0, which is accepted. |
| Severity Info, enabled by default | The BC w1 29 Base App has about 2,590 `Caption` duplicates; Info surfaces them without ever breaking a CI build. |
| Label attributes (`Locked`, `Comment`, `MaxLength`) are ignored | Only the value decides what the runtime shows; comparing attributes would hide duplicates that differ in a translator comment. |
| Ordinal, case-sensitive comparison of `ValueText` | A value that differs only in case is a different trans-unit and a different caption, so it is not a copy. |
| Empty values are never reported | An empty override such as `ToolTip = ''` deliberately suppresses the inherited value; it is not a copy, even when the target is empty too. |
| `modify` blocks in page extensions are reported only when the base action sets neither the property nor its ML variant | When the base action sets its own value, the modify replaces that value; removing the modify would bring back the base value, not the target's. |
| Page customizations are excluded | Customizations are not translated, so a copy there adds no trans-unit; their action `Caption` and `ToolTip` are not even customization-modifiable. |
| A report's About* values come from its request page, found through the public `GetMembers()` | `IReportTypeSymbol` exposes no public request page; the emitter also reads report About* from the request page. |
| No CodeFix | Removing the property is trivial in the editor, and the rule is informational. |

## Deliberate non-reports

- Groups, areas, `actionref`s, custom actions, system actions and file-upload actions: only `ActionKind.Action` has a `RunObject` fallback.
- `RunObject` targets other than pages and reports (codeunits, xmlports, queries): they have no caption fallback.
- Obsolete actions, obsolete modify blocks, obsolete base actions and obsolete targets.
- An action property with an empty value, whatever the target declares.
- A target that does not declare the property, or declares only its ML variant: there is nothing to duplicate.
- An action that sets only the ML variant: the compiler rejects `Caption` together with `CaptionML` (AL0428), so ML values are never compared.
- `modify` of a page control: the base page's actions are searched by name, and AL rejects a control and an action with the same name on one page (AL0155), so a control modify finds no action.

## Known issues

- An action in a referenced app's report target reads About* from the request page, which `ReferenceReportTypeSymbol.GetMembers()` omits, so About* duplicates against a report consumed as a symbol package are not reported. A report from another project in the same workspace binds as a source symbol and is reported. The test harness only offers workspace dependency projects, so the symbol-package case has no fixture.
- A `modify` of an action that another page extension added is not reported: `IPageBaseTypeSymbol.FlattenedActions` of the base page lists only the page's own actions.
- The base-action guard looks at the base action only. When two page extensions modify the same property of the same action, the one that equals the target's value is reported although removing it would reveal the other extension's value, not the target's; accepted because it needs two extensions on one action property.

## SDK facts

- The emitter writes `RunObject*TranslationKey` attributes for an `ActionKind.Action` with a page or report `RunObject`, mapping `Caption`, `AboutTitle`, `AboutText` and `ToolTip` to the target; for a report target, About* map to `ReportTypeSymbol.RequestPage` (`Microsoft.Dynamics.Nav.CodeAnalysis.Emit/PageBaseMetadataEmitter.cs` lines 20-26 and 286-327, verified against `../nav-sdk-source` net10.0 at `v30.0.44.47290-prerelease`).
- `IActionSymbol.Target` is not the `RunObject` target: `SourceActionSymbol` only resolves it for `actionref` syntax.
- For a source action, `GetProperty(RunObject).Value` is the bound `ApplicationObjectTypeSymbol` (`BoundApplicationObjectReferencePropertyValue`, `Binder.cs` around line 7915); for an action from a referenced app, `ReferencePropertySymbol.Value` is resolved from the stored string and is not guaranteed to be an object symbol, so the cast must stay null-safe.
- Source label `ValueText` is the label text without quotes; `ReferencePropertySymbol.ValueText` is the string stored in `SymbolReference.json`, which also holds the bare text.
- `ReferenceReportTypeSymbol.GetMembers()` contains methods, labels, global variables, data items and columns, but not the request page; a source report's `GetMembers()` includes its `RequestPageTypeSymbol`.
- The report property table has `TOOLTIP` but no `TOOLTIPML`.
- `RUNOBJECT` in `ObjectParser.GetPageActionProperties` carries neither extension nor customization modifiability, so a `modify` block cannot change the target. `CAPTION` and `TOOLTIP` there are extension-modifiable only; `ABOUTTITLE` and `ABOUTTEXT` are both extension- and customization-modifiable.
- `VersionChecker` reads the runtime from the syntax tree's `ParseOptions` and falls back to `RuntimeVersion.CurrentRelease` when none is set.

## Test notes

- Every test method calls `RequireMinimumVersion("18.0", ...)`: the harness cannot set the app runtime, so it compiles at the SDK's current release, and the 18.0 gate makes the analyzer inert on the 12.0 and 16.0 SDK legs.
- `NoDiagnosticBelowRuntimeGate` sets `ParseOptions` to runtime 17.0 to prove the gate; its fixture must not use a page-object `ToolTip`, which needs 18.0.
- The `ActionGroup` and `ActionRef` NoDiagnostic fixtures mark a property the rule never compares: a group has no `RunObject` and an `actionref` has no label property, so no stronger fixture can be built for them.
