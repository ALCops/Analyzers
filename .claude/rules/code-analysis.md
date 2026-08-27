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
| IDE0055 is the first rule promoted to `error`, IDE0005 the second | Both are fully machine-fixable, so each count went to 0 in one tool-executed PR. Isolate the fix with `dotnet format style ALCops.sln --diagnostics <ID>` — the `whitespace` subcommand also rewrites CHARSET (UTF-8 BOM), which was stripped in a separate PR. Note IDE0055 also covers using-directive order (`dotnet_sort_system_directives_first`). |
| Usings needed only under a `#if` TFM guard live inside that same `#if` block | IDE0005 is evaluated per compilation, so a using consumed only by `#if NETSTANDARD2_1` (or `!NET10_0_OR_GREATER`) code is "unnecessary" in the other TFMs — and since IDE0005 is `error`, CI fails on those TFMs. The reverse also bites: local builds are `net10.0` only, so `dotnet format style --diagnostics IDE0005` removed usings that only the `netstandard2.1`/`net8.0` branches need (PR #477 broke `SymbolInterfaceExtensions`, `UsePartialRecordsOnRead`). Before promoting any per-TFM-sensitive rule, build every cop with `-p:ContinuousIntegrationBuild=true` locally. |
| Column-aligned lookup tables keep their alignment, wrapped in `#pragma warning disable/restore IDE0055` with a reason comment | Roslyn has no aligned-assignment option (`csharp_space_around_binary_operators` cannot express it), so the formatter collapses the columns. The alignment is what makes these tables readable; the pragma is the only way to keep both. |
| Severities live in `.editorconfig`, never in MSBuild or `#pragma` | One place to audit, and `dotnet format` honours it. `#pragma warning disable` inside an analyzer needs a justification comment. |
| Analyzer packages use `PrivateAssets="all"` | They must not become dependencies of the shipped `ALCops.Analyzers` NuGet. |
| `GenerateDocumentationFile=true` + `CS1591` silenced | IDE0005 (unused usings) only reports in a command-line build when XML doc generation is on. The doc files are not packed (`ALCops.Analyzers.csproj` packs explicit DLL paths only). |
| `csharp_style_namespace_declarations` is `silent` | The codebase is mixed (file-scoped and block-scoped). Enforce only after a dedicated unification PR. |
| `CA1510`, `CA1863`, `CA2263` off, tagged `[NETSTANDARD2_1-BLOCKED]` in `.editorconfig` | The suggested APIs (`ArgumentNullException.ThrowIfNull`, `CompositeFormat`, `Enum.GetNames<T>`/`GetValues<T>`, `MethodInfo.CreateDelegate<T>`) do not exist on netstandard2.1, which every analyzer must compile for; the CI netstandard2.1 pass confirms the rules never fire there. `#if` guards were rejected (dual paths at ~15 sites, no functional gain). The only exception, `Enum.Parse<T>(string)`, does exist on netstandard2.1 and was fixed in `EnumProvider`. **When netstandard2.1 support is dropped, grep `NETSTANDARD2_1-BLOCKED` and delete the block** — the rules are otherwise wanted. |
| Culture rules `CA1304/1305/1307/1309/1310` off | AL identifiers and keywords are compared ordinally by design throughout the cops. |
| `CA1062` off | Analyzer callbacks receive contexts from the host; null-guarding every public parameter is noise. |
| CI `dotnet format --verify-no-changes` runs *after* the cop builds and has `continue-on-error: true` | Under `ContinuousIntegrationBuild` the test projects reference the cop DLLs by `HintPath`; formatting before the build gave 262 `CS0246` and turned every test-project using into a bogus `IDE0005` error. The step also sets `Configuration=Release` via `env:` because the HintPath contains `$(Configuration)` and `dotnet format` (no `--configuration` switch) defaults to Debug. The step is otherwise the same baseline principle. The `whitespace` (IDE0055 + CHARSET) and `style` (IDE0005) categories are clean; the umbrella command still reports a few RCS fixes at `--severity warn`. Flip to blocking once those are 0 on `main`. |
| No `end_of_line` in `.editorconfig` | The repo relies on git `autocrlf`; the index is LF, Windows working trees are CRLF. An explicit EOL rule would make `dotnet format --verify-no-changes` fail locally on Windows. |
| `charset = utf-8` (no BOM) is enforced; all `.cs` files are BOM-less | EditorConfig `utf-8` means *without* BOM; 23 Visual Studio-saved files had one and made `dotnet format --verify-no-changes` red (`CHARSET`). Stripped with `dotnet format whitespace ALCops.sln`. |
| `src/**/Rules/**/*.al` marked `generated_code = true` and excluded from `dotnet format` | Fixtures are test inputs and must stay byte-stable (trailing whitespace, final newline included). |

## Baseline snapshot (2026-08-26, local net10.0 build)

Approximate warning counts at introduction, for the ratchet plan: IDE0055 formatting **0** (was ~340 in 17 files, mostly tab indentation in DocumentationCop; fixed and promoted to `error`), IDE0005 (unused usings) **0** (was 28 in 26 files, mostly test classes; fixed and promoted to `error`), CA1725 (parameter names vs base) ~44, CA1852 (seal internal types) ~36, CA1861 ~14, CA2263 **0** (was 13; 12 suppressed as netstandard2.1-blocked, 1 fixed), CA1859 ~10, CA1720/CA1311/CA1822 ~6 each, CA1711 ~5, CA1068/CA2249 ~4, RCS1075/RCS1102 <5, CS1570/CS1573 (malformed XML docs) ~5. Everything else <3.

Remaining `dotnet format --verify-no-changes` work: RCS1075/RCS1102 (3) — still make the CI format step red (`continue-on-error`). IDE0055, CHARSET (UTF-8 BOM, 23 files) and IDE0005 (unused usings) are fixed. The 5 IMPORTS findings went away with IDE0055, which also enforces using order.

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
