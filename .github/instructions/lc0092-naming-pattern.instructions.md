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
| Single diagnostic ID | LC0092 for all 13 naming targets | Simpler user experience; message differentiates targets |
| Settings format | `NamingPatterns` dictionary in alcops.json | PascalCase keys, named per target |
| Default behavior | Built-in MS convention defaults, user can override | Immediate value without configuration |
| Object affixes | Strip AppSourceCop affixes before checking, trim whitespace | Avoids false positives on prefixed/suffixed names; handles common `"PTE MyCodeunit"` pattern where space separates affix from name |
| Skip triggers | Yes | Platform-defined names, can't rename |
| Skip interface implementations | Yes | Name is dictated by the interface |
| Skip event subscriber params | Yes | Subscriber parameters must match publisher signature (AL0828); platform trigger params (`xRec`, `BelowxRec`, `RunTrigger`, etc.) can't be renamed |
| Skip API object controls | Yes | API page/query controls require camelCase per AA0102; the default PascalCase pattern would always conflict |
| Skip whitespace-only names | Yes | `value(0; " ")` is a common "empty" enum value pattern; not a naming issue |
| Strip `&` accelerator for Action/Control | Yes | `&` before a character in action/control/group names is a Windows UI keyboard accelerator prefix (Alt+key shortcut), inherited from classic NAV/C/SIDE. Not stripped for other targets so fields/variables still flag `&` via their disallow pattern. |
| Skip obsolete | Yes | Standard ALCops convention |
| netstandard2.1 | Full support | No net8.0-only APIs used |
| Regex safety | 2-second timeout, catch ArgumentException and RegexMatchTimeoutException | Protects against ReDoS |
| AppSourceCop exception handling | try-catch in CompilationStart | `GetAppSourceCopConfiguration` may throw in test contexts |
| Message UX | Four-tier strategy: description → suggestion → regex explainer → raw regex | Progressive enhancement; most users see human-readable messages |
| Built-in descriptions | Hardcoded per default pattern | Best UX for out-of-box experience |
| AllowDescription/DisallowDescription | Optional user-provided description fields in settings | Users can provide custom descriptions for their custom patterns |
| Auto-suggestion | Pattern-specific name transformation | `^[A-Z]` capitalizes first char, `^(?:[A-Za-z]$\|[A-Z])` capitalizes first char for multi-char names only, `[%&!?]` removes disallowed chars |
| RegexExplainer | Mini parser for common constructs (char classes, anchors) | Translates simple regex to English when no description available |
| Single-letter variable/parameter names | Exempt from uppercase-start requirement | Common idiom (`i`, `j`, `k` for loops, `t` for text). Aligned with pylint `good-names`, ESLint `id-length`, Checkstyle `allowOneCharVarInForLoop`. Default pattern changed from `^[A-Z]` to `^(?:[A-Za-z]$\|[A-Z])` for Variable and Parameter targets |
| Underscore prefix for variables/parameters | Allow `_` followed by PascalCase | C# convention used in AL for variable disambiguation when the name collides with a parameter or type. PascalCase enforced after `_` (`_Text` passes, `_text` fails) to stay consistent with AL conventions. Pattern: `_[A-Z]` added to Variable and Parameter defaults |
| xRec prefix for variables/parameters | Allow `x` followed by PascalCase | Idiomatic AL convention for "previous record state" (e.g., `xSalesLine`). The platform uses `Rec`/`xRec`; developers extend this pattern to custom variables. Pattern: `x[A-Z]` added to Variable and Parameter defaults |

## Architecture

### Registration strategy

Uses `RegisterCompilationStartAction` to:
1. Load `ALCopsSettings` (for NamingPatterns user overrides)
2. Load AppSourceCop affixes (for object name stripping, wrapped in try-catch)
3. Build `NamingPatternConfig` (resolves effective patterns per target)
4. Register `RegisterSymbolAction` for all relevant SymbolKinds

### NamingTarget enum (13 values)

| Target | SymbolKind registration | Classification logic |
|---|---|---|
| Procedure | Method | (parent, never directly used for checking) |
| LocalProcedure | Method | `method.IsLocal == true` |
| GlobalProcedure | Method | `!IsLocal && !IsEvent` |
| EventSubscriber | Method | `AttributeKind.EventSubscriber` |
| EventDeclaration | Method | `AttributeKind.IntegrationEvent` or `AttributeKind.BusinessEvent` |
| Variable | LocalVariable, GlobalVariable | Direct registration |
| Parameter | Method | Iterate `method.Parameters` |
| ReturnValue | Method | `method.ReturnValueSymbol` |
| Object | Table, Page, Codeunit, Report, Query, XmlPort, Enum, Interface, PermissionSet | Direct registration |
| Field | Field | Direct registration |
| Action | Action | Direct registration |
| EnumValue | EnumValue | Direct registration |
| Control | Control | Direct registration |

### NamingPatternConfig (inner class)

Resolves effective patterns per target using a three-level cascade:

1. **User override** for the specific target (from `alcops.json`)
2. **User override for parent** (sub-targets inherit from Procedure)
3. **Built-in defaults** (hardcoded in `BuiltInDefaults` dictionary)

Inheritance map: LocalProcedure, GlobalProcedure, EventSubscriber, EventDeclaration all inherit from Procedure.

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

| Target | AllowPattern | DisallowPattern |
|---|---|---|
| Procedure | `^[A-Z]` | (none) |
| Variable | `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` | `[%&!?]` |
| Parameter | `^(?:[A-Za-z]$\|[A-Z]\|_[A-Z]\|x[A-Z])` | (none) |
| ReturnValue | `^[A-Z]` | (none) |
| Object | `^[A-Z]` | (none) |
| Field | `^[A-Za-z]` | `[%&!?]` |
| Action | `^[A-Z]` | (none) |
| EnumValue | `^[A-Z]` | (none) |
| Control | `^[A-Z]` | (none) |

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

**HasDiagnostic (10 cases):** ProcedureLowerCaseStart, VariableLowerCaseStart, VariableWithSpecialChars, ParameterLowerCaseStart, ReturnValueLowerCaseStart, ObjectLowerCaseStart, FieldWithSpecialChars, EnumValueLowerCaseStart, ActionLowerCaseStart, ControlLowerCaseStart.
**NoDiagnostic (17 cases):** ProcedurePascalCase, VariablePascalCase, FieldWithLettersAndDigits, ObsoleteProcedure, TriggerMethod, InterfaceImplementingMethod, EventSubscriberPascalCase, EventSubscriberPlatformParams, EventSubscriberUserParams, ApiPageControlCamelCase, ActionAcceleratorKey, EnumValueBlankSpace, SingleLetterVariable, SingleLetterParameter, UnderscorePrefix, XRecVariable, XRecParameter, ParameterPascalCase.

## Phase 2 roadmap (not yet implemented)

- **Custom pattern tests**: Test cases that inject custom NamingPatterns via alcops.json
- **Object affix stripping tests**: Test cases with AppSourceCop configuration
- **ControlAddIn object type**: Add ControlAddIn to the Object target registration
- **Procedure sub-target inheritance tests**: Verify LocalProcedure/GlobalProcedure/EventSubscriber/EventDeclaration inheritance
- **Per-object-type overrides**: Allow different patterns for Table vs Codeunit vs Page names
- **CodeFix**: Auto-rename to match the pattern (complex, requires semantic analysis)
