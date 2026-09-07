---
name: new-analyzer
description: Scaffold a new ALCops diagnostic rule end to end — ID, resx, descriptor, analyzer class, tests with .al fixtures, and the rule doc. Use when asked to add/create a new rule, analyzer, or diagnostic.
argument-hint: <ID> <RuleName> <Cop>   e.g. LC0100 AvoidFooBar LinterCop
---

# New analyzer rule

**Not for:** a wrong or missing diagnostic in an existing rule → `/fix-false-positive`; a fix for an existing rule → `/new-codefix`.

Arguments: `$ARGUMENTS` → `{ID}`, `{RuleName}` (PascalCase; also the class, descriptor, `DiagnosticIds` field and test-folder name), `{Cop}` (`ApplicationCop` | `DocumentationCop` | `FormattingCop` | `LinterCop` | `PlatformCop` | `TestAutomationCop`).

Knowledge you need loads automatically when you open files under `Analyzers/` and `*.Test/`: `.claude/rules/analyzer-development.md`, `sdk-analysis-scope.md`, `symbol-resolution.md`, `record-receiver-forms.md`, `analyzer-performance.md`, `netstandard21-compatibility.md`, `testing.md`.

## Confirm rule parameters (hard gate)

**STOP. Do not create or edit any file until the user has confirmed every parameter below.** Do not invent defaults for anything the user or the issue did not state. Ask once, as a single checklist (see `references/rule-parameters.md` for the allowed values):

| Parameter | Propose or ask? |
|---|---|
| `{ID}` | **Propose** — computed as the next free sequential ID in `src/ALCops.{Cop}/DiagnosticIds.cs`; state it, verify it is unused. |
| `{RuleName}` | Propose from the request; confirm. |
| What is reported, and what is deliberately **not** (false-negative trade-offs) | **Ask** — one sentence each. |
| `Category` | **Ask** |
| `DefaultSeverity` | **Ask** |
| `isEnabledByDefault` | **Ask** |
| CodeFix now / later / never | **Ask** |
| Configurable via `alcops.json`? (name + default) | **Ask** |
| Minimum BC/SDK version (version gate) or net8.0-only SDK API | **Propose** after step 1 below; confirm. |

## Steps

1. **Study the SDK first (mandatory).** Start with `/nav-sdk-docs:sdk-lookup` for every syntax kind, operation kind, symbol member and registration you plan to use; read the docs page it names (registrations and passes: `docs/50-diagnostics/`, operations: `docs/40-operations/`, symbols: `docs/20-symbols/`) and open `../nav-sdk-source` only for what the docs do not cover. Record the four availability cells of every member that is not `yes` at `ns2.0 12.0` (→ version gate or `#if NETSTANDARD2_1` stub) and feed them into the gate above. Find an existing analyzer with the same registration shape and reuse its pattern and `ALCops.Common` helpers.
2. **Wire the diagnostic.** `DiagnosticIds.cs` field → `ALCops.{Cop}Analyzers.resx` (`{RuleName}Title`, `{RuleName}MessageFormat`, `{RuleName}Description`) → `DiagnosticDescriptors.cs` entry with help URI `https://alcops.dev/docs/analyzers/{copslug}/{id}/`. If configurable: `ALCopsSettings.cs` + `alcops.schema.json` together (`.claude/rules/settings-schema.md`). Code shapes: `references/wiring.md`.
3. **Write the analyzer** in `src/ALCops.{Cop}/Analyzers/{RuleName}.cs`: `[DiagnosticAnalyzer]`, `sealed`, extends plain `DiagnosticAnalyzer`, narrowest `Register*Action`, no state carried across callbacks, `IsObsolete()` check first, `GetSymbolSafe()` in operation callbacks, report at the most specific location. Templates per registration shape: `references/analyzer-template.md`.
4. **Write tests** in `src/ALCops.{Cop}.Test/Rules/{RuleName}/`: class from `references/test-class-template.md`; `.al` fixtures in `HasDiagnostic/` and `NoDiagnostic/` with `[|...|]` markers in both; one fixture per confirmed design decision, including the deliberate non-reports.
5. **Build and run:** `dotnet build ALCops.sln`, then `dotnet test src/ALCops.{Cop}.Test/ --filter "FullyQualifiedName~{RuleName}"`. Report the real output.
   Then run `/code-review` on the branch: it applies `REVIEW.md` (house rules and the NAV SDK checklist). Fix or justify every correctness finding before committing.
6. **Document.** Create `.claude/rules/diagnostics/{id-lowercase}-{kebab-slug}.md` from `references/rule-doc.md` with `paths: src/ALCops.{Cop}/**/{RuleName}*`; every gate answer becomes a Design-decision row with its rationale, and every deliberate non-report becomes a bullet under Deliberate non-reports.
7. **Remind:** docs-site page required at `../alcops.dev/content/docs/analyzers/{copslug}/{ID}.md` (sibling repo; out of scope unless asked).
8. Commit `feat({ID}): <summary>` on `feat/{id}-<slug>`; never on `main`.

## Common Mistakes

| Mistake | Fix |
|---|---|
| Inventing severity / enabled-by-default / category because the issue did not say | Stop at the gate and ask; these are never derivable. |
| Deciding the version gate from memory or from the latest SDK source | Quote the `reference/` availability cells via `/nav-sdk-docs:sdk-lookup`; only the tables know what 12.0 has. |
| `MessageFormat` placeholder count ≠ arguments passed to `Diagnostic.Create` (#415) | Count `{n}` in the resx and match the `messageArgs`; add a fixture whose message is asserted. |
| Stray characters in resx text, e.g. an unbalanced backtick (#397) | Proofread the three resx entries; they render verbatim in the editor. |
| Deriving from `ALCopsDiagnosticAnalyzer` / `{Cop}Analyzer` | Extend plain `DiagnosticAnalyzer` (`analyzer-exception-harness.md`). |
| Collecting in one callback and reporting in another (two-phase accumulator) | Partial-analysis passes run per-declaration callbacks only for the edited file, or not at all; analyze and report inside the same callback (`sdk-analysis-scope.md`). |
| `SemanticModel.GetSymbolInfo()` inside an operation callback, or comparing syntax text to identify symbols | Use `IOperation.GetSymbolSafe()` and compare symbols, not `ToString()` / `ValueText` (`symbol-resolution.md`). |
| Raw `StringComparison.OrdinalIgnoreCase` for AL identifiers | Use the `SemanticFacts` name-comparison API. |
| Using a net8.0-only SDK member without a guard | `#if NETSTANDARD2_1` stub or `VersionProvider` gate (`netstandard21-compatibility.md`). |
| Handling only one temporary-table form (#379, #382, #384) | Cover `TableType = Temporary`, `Record X temporary` variables/parameters, and temporary page source tables. |
| Name-keyed variable maps that ignore AL scoping (#448) | Consult the full local scope (locals, parameters, named return) before object scope; classify by symbol type, not name. |
| `[TestCase("Foo")]` without a matching `Foo.al`, or `NoDiagnostic` fixtures without `[|...|]` markers | Names must match exactly; both fixture kinds need markers. |
| Skipping the rule doc or the docs-site reminder | Steps 6 and 7 are part of "done". |
| Gating on `invocation.Instance` non-null silently skips bare self calls (#348) | Resolve via `GetReceiverTableType`; fixtures for all four forms plus the tableextension variant (`record-receiver-forms.md`). |
