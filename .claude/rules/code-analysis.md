---
paths:
  - "Directory.Build.props"
  - "Directory.Packages.props"
  - ".editorconfig"
  - "src/**/*.csproj"
---

# Code analysis on the C# codebase

The repo ships analyzers, so it dogfoods static analysis on itself: .NET analyzers (`AnalysisLevel=latest-recommended`), `Microsoft.CodeAnalysis.Analyzers` (RS rules for analyzer authors), Roslynator (RCS rules) and `.editorconfig` code style enforced in build.

## Where things live

- `Directory.Build.props` — every property shared by all projects (`Nullable`, `LangVersion`, `WarningsAsErrors`, analysis switches) and the analyzer `PackageReference`s. A csproj should only contain what is genuinely project-specific (TFMs, NAV SDK `Reference`s, resx wiring, pack layout).
- `Directory.Packages.props` — Central Package Management. Never put `Version=` on a `PackageReference`; add/bump versions here.
- `.editorconfig` — all rule severities and suppressions, with a comment per suppression explaining why.

## Design decisions

| Decision | Why |
|---|---|
| Warnings-only baseline; only `CS8600;CS8602;CS8603;CS8604;CS8605` are errors | The analyzers were introduced onto an existing codebase. The ratchet plan is: rules with zero occurrences → `error`; the rest fixed and promoted category by category in dedicated PRs. |
| IDE0055 is the first rule promoted to `error` | It is fully machine-fixable, so the count went to 0 in one tool-executed PR. Isolate the fix with `dotnet format style ALCops.sln --diagnostics IDE0055` — the `whitespace` subcommand also rewrites CHARSET (UTF-8 BOM), which was stripped in a separate PR. Note IDE0055 also covers using-directive order (`dotnet_sort_system_directives_first`). |
| Column-aligned lookup tables keep their alignment, wrapped in `#pragma warning disable/restore IDE0055` with a reason comment | Roslyn has no aligned-assignment option (`csharp_space_around_binary_operators` cannot express it), so the formatter collapses the columns. The alignment is what makes these tables readable; the pragma is the only way to keep both. |
| Severities live in `.editorconfig`, never in MSBuild or `#pragma` | One place to audit, and `dotnet format` honours it. `#pragma warning disable` inside an analyzer needs a justification comment. |
| Analyzer packages use `PrivateAssets="all"` | They must not become dependencies of the shipped `ALCops.Analyzers` NuGet. |
| `GenerateDocumentationFile=true` + `CS1591` silenced | IDE0005 (unused usings) only reports in a command-line build when XML doc generation is on. The doc files are not packed (`ALCops.Analyzers.csproj` packs explicit DLL paths only). |
| `csharp_style_namespace_declarations` is `silent` | The codebase is mixed (file-scoped and block-scoped). Enforce only after a dedicated unification PR. |
| Culture rules `CA1304/1305/1307/1309/1310` off | AL identifiers and keywords are compared ordinally by design throughout the cops. |
| `CA1062` off | Analyzer callbacks receive contexts from the host; null-guarding every public parameter is noise. |
| CI `dotnet format --verify-no-changes` has `continue-on-error: true` | Same baseline principle. The `whitespace` category (IDE0055 + CHARSET) is clean; the umbrella command still reports IDE0005 (unused usings) and a few RCS fixes at `--severity warn`. Flip to blocking once those are 0 on `main`. |
| No `end_of_line` in `.editorconfig` | The repo relies on git `autocrlf`; the index is LF, Windows working trees are CRLF. An explicit EOL rule would make `dotnet format --verify-no-changes` fail locally on Windows. |
| `charset = utf-8` (no BOM) is enforced; all `.cs` files are BOM-less | EditorConfig `utf-8` means *without* BOM; 23 Visual Studio-saved files had one and made `dotnet format --verify-no-changes` red (`CHARSET`). Stripped with `dotnet format whitespace ALCops.sln`. |
| `src/**/Rules/**/*.al` marked `generated_code = true` and excluded from `dotnet format` | Fixtures are test inputs and must stay byte-stable (trailing whitespace, final newline included). |

## Baseline snapshot (2026-08-26, local net10.0 build)

Approximate warning counts at introduction, for the ratchet plan: IDE0055 formatting **0** (was ~340 in 17 files, mostly tab indentation in DocumentationCop; fixed and promoted to `error`), CA1725 (parameter names vs base) ~44, CA1852 (seal internal types) ~36, IDE0005 (unused usings) ~26, CA1861 ~14, CA2263 ~13, CA1859 ~10, CA1720/CA1311/CA1822 ~6 each, CA1711 ~5, CA1068/CA2249 ~4, RCS1075/RCS1102 <5, CS1570/CS1573 (malformed XML docs) ~5. Everything else <3.

Remaining `dotnet format --verify-no-changes` work: IDE0005 (~56 unused usings) and RCS1075/RCS1102 (3) — still make the CI format step red (`continue-on-error`). CHARSET (UTF-8 BOM, 23 files) is fixed. The 5 IMPORTS findings went away with IDE0055, which also enforces using order.

## Known issues / limitations

| Issue | Status |
|---|---|
| RS rules from `Microsoft.CodeAnalysis.Analyzers` mostly key on the real `Microsoft.CodeAnalysis` types; the NAV SDK is a fork (`Microsoft.Dynamics.Nav.CodeAnalysis`), so many RS rules never fire on the cops. | Accepted. Record any RS rule that *does* fire against a NAV type in the Design decisions table before suppressing it. |
| `MSB3277` assembly-version conflicts on test projects (NAV SDK vs `Microsoft.Extensions.*` transitive refs). | Pre-existing, unrelated to analyzers. |
| `NU1903`: transitive `System.Data.SqlClient 4.8.5` (via NAV SDK / test packages) has a known vulnerability. | Pre-existing. Candidate for `CentralPackageTransitivePinningEnabled` or an explicit `PackageVersion` override. |

## Adding a suppression

1. Confirm the rule is wrong for this codebase, not just for one file. Single-site exceptions use `#pragma` with a reason.
2. Add `dotnet_diagnostic.XXXX.severity = none|suggestion` under the right section of `.editorconfig` with a one-line reason.
3. If the reason involves a NAV SDK type, add a row to the table above.
