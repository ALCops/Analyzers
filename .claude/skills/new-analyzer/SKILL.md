---
name: new-analyzer
description: Scaffold a new ALCops diagnostic rule end to end — ID, resx, descriptor, analyzer class, tests with .al fixtures, and the rule doc. Use when asked to add/create a new rule, analyzer, or diagnostic.
argument-hint: <ID> <RuleName> <Cop>   e.g. LC0100 AvoidFooBar LinterCop
---

# New analyzer rule

Arguments: `$ARGUMENTS` → `{ID}` (e.g. `LC0100`), `{RuleName}` (PascalCase, also the class/descriptor/test-folder name), `{Cop}` (`ApplicationCop` | `DocumentationCop` | `FormattingCop` | `LinterCop` | `PlatformCop` | `TestAutomationCop`). If any is missing, derive the next free ID from `src/ALCops.{Cop}/DiagnosticIds.cs` and ask for the rest.

Read `.claude/rules/analyzer-development.md`, `.claude/rules/sdk-analyzer-infrastructure.md` and `.claude/rules/testing.md` (they load automatically once you open files under `Analyzers/` and `*.Test/`).

## Steps

1. **Confirm scope.** Restate what the rule detects, what it deliberately does not (false-negative trade-offs), severity, default enabled/disabled, and whether it needs a setting in `ALCopsSettings` + `alcops.schema.json` (see `.claude/rules/settings-schema.md`). Check `DiagnosticIds.cs` that `{ID}` is unused and sequential.
2. **Study the SDK first (mandatory).** Locate the relevant syntax kinds, operation kinds, and symbol members in the decompiled NAV SDK source before writing code; note any API that is absent in `netstandard2.1` (→ version gate or `#if NETSTANDARD2_1` stub, see `.claude/rules/netstandard21-compatibility.md`). Look for an existing analyzer with the same registration shape and reuse its pattern and `ALCops.Common` helpers.
3. **Wire the diagnostic.**
   - `DiagnosticIds.cs`: `public static readonly string {RuleName} = "{ID}";`
   - `ALCops.{Cop}Analyzers.resx`: `{RuleName}Title`, `{RuleName}MessageFormat`, `{RuleName}Description`.
   - `DiagnosticDescriptors.cs`: descriptor with category, severity, `isEnabledByDefault`, help URI `https://alcops.dev/docs/analyzers/{copslug}/{id}/`.
4. **Write the analyzer** in `src/ALCops.{Cop}/Analyzers/{RuleName}.cs`: `[DiagnosticAnalyzer]`, extends `DiagnosticAnalyzer` (not the exception harness — see CLAUDE.md), narrowest `Register*Action`, no cross-callback state accumulation, `GetSymbolSafe` where applicable.
5. **Write tests** in `src/ALCops.{Cop}.Test/Rules/{RuleName}/`: `{RuleName}.cs` from the class template in `testing.md`; `.al` fixtures in `HasDiagnostic/` and `NoDiagnostic/` with `[|...|]` markers in both; fixture names must equal `[TestCase]` names; each fixture self-contained (define referenced tables/enums). Cover every design decision from step 1 with at least one fixture, including the deliberate non-reports.
6. **Build and run:** `dotnet build ALCops.sln` then `dotnet test src/ALCops.{Cop}.Test/ --filter "FullyQualifiedName~{RuleName}"`. Fix until green; report the real output.
7. **Document.** Create `.claude/rules/diagnostics/{id-lowercase}-{kebab-slug}.md` from `.claude/skills/new-analyzer/templates/rule-doc.md`: fill Purpose, every design decision with rationale, Architecture, Known issues. Set `paths` to `src/ALCops.{Cop}/**/{RuleName}*`. No diagnostic-property table, no test-case list.
8. **Remind about the docs site:** a page is required at `../alcops.dev/content/docs/analyzers/{copslug}/{ID}.md` (sibling repo, out of scope for this repo's PR unless asked).
9. Commit as `feat({ID}): <summary>` on a `feat/{id}-<slug>` branch; never on `main`.
