---
paths:
  - "src/ALCops.LinterCop/**/NamingPattern*"
  - "src/ALCops.LinterCop.Test/Rules/NamingPattern/**"
---

# LC0092: NamingPattern

## Purpose

Validates names of procedures, variables, parameters, return values, objects (including tableextension, pageextension, reportextension, enumextension, permissionsetextension, profileextension and pagecustomization), fields, actions, enum values, and controls against configurable regex patterns. Enforces [Microsoft best practices](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/compliance/apptest-bestpracticesforalcode) and [AL Guidelines](https://alguidelines.dev/docs/agentic-coding/vibe-coding-rules/al-naming-conventions/) naming conventions by default.

Registers `CompilationStartAction` (settings, AppSourceCop affixes, `NamingPatternConfig`) then `SymbolAction` on method, variable, object, field, action, enum-value and control kinds; main type `NamingPattern` with inner `NamingPatternConfig`, `ResolvedPatterns` and `RegexExplainer`.

**References:**
- [BusinessCentral.LinterCop LC0092](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0092) (original rule, re-implemented)
- [MS Docs: Best Practices for AL Code](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/compliance/apptest-bestpracticesforalcode)
- [AL Guidelines: Naming Conventions](https://alguidelines.dev/docs/agentic-coding/vibe-coding-rules/al-naming-conventions/)
- [#655](https://github.com/ALCops/Analyzers/issues/655) (interface members and interface implementers)

## Design decisions

| Decision | Rationale |
|---|---|
| Same ID as BC.LinterCop; Severity Warning | Migration path; a convention violation, not a bug. |
| One diagnostic ID for all 16 naming targets | Simpler user experience; the message names the target. |
| Built-in Microsoft-convention defaults that users may override | Immediate value without configuration. |
| Two-phase pattern resolution: user overrides along the whole inheritance chain first, then built-in defaults | Overriding `Variable` in `alcops.json` applies to `LocalVariable`, `GlobalVariable` and `Parameter` even though those have their own built-in defaults. |
| `LocalVariable`/`GlobalVariable` distinct targets under a pure-parent `Variable`; `Parameter` inherits from `LocalVariable`; `VarParameter` inherits from `Parameter` | Teams want different conventions per scope (e.g. `_` prefix only for locals); parameter naming is closer to local than global conventions, and `var` parameters may warrant their own rules. |
| Object names: strip AppSourceCop affixes and trim whitespace before matching | Avoids false positives on `"PTE MyCodeunit"`-style names where a space separates affix and name. |
| Extension object names (tableextension, pageextension, reportextension, enumextension, permissionsetextension, profileextension, pagecustomization) use the `Object` target | One convention for everything a developer names as an object; a separate target would widen the settings schema for no known use. |
| `&` accelerator stripped for Action/Control names only | `&` is the classic Windows keyboard-accelerator prefix inherited from C/SIDE; other targets keep flagging `&` through their disallow pattern. |
| Single-letter variable/parameter names exempt from the uppercase-start rule | Common idiom (`i`, `j`, `k`, `t`), aligned with pylint `good-names`, ESLint `id-length`, Checkstyle `allowOneCharVarInForLoop`. |
| `_` prefix followed by PascalCase allowed for variables/parameters | C# convention used in AL to disambiguate a name colliding with a parameter or type; `_Text` passes, `_text` fails. |
| `x` prefix followed by PascalCase allowed for variables/parameters | Idiomatic "previous record state" convention (`xRec`, `xSalesLine`). |
| `EnumValue` has no built-in default (opt-in only) | Digit-leading enum values are common and not prohibited by Microsoft guidelines ([#321](https://github.com/ALCops/Analyzers/issues/321)). |
| Four-tier message: description, auto-suggestion for recognized patterns, `RegexExplainer` for simple regexes, raw regex fallback; users can supply `AllowDescription`/`DisallowDescription` | Progressive enhancement so most users see a human-readable message; the explainer returns null for constructs it cannot parse rather than guessing. |
| Regex safety: 2-second match timeout, `ArgumentException`/`RegexMatchTimeoutException` caught and the pattern disabled | Protects against ReDoS and invalid user patterns without failing the analysis. |
| `GetAppSourceCopConfiguration` wrapped in try-catch at compilation start, continuing with null affixes | It may throw in minimal (test) runtime environments. |
| Settings loaded through the compilation snapshot with the callback's cancellation token | Retains virtual-file lookup and MemoryFileSystem tests while sharing one configuration with CM0001 and the other cops. |
| Filter `SymbolKind.Action` by `ActionKind` and `SymbolKind.Control` by `ControlKind` instead of adding naming targets | Each of those two symbol kinds covers several AL constructs, only some of which carry a developer-chosen name; new targets (`ActionArea`, `SystemAction`, ...) would enlarge the settings schema for kinds nobody can rename. Ordinary groups stay under `Action` and every other control kind under `Control`. |
| Interface members are checked fully (method name, parameters, named return value), abstract and default-bodied alike; a codeunit procedure that implements an interface method skips only the method-name check | The interface author picks every name the interface declares, so one interface must not get a different result per member kind. An implementer cannot rename the method, but parameter and return-value names are not part of the interface contract: AL matches an implementation by name and signature only, so the implementer picks its own parameter names and they follow the same convention as any procedure ([#655](https://github.com/ALCops/Analyzers/issues/655)). Methods inherited through `interface B extends A` count as implemented, so their names are skipped too. |
| Independent of LC0098: a subscriber violating both rules receives two diagnostics | LC0092 constrains the character class of the first character, LC0098 the structural template; their settings are decoupled. Teams whose source objects start lowercase or non-letter (the LC0098 default emits a quoted identifier) should relax `NamingPatterns.EventSubscriber` to accept the leading quote. |

## Deliberate non-reports

- Triggers: platform-defined names.
- The method name of an interface implementation (including one inherited through `extends`): the interface dictates it. Its parameters and named return value are still checked.
- Control add-in procedures and events (`DeclareMethod` outside an interface): the JavaScript add-in fixes their names.
- Event subscriber parameters: must match the publisher signature (AL0828), and platform trigger parameters (`xRec`, `BelowxRec`, `RunTrigger`, ...) cannot be renamed.
- Controls on API pages/queries: AA0102 requires camelCase, which the default PascalCase pattern would always contradict.
- Whitespace-only names such as `value(0; " ")`: a common "empty" enum value, not a naming issue.
- Action areas (`area(Processing)`, `area(Promoted)`, ...) and layout areas (`area(Content)`, `area(FactBoxes)`, ...): the name selects a platform area, so it is fixed by `ActionAreaKind` / the page layout rather than chosen.
- `systemaction(OK)` and friends: the name selects a `SystemActionKind` member.
- Action groups named after a predefined promoted category (`Category_New`, `Category_Process`, `Category_Report`, `Category_Category4` .. `Category_Category20`): the name binds the group to that platform category slot. AC0011 skips them through the same Common helper.
- Enum values, unless a pattern is configured.
- Obsolete symbols (standard ALCops convention).

## SDK facts

- Every node inside an `actions { }` block is a single `SymbolKind.Action` symbol; `IActionSymbol.ActionKind` is the only thing distinguishing `area` from `group`, `action`, `separator`, `actionref`, `customaction`, `systemaction` and `fileuploadaction`. `SymbolKind.Control` is shaped the same way, with `IControlSymbol.ControlKind` telling the layout `area` apart from groups, fields and parts.
- Action and control names come from the source text (`syntax.Name.Unquoted()`), not from the canonical enum member, so `area(processing)` really is named `processing` and fails an uppercase-start pattern.
- `ActionKind.SystemAction` is absent at the 12.0 SDK floor and present by 16.0, so `EnumProvider` resolves it through the string overload with an out-of-range sentinel: `default(ActionKind)` is `Area`, and falling back to it would make every action area read as a system action.
- The predefined promoted-category test lives in Common as `IActionSymbol.IsPredefinedPromotedCategoryGroup()` and is shared with AC0011, so both cops skip exactly the same groups. It wraps `SyntaxFacts.PromotedCategoriesSynthesizedSymbolNames`, an `ImmutableHashSet<string>` the SDK already builds over `PromotedCategoryKind` with a case-insensitive comparer, so no local copy or lowercasing is needed.
- A source method without a body has `MethodKind.DeclareMethod` (`SourceMethodSymbol.MethodKind`), so abstract interface members share the kind with control add-in procedures and events, so the analyzer accepts `DeclareMethod` only when `ContainingSymbol.Kind` is `SymbolKind.Interface`. A default-bodied member (runtime 18.0) has `MethodKind.Method` and the interface as its `ContainingSymbol`. Interface members cannot be `local`, so `ClassifyMethod` maps them to `GlobalProcedure`.
- `ICodeunitTypeSymbol.ImplementedInterfaces` is the declared `implements` list. It does not include the interfaces those extend.
- The Common helper `MethodImplementsInterfaceMethod` therefore also walks `IInterfaceTypeSymbol.GetAllExtendedInterfaces()`. The result is transitive and excludes the interface itself.
- `GetAllExtendedInterfaces()` arrived with interface `extends` in SDK 14.0 (`Feature.InterfaceExtends`, runtime Fall2024). The 12.0 SDK that the netstandard2.1 binary compiles against does not have it. `GetAllExtendedInterfacesWithReflection` therefore calls it directly on net8.0+ and through a cached reflection delegate in the netstandard2.1 build. On SDKs without `extends`, the delegate returns an empty array.
- The seven extension and customization `SymbolKind` members exist at the 12.0 SDK floor, so `EnumProvider` resolves them by name without a fallback sentinel.

## Test notes

- Custom patterns are injected as `alcops.json` through a `MemoryFileSystem`; `NamingPatternSettings.cs` unit-tests the inheritance-chain resolution of `NamingPatternConfig` directly.
- Extension object fixtures declare their target in the same file, which the compiler accepts only from SDK 13.0, so they are gated with `SkipTestIfVersionIsTooLow(..., "13.0", ...)`. The profileextension fixtures are also gated on `"14.0"`: `Feature.ProfileExtensions` requires runtime Fall2024. Profile extensions and page customizations have their own fixtures so those gates stay narrow.
- `InterfaceDefaultMemberLowerCase` is gated at 18.0: default interface method bodies need runtime 18.0 and the harness compiles at the SDK's current release. It holds an abstract and a default-bodied member side by side, so it pins that both kinds get the same result.
- `InterfaceExtendedImplementingMethod` and `InterfaceExtendedImplementingMethodLowerCaseParameter` are gated at 14.0, the first SDK that compiles interface `extends`. The first marks a lowercase implementer method name inherited from the base interface, so it proves the name skip through `extends` (AL names are case-insensitive, so the lowercase name still implements the member). The second marks a lowercase parameter of such an implementer, so it proves the parameter check still runs there.
- The `systemaction` fixture is gated with `SkipTestIfVersionIsTooLow(..., "16.2.31", ...)`. `systemaction` itself is absent at the 12.0 SDK floor and present by 16.0, but the gate is set by the fixture's `ConfigurationDialog` page type: SDKs up to 16.2.28 reject it as a feature under development (AL0574), and 16.2.31 downgrades that to a public-preview warning. The gate therefore follows the first SDK that compiles the page type, not the parser or the action. The page also needs `Extensible = false` (AL0223) and a system-action name the page type supports (`Ok` or `Cancel`).

## Settings

| Setting | Default | Effect |
|---|---|---|
| `NamingPatterns` | unset (built-in defaults) | Dictionary keyed by target name (case-insensitive) with `AllowPattern`, `DisallowPattern`, `AllowDescription`, `DisallowDescription`; unresolved targets inherit along `VarParameter -> Parameter -> LocalVariable -> Variable` and `LocalProcedure/GlobalProcedure/EventSubscriber/EventDeclaration -> Procedure`. Built-in defaults: `^[A-Z]` for procedures, objects, actions, controls and return values; `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` for variables and parameters (disallow `[%&!?]` for variables only); `^[A-Za-z]` with disallow `[%&!?]` for fields; none for `EnumValue`. |
