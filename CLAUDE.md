# ALCops Analyzers

Six custom code analyzers for AL (Microsoft Dynamics 365 Business Central), built on the `Microsoft.Dynamics.Nav.CodeAnalysis` SDK (the "NAV SDK"). Each cop is a .NET project under `src/` with a sibling `*.Test` project; `ALCops.Common` is the shared library (it also hosts the cross-cutting `CM` diagnostics); `ALCops.Analyzers` is a CI-only NuGet meta-package (not in the `.sln`).

| Project | Prefix | Help URI slug | CodeFixes |
|---|---|---|---|
| `ALCops.ApplicationCop` | `AC` | `applicationcop` | yes |
| `ALCops.DocumentationCop` | `DC` | `documentationcop` | no |
| `ALCops.FormattingCop` | `FC` | `formattingcop` | yes |
| `ALCops.LinterCop` | `LC` | `lintercop` | yes |
| `ALCops.PlatformCop` | `PC` | `platformcop` | yes |
| `ALCops.TestAutomationCop` | `TA` | `testautomationCop` (sic, matches descriptors) | no |
| `ALCops.Common` | `CM` | `common` | no |

Per cop: `DiagnosticIds.cs`, `DiagnosticDescriptors.cs`, `ALCops.{Cop}Analyzers.resx` (messages; generates a strongly-typed class at build), `Analyzers/{RuleName}.cs`, `CodeFixes/{RuleName}CodeFixProvider.cs`. Tests: `src/ALCops.{Cop}.Test/Rules/{RuleName}/{RuleName}.cs` + `HasDiagnostic/`, `NoDiagnostic/`, `HasFix/` `.al` fixtures.

## Build and test

```bash
dotnet build ALCops.sln
dotnet test ALCops.sln
dotnet test src/ALCops.LinterCop.Test/ --filter "FullyQualifiedName~{RuleName}"
dotnet test src/ALCops.LinterCop.Test/ --filter "FullyQualifiedName~{RuleName}.HasDiagnostic"
```

- Shared MSBuild settings live in `Directory.Build.props`; package versions in `Directory.Packages.props` (Central Package Management - no `Version=` on `PackageReference`).
- Static analysis (NetAnalyzers, `Microsoft.CodeAnalysis.Analyzers`, Roslynator, `.editorconfig` code style) is enforced: every analyzer warning is a build error, and `dotnet format ALCops.sln --verify-no-changes` is a blocking CI gate. Suppressions live in `.editorconfig` with a reason. See `.claude/rules/code-analysis.md`.

- Requires BC Dev Tools at `../../Microsoft.Dynamics.BusinessCentral.Development.Tools` (repo-root relative) or `/p:BcDevToolsDir=<path>`. `.vscode/Setup-BCDevTools.ps1` downloads them.
- Local builds target `net10.0` only. CI (`ContinuousIntegrationBuild=true`) builds `netstandard2.1;net8.0;net10.0` because BC ships the SDK in all three. Test projects target `net10.0` and switch via `NavTargetFramework`.
- Before merging, build every cop for all three TFMs locally: `dotnet build src/ALCops.{Cop}/ALCops.{Cop}.csproj -c Release -p:ContinuousIntegrationBuild=true --no-incremental` — analyzers evaluate one compilation at a time, so `#if`-guarded code only surfaces there.
- Nullable warnings `CS8600;CS8602;CS8603;CS8604;CS8605` are errors.

## Hard constraints

- **Read the decompiled NAV SDK source before using any SDK API.** Syntax kinds, operation shapes, and symbol members are undocumented and version-dependent. See `.claude/rules/analyzer-development.md` (§NAV SDK source) and `.claude/rules/symbol-resolution.md`.
- **Every analyzer must compile on `netstandard2.1`.** Guard newer C# features and missing SDK APIs; net8.0-only analyzers compile as empty stubs under `#if NETSTANDARD2_1`. See `.claude/rules/netstandard21-compatibility.md`.
- **Never assume analyzer callback ordering or that every callback runs** (the host's partial-analysis module pass skips all per-declaration callbacks). No two-phase accumulator patterns. See `.claude/rules/sdk-analysis-scope.md`.
- **Analyzers extend plain `DiagnosticAnalyzer`.** Do not switch them to the `ALCopsDiagnosticAnalyzer` / `{Cop}Analyzer` exception harness: deriving from a Common-based type makes `alc` fail with `AL1003` (issue #389). The harness stays test-only until a loader-safe approach exists. See `.claude/rules/analyzer-exception-harness.md`.
- **`ALCopsSettings.cs` and `alcops.schema.json` must stay in sync**; a parity test enforces it. See `.claude/rules/settings-schema.md`.
- Diagnostic IDs are `{Prefix}{4 digits}`, sequential per cop. Help URI: `https://alcops.dev/docs/analyzers/{copslug}/{id}/`. Every new rule needs a page in the sibling docs repo (`../alcops.dev`, `content/docs/analyzers/{copslug}/{ID}.md`).
- Resx keys: `{RuleName}Title`, `{RuleName}MessageFormat`, `{RuleName}Description`. Descriptor field, `DiagnosticIds` field, analyzer class, and test folder all share the rule name.

## Workflow

- `main` is protected — never commit to it. Branch from `main`: `feat/<desc>`, `fix/<desc>`, `docs/<desc>`, `chore/<desc>`; `release/vX.Y.Z` for release stabilization. Open PRs with `gh pr create`; CI runs build + tests.
- Commit messages: conventional commits scoped by rule ID — `feat(LC0095): …`, `fix(PC0021): …`, `test(FC0002): …`, `docs: …`, `chore: …`.
- Code comments and XML docs must be self-contained: explain the mechanism, never cite issue or PR numbers. Deep context and issue links belong in the rule's `.claude/rules/diagnostics/{id}-{slug}.md`. Docs and config describe current state only — no PR numbers anywhere.
- Bug fixes start with a failing regression fixture (`NoDiagnostic/` for false positives, `HasDiagnostic/` for false negatives) before touching the analyzer.
- Releases use GitVersion with alpha/beta/stable channels. See `.claude/rules/release-strategy.md`; use `/release`.

## Keeping `.claude/` in sync

- New rule → create `.claude/rules/diagnostics/{id}-{slug}.md` from `.claude/skills/new-analyzer/references/rule-doc.md`. New CodeFix → add a `## CodeFix` section to that file. Changed or added design decision, non-obvious workaround, or accepted limitation → update its Design decisions / Known issues table.
- New shared component or convention → new `.claude/rules/<area>.md` with a `paths:` frontmatter scoped as narrowly as possible. Never add a rules file without `paths:` (it would load in every session).
- Rules files document *why*, not *what*: no diagnostic-property tables, test-case lists, or file inventories — the code is the source of truth for those.
- Knowledge needed whenever you edit matching files lives in `.claude/rules/`; procedural templates and checklists used only while running a skill live in `.claude/skills/*/references/`. Never keep the same content in both — leave a pointer.
- If none of this applies to a change, say "No `.claude` doc changes needed" in the plan.

## Where to look

- `.claude/rules/*.md` — path-scoped guides, auto-loaded when you touch matching files: analyzer development (core rules), SDK analysis scope (how callbacks run), symbol resolution, record receiver forms, analyzer performance, CodeFixes, testing, Common library, exception harness, record-method classification, settings schema, netstandard2.1, code analysis, release strategy, BC DevTools action.
- `.claude/rules/diagnostics/{id}-{slug}.md` — one file per rule: purpose, design decisions, architecture, known issues, CodeFix decisions.
- Skills: `/new-analyzer <ID> <ClassName> <Cop>`, `/new-codefix <ID>`, `/fix-false-positive <issue-or-description>`, `/release`.
