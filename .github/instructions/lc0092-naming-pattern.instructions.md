---
applyTo: 'src/ALCops.LinterCop/**/NamingPattern*'
---

# LC0092: NamingPattern

## Purpose

Validates names of procedures, variables, parameters, return values, objects, fields, actions, enum values, and controls against configurable regex patterns. Enforces [Microsoft best practices](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/compliance/apptest-bestpracticesforalcode) and [AL Guidelines](https://alguidelines.dev/docs/agentic-coding/vibe-coding-rules/al-naming-conventions/) naming conventions by default.

**References:**
- [BusinessCentral.LinterCop LC0092](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0092) (original rule, re-implemented)
- [MS Docs: Best Practices for AL Code](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/compliance/apptest-bestpracticesforalcode)
- [AL Guidelines: Naming Conventions](https://alguidelines.dev/docs/agentic-coding/vibe-coding-rules/al-naming-conventions/)

## Diagnostic properties

**LC0092** · Category: Naming · Severity: Warning · Enabled: true
Message: `{0} name "{1}" {2}`
No version gate · Full netstandard2.1 support

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | LinterCop (LC0092) | Code quality convention; reuse same ID for migration from BusinessCentral.LinterCop |
| Severity | Warning | Convention violation, not a bug |
| Single diagnostic ID | LC0092 for all 16 naming targets | Simpler user experience; message differentiates targets |
| Settings format | `NamingPatterns` dictionary in alcops.json | PascalCase keys, named per target |
| Default behavior | Built-in MS convention defaults, user can override | Immediate value without configuration |
| Object affixes | Strip AppSourceCop affixes before checking, trim whitespace | Avoids false positives on prefixed/suffixed names; handles common `"PTE MyCodeunit"` pattern where space separates affix from name |
| Skip triggers | Yes | Platform-defined names, can't rename |
| Skip interface implementations | Yes | Name is dictated by the interface |
| Skip event subscriber params | Yes | Subscriber parameters must match publisher signature (AL0828); platform trigger params (`xRec`, `BelowxRec`, `RunTrigger`, etc.) can't be renamed |
| Skip API object controls | Yes | API page/query controls require camelCase per AA0102; the default PascalCase pattern would always conflict |
| Skip whitespace-only names | Yes | `value(0; " ")` is a common "empty" enum value pattern; not a naming issue |
| EnumValue has no built-in default | Opt-in only via alcops.json | Issue #321: enum values starting with digits are common in AL and not prohibited by MS guidelines |
| Settings loading uses IFileSystem | `GetSettings(fileSystem)` aligns with TranslatableTextShouldBeTranslated pattern | Respects SDK file abstraction; enables testing with MemoryFileSystem |
| Strip `&` accelerator for Action/Control | Yes | `&` before a character in action/control/group names is a Windows UI keyboard accelerator prefix (Alt+key shortcut), inherited from classic NAV/C/SIDE. Not stripped for other targets so fields/variables still flag `&` via their disallow pattern. |
| Skip obsolete | Yes | Standard ALCops convention |
| netstandard2.1 | Full support | No net8.0-only APIs used |
| Regex safety | 2-second timeout, catch ArgumentException and RegexMatchTimeoutException | Protects against ReDoS |
| AppSourceCop exception handling | try-catch in CompilationStart | `GetAppSourceCopConfiguration` may throw in test contexts |
| Message UX | Four-tier strategy: description → suggestion → regex explainer → raw regex | Progressive enhancement; most users see human-readable messages |
| Built-in descriptions | Hardcoded per default pattern | Best UX for out-of-box experience |
| AllowDescription/DisallowDescription | Optional user-provided description fields in settings | Users can provide custom descriptions for their custom patterns |
| Single-letter variable/parameter names | Exempt from uppercase-start requirement | Common idiom (`i`, `j`, `k` for loops, `t` for text). Aligned with pylint `good-names`, ESLint `id-length`, Checkstyle `allowOneCharVarInForLoop`. Default pattern changed from `^[A-Z]` to `^(?:[A-Za-z]$\|[A-Z])` for LocalVariable, GlobalVariable, and Parameter targets |
| Underscore prefix for variables/parameters | Allow `_` followed by PascalCase | C# convention used in AL for variable disambiguation when the name collides with a parameter or type. PascalCase enforced after `_` (`_Text` passes, `_text` fails) to stay consistent with AL conventions. Pattern: `_[A-Z]` added to LocalVariable, GlobalVariable, and Parameter defaults |
| xRec prefix for variables/parameters | Allow `x` followed by PascalCase | Idiomatic AL convention for "previous record state" (e.g., `xSalesLine`). The platform uses `Rec`/`xRec`; developers extend this pattern to custom variables. Pattern: `x[A-Z]` added to LocalVariable, GlobalVariable, and Parameter defaults |
| Auto-suggestion | Pattern-specific name transformation | `^[A-Z]` capitalizes first char, `^(?:[A-Za-z]$\|[A-Z])` capitalizes first char for multi-char names only, `[%&!?]` removes disallowed chars |
| RegexExplainer | Mini parser for common constructs (char classes, anchors) | Translates simple regex to English when no description available |
| LocalVariable vs GlobalVariable as distinct targets | Yes | Enables different conventions for local vs global variables (e.g. `_` prefix only for locals). `Variable` remains as pure fallback parent, never directly dispatched to. |
| Parameter inherits from LocalVariable | Yes | Parameter naming is closer to local variable conventions than global. Multi-level chain: Parameter → LocalVariable → Variable. |
| VarParameter as distinct target | Yes | `var` parameters are passed by reference; teams may want different conventions (e.g. no underscore prefix). Inherits from `Parameter` → `LocalVariable` → `Variable`. |

## Architecture

### Registration strategy

Uses `RegisterCompilationStartAction` to:
1. Load `ALCopsSettings` (for NamingPatterns user overrides)
2. Load AppSourceCop affixes (for object name stripping, wrapped in try-catch)
3. Build `NamingPatternConfig` (resolves effective patterns per target)
4. Register `RegisterSymbolAction` for all relevant SymbolKinds

### NamingTarget enum (16 values)

| Target | SymbolKind registration | Classification logic |
|---|---|---|
| Procedure | Method | Parent target, never directly dispatched to |
| LocalProcedure | Method | `method.IsLocal == true` |
| GlobalProcedure | Method | `!IsLocal && !IsEvent` |
| EventSubscriber | Method | `AttributeKind.EventSubscriber` |
| EventDeclaration | Method | `AttributeKind.IntegrationEvent` or `AttributeKind.BusinessEvent` |
| Variable | — | Parent target, never directly dispatched to |
| LocalVariable | LocalVariable | `SymbolKind.LocalVariable` |
| GlobalVariable | GlobalVariable | `SymbolKind.GlobalVariable` |
| Parameter | Method | `parameter.IsVar == false` in `method.Parameters` |
| VarParameter | Method | `parameter.IsVar == true` in `method.Parameters` |
| ReturnValue | Method | `method.ReturnValueSymbol` |
| Object | Table, Page, Codeunit, Report, Query, XmlPort, Enum, Interface, PermissionSet | Direct registration |
| Field | Field | Direct registration |
| Action | Action | Direct registration |
| EnumValue | EnumValue | Direct registration |
| Control | Control | Direct registration |

### NamingPatternConfig (inner class)

Resolves effective patterns per target using a two-phase chain walk:

1. **Phase 1 — User overrides**: Walk the inheritance chain from most specific to most general; return the first match.
2. **Phase 2 — Built-in defaults**: Walk the same chain; return the first match.

Inheritance chains:
- `LocalProcedure`, `GlobalProcedure`, `EventSubscriber`, `EventDeclaration` → `Procedure`
- `LocalVariable`, `GlobalVariable` → `Variable`
- `Parameter` → `LocalVariable` → `Variable`
- `VarParameter` → `Parameter` → `LocalVariable` → `Variable`

`Variable` and `Procedure` are pure parent targets with no SymbolKind registration. Overriding `Variable` in `alcops.json` automatically applies to `LocalVariable`, `GlobalVariable`, and `Parameter` unless those have their own override.

Returns `ResolvedPatterns` containing: compiled `Regex`, original pattern string, and description (for both allow and disallow).

### ResolvedPatterns (inner class)

Carries all resolved data per target:
- `AllowRegex` / `DisallowRegex`: Compiled `Regex` objects (null if no pattern)
- `AllowPatternString` / `DisallowPatternString`: Original pattern strings (for fallback display)
- `AllowDescription` / `DisallowDescription`: Human descriptions (built-in or user-provided)

### Diagnostic message assembly (BuildMessage)

Four-tier priority, first match wins:
1. **Description**: From built-in defaults or user `AllowDescription`/`DisallowDescription`
2. **Auto-suggestion**: For recognized patterns, appends `. Consider: "SuggestedName"` 
3. **Regex explainer**: `RegexExplainer.TryExplain()` translates simple patterns to English
4. **Raw regex**: `must match pattern "{regex}"` fallback

### RegexExplainer (inner static class)

Translates common regex constructs to English. Handles:
- Anchors: `^` (start), `$` (end)
- Character classes: `[A-Z]`, `[a-z]`, `[A-Za-z]`, `[A-Za-z0-9]`, `[0-9]`
- Literal character lists: `[%&!?]` → "any of: %, &, !, ?"
- Quantifiers: `*`, `+`, `?` (silently consumed)

Returns null for patterns it can't parse (complex quantifiers, groups, lookahead/lookbehind).

### Auto-suggestion generators (TryGenerateSuggestion)

Pattern-specific name transformations:
- `^[A-Z]` (allow) → Capitalize first character
- `^[a-z]` (allow) → Lowercase first character
- `[%&!?]` (disallow) → Remove matching characters

### Default patterns

| Target | AllowPattern | DisallowPattern | Note |
|---|---|---|---|
| Procedure | `^[A-Z]` | (none) | Parent; own built-in default |
| Variable | `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` | `[%&!?]` | Parent; own built-in default |
| LocalVariable | `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` | `[%&!?]` | Own built-in default |
| GlobalVariable | `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` | `[%&!?]` | Own built-in default |
| Parameter | `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` | (none) | Own built-in default; no disallow |
| VarParameter | `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` | (none) | Own built-in default; inherits from Parameter |
| ReturnValue | `^[A-Z]` | (none) | |
| Object | `^[A-Z]` | (none) | |
| Field | `^[A-Za-z]` | `[%&!?]` | |
| Action | `^[A-Z]` | (none) | |
| EnumValue | (none, opt-in only) | (none) | |
| Control | `^[A-Z]` | (none) | |

### Settings schema (alcops.json)

```json
{
  "NamingPatterns": {
    "Procedure": { "AllowPattern": "^[A-Z]", "DisallowPattern": "" },
    "LocalProcedure": { "AllowPattern": "^[a-z]", "AllowDescription": "should start with a lowercase letter" }
  }
}
```

- Settings POCO: `ALCops.Common.Settings.NamingPattern` (AllowPattern, DisallowPattern, AllowDescription, DisallowDescription)
- ALCopsSettings property: `Dictionary<string, NamingPattern>? NamingPatterns`
- Lookup is case-insensitive on target name keys

## Known issues and workarounds

### AppSourceCop exception in test contexts

`AppSourceCopConfigurationProvider.GetAppSourceCopConfiguration(compilation)` may throw exceptions in test contexts where the SDK runtime environment is minimal. The `CompilationStart` method wraps `GetAffixes` in a try-catch and continues with null affixes.

### Regex compilation failures

Invalid user-supplied patterns fail gracefully: `CompilePattern` catches `ArgumentException` and returns null, effectively disabling that pattern check.

## Test coverage

**HasDiagnostic (9 cases):** ProcedureLowerCaseStart, VariableLowerCaseStart, VariableWithSpecialChars, ParameterLowerCaseStart, ReturnValueLowerCaseStart, ObjectLowerCaseStart, FieldWithSpecialChars, ActionLowerCaseStart, ControlLowerCaseStart.
**NoDiagnostic (18 cases):** ProcedurePascalCase, VariablePascalCase, FieldWithLettersAndDigits, ObsoleteProcedure, TriggerMethod, InterfaceImplementingMethod, EventSubscriberPascalCase, EventSubscriberPlatformParams, EventSubscriberUserParams, ApiPageControlCamelCase, ActionAcceleratorKey, EnumValueBlankSpace, EnumValueLowerCaseStart, SingleLetterVariable, SingleLetterParameter, UnderscorePrefix, XRecVariable, XRecParameter, ParameterPascalCase.
**HasDiagnosticWithCustomSettings (1 case):** EnumValueLowerCaseStartCustomSettings.

## Phase 2 roadmap (not yet implemented)

- **Custom pattern tests**: Test cases that inject custom NamingPatterns via alcops.json
- **Object affix stripping tests**: Test cases with AppSourceCop configuration
- **ControlAddIn object type**: Add ControlAddIn to the Object target registration
- **Procedure sub-target inheritance tests**: Verify LocalProcedure/GlobalProcedure/EventSubscriber/EventDeclaration inheritance
- **Variable sub-target inheritance tests**: Verify LocalVariable/GlobalVariable/Parameter multi-level fallback chain
- **Per-object-type overrides**: Allow different patterns for Table vs Codeunit vs Page names
- **CodeFix**: Auto-rename to match the pattern (complex, requires semantic analysis)
