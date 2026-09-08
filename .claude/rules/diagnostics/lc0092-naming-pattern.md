---
paths:
  - "src/ALCops.LinterCop/**/NamingPattern*"
  - "src/ALCops.LinterCop.Test/Rules/NamingPattern/**"
---

# LC0092: NamingPattern

## Purpose

Validates names of procedures, variables, parameters, return values, objects, fields, actions, enum values, and controls against configurable regex patterns. Enforces [Microsoft best practices](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/compliance/apptest-bestpracticesforalcode) and [AL Guidelines](https://alguidelines.dev/docs/agentic-coding/vibe-coding-rules/al-naming-conventions/) naming conventions by default.

Registers `CompilationStartAction` (settings, AppSourceCop affixes, `NamingPatternConfig`) then `SymbolAction` on method, variable, object, field, action, enum-value and control kinds; main type `NamingPattern` with inner `NamingPatternConfig`, `ResolvedPatterns` and `RegexExplainer`.

**References:**
- [BusinessCentral.LinterCop LC0092](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0092) (original rule, re-implemented)
- [MS Docs: Best Practices for AL Code](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/compliance/apptest-bestpracticesforalcode)
- [AL Guidelines: Naming Conventions](https://alguidelines.dev/docs/agentic-coding/vibe-coding-rules/al-naming-conventions/)

## Design decisions

| Decision | Rationale |
|---|---|
| Same ID as BC.LinterCop; Severity Warning | Migration path; a convention violation, not a bug. |
| One diagnostic ID for all 16 naming targets | Simpler user experience; the message names the target. |
| Built-in Microsoft-convention defaults that users may override | Immediate value without configuration. |
| Two-phase pattern resolution: user overrides along the whole inheritance chain first, then built-in defaults | Overriding `Variable` in `alcops.json` applies to `LocalVariable`, `GlobalVariable` and `Parameter` even though those have their own built-in defaults. |
| `LocalVariable`/`GlobalVariable` distinct targets under a pure-parent `Variable`; `Parameter` inherits from `LocalVariable`; `VarParameter` inherits from `Parameter` | Teams want different conventions per scope (e.g. `_` prefix only for locals); parameter naming is closer to local than global conventions, and `var` parameters may warrant their own rules. |
| Object names: strip AppSourceCop affixes and trim whitespace before matching | Avoids false positives on `"PTE MyCodeunit"`-style names where a space separates affix and name. |
| `&` accelerator stripped for Action/Control names only | `&` is the classic Windows keyboard-accelerator prefix inherited from C/SIDE; other targets keep flagging `&` through their disallow pattern. |
| Single-letter variable/parameter names exempt from the uppercase-start rule | Common idiom (`i`, `j`, `k`, `t`), aligned with pylint `good-names`, ESLint `id-length`, Checkstyle `allowOneCharVarInForLoop`. |
| `_` prefix followed by PascalCase allowed for variables/parameters | C# convention used in AL to disambiguate a name colliding with a parameter or type; `_Text` passes, `_text` fails. |
| `x` prefix followed by PascalCase allowed for variables/parameters | Idiomatic "previous record state" convention (`xRec`, `xSalesLine`). |
| `EnumValue` has no built-in default (opt-in only) | Digit-leading enum values are common and not prohibited by Microsoft guidelines ([#321](https://github.com/ALCops/Analyzers/issues/321)). |
| Four-tier message: description, auto-suggestion for recognized patterns, `RegexExplainer` for simple regexes, raw regex fallback; users can supply `AllowDescription`/`DisallowDescription` | Progressive enhancement so most users see a human-readable message; the explainer returns null for constructs it cannot parse rather than guessing. |
| Regex safety: 2-second match timeout, `ArgumentException`/`RegexMatchTimeoutException` caught and the pattern disabled | Protects against ReDoS and invalid user patterns without failing the analysis. |
| `GetAppSourceCopConfiguration` wrapped in try-catch at compilation start, continuing with null affixes | It may throw in minimal (test) runtime environments. |
| Settings loaded through the compilation snapshot with the callback's cancellation token | Retains virtual-file lookup and MemoryFileSystem tests while sharing one configuration with CM0001 and the other cops. |
| Independent of LC0098: a subscriber violating both rules receives two diagnostics | LC0092 constrains the character class of the first character, LC0098 the structural template; their settings are decoupled. Teams whose source objects start lowercase or non-letter (the LC0098 default emits a quoted identifier) should relax `NamingPatterns.EventSubscriber` to accept the leading quote. |

## Deliberate non-reports

- Triggers: platform-defined names.
- Interface implementations: the interface dictates the name.
- Event subscriber parameters: must match the publisher signature (AL0828), and platform trigger parameters (`xRec`, `BelowxRec`, `RunTrigger`, ...) cannot be renamed.
- Controls on API pages/queries: AA0102 requires camelCase, which the default PascalCase pattern would always contradict.
- Whitespace-only names such as `value(0; " ")`: a common "empty" enum value, not a naming issue.
- Enum values, unless a pattern is configured.
- Obsolete symbols (standard ALCops convention).

## Test notes

- Custom patterns are injected as `alcops.json` through a `MemoryFileSystem`; `NamingPatternSettings.cs` unit-tests the inheritance-chain resolution of `NamingPatternConfig` directly.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `NamingPatterns` | unset (built-in defaults) | Dictionary keyed by target name (case-insensitive) with `AllowPattern`, `DisallowPattern`, `AllowDescription`, `DisallowDescription`; unresolved targets inherit along `VarParameter -> Parameter -> LocalVariable -> Variable` and `LocalProcedure/GlobalProcedure/EventSubscriber/EventDeclaration -> Procedure`. Built-in defaults: `^[A-Z]` for procedures, objects, actions, controls and return values; `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` for variables and parameters (disallow `[%&!?]` for variables only); `^[A-Za-z]` with disallow `[%&!?]` for fields; none for `EnumValue`. |
