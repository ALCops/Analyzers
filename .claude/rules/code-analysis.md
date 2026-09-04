---
paths:
  - "Directory.Build.props"
  - "Directory.Packages.props"
  - ".editorconfig"
  - "src/**/*.csproj"
---

# Code analysis on the C# codebase

The repo ships analyzers, so it dogfoods static analysis on itself: .NET analyzers (`AnalysisLevel=latest-recommended`), `Microsoft.CodeAnalysis.Analyzers` (RS rules for analyzer authors), Roslynator (RCS rules) and `.editorconfig` code style enforced in build. Every analyzer warning is a build error; the only exceptions are the suppressions in `.editorconfig`, each with its reason.

## Where things live

- `Directory.Build.props` — every property shared by all projects (`Nullable`, `LangVersion`, `WarningsAsErrors`, analysis switches) and the analyzer `PackageReference`s. A csproj should only contain what is genuinely project-specific (TFMs, NAV SDK `Reference`s, resx wiring, pack layout).
- `Directory.Packages.props` — Central Package Management. Never put `Version=` on a `PackageReference`; add/bump versions here.
- `.editorconfig` — code style and all suppressions, with a comment per suppression explaining why.
- `src/ALCops.Common.Test/Conventions/` — convention tests source-linked into every `*.Test` project (things analyzers cannot express).

## Design decisions

| Decision | Why |
|---|---|
| `CodeAnalysisTreatWarningsAsErrors=true` for analyzers; compiler warnings promoted individually via `WarningsAsErrors` (`CS8600-CS8605` nullable flow, `CS1570/CS1572/CS1573/CS1574` XML docs) | One switch instead of a severity line per rule. `CodeAnalysisTreatWarningsAsErrors` covers analyzer diagnostics only, so compiler warnings need the explicit list. Suppressions (`none`/`suggestion`) in `.editorconfig` are the only exceptions and each states its reason. |
| Code that exists only under a `#if` TFM guard keeps its usings, doc comments and pragmas inside that same `#if`; verify with the multi-TFM build before merging | Analyzers and the compiler evaluate one compilation at a time. Local builds are `net10.0` only, so anything specific to `netstandard2.1`/`net8.0` (a using consumed only there, an `[Obsolete]` API in the older SDK, a `<param>` tag on a record whose netstandard2.1 fallback is a plain class) is invisible locally and fails CI. `dotnet build src/ALCops.{Cop}/ALCops.{Cop}.csproj -c Release -p:ContinuousIntegrationBuild=true --no-incremental` per cop, counting *all* warning lines. Fixes can cascade (making a method static or narrowing a return type exposes the next caller), so rebuild until nothing new appears. |
| Single-site exceptions use `#pragma warning disable/restore <ID>` with a reason comment; rule-wide exceptions go to `.editorconfig` | Keeps the audit surface small: a pragma marks exactly one deliberate deviation. Current pragmas: `IDE0055` around column-aligned lookup tables (Roslyn has no aligned-assignment option and would collapse them), `RCS1075` around `PropertyAccessor`'s two reflection catch blocks (version-tolerant NAV SDK access; Roslynator's fixer would remove the catch), `CS0618` around `GetApplicationObjectTypeSymbolsByNameAcrossModules` in the netstandard2.1 branch of `TableRelationFieldLength` (obsolete only in that SDK, no replacement there). |
| Every analyzer, CodeFixProvider and nested CodeAction is `sealed`; enforced by CA1852 plus the `AnalyzerTypesAreSealed` convention test | The host instantiates these via attributes and never subclasses them, so they are leaves; `sealed` lets the JIT devirtualise and blocks accidental inheritance. CA1852 ignores `public` types and skips assemblies with `InternalsVisibleTo` (LinterCop), hence the reflection test in every `*.Test` project. The abstract `ALCopsDiagnosticAnalyzer`/`{Cop}Analyzer` harness bases are the intended exception. |
| `dotnet format analyzers --diagnostics CAxxxx` requires an explicit `dotnet_diagnostic.CAxxxx.severity` line in the matching `.editorconfig` section | `dotnet format` ignores the SDK's `analysislevel_*_recommended.globalconfig` and uses each rule's default severity (`hidden` for most CA rules), so without the line it reports 0 sites while the build reports them. Add the line temporarily (in `[*.cs]`, not appended at file end where it lands in the test section), run the fixer, remove it. Roslynator rules default to `warning` and never need this. Referencing `Microsoft.CodeAnalysis.NetAnalyzers` as a package is *not* the fix and duplicates analyzer instances. CA1068, CA1859 and CA2012 have no fixer at all. |
| `CA1510`, `CA1863`, `CA2263` off, tagged `[NETSTANDARD2_1-BLOCKED]` | The suggested APIs (`ArgumentNullException.ThrowIfNull` .NET 6+, `CompositeFormat` .NET 8+, `Enum.GetNames<T>`/`GetValues<T>` and `MethodInfo.CreateDelegate<T>` .NET 5+) do not exist on netstandard2.1, which every analyzer must compile for; `#if` guards would mean dual code paths for no functional gain. `Enum.Parse<T>(string)` does exist there and is used. **When netstandard2.1 support is dropped, grep `NETSTANDARD2_1-BLOCKED` and delete the block** — the rules are otherwise wanted. |
| `CA1711` (reserved suffixes) and `CA1720` (type names in identifiers) off | Both encode BCL-authoring naming conventions. `Permission`/`Enum`/`Attribute` are AL domain terms here (`RequiredPermission` is an AL permission requirement; `OptionTypeShouldBeEnum`, `GlobalMethodRequiresTestAttribute` are rule names that CLAUDE.md pins to descriptor, resx, test folder and docs). `EnumProvider.Char/Decimal/Guid/Integer/String` and `NamingPattern.Object` deliberately mirror the NAV SDK's `NavTypeKind` member names. Not TFM-related, so not in the `[NETSTANDARD2_1-BLOCKED]` block. |
| Culture rules `CA1304/1305/1307/1309/1310` off; help-URI slugs use `ToLowerInvariant()` | AL identifiers and keywords are compared ordinally by design throughout the cops; the single deliberate exception is `NaturalStringComparer` (FC0004), which uses `InvariantCultureIgnoreCase` to match AZ AL Dev Tools. The one place casing is *produced* (`DiagnosticDescriptors.GetHelpUri`) must be culture-independent so a help link is identical on every machine (Turkish-İ). |
| `CA1062` off | Analyzer callbacks receive contexts from the host; null-guarding every public parameter is noise. |
| Test code (`[src/*.Test/**.cs]`): `CA1707`, `CA1822`, `CA1861`, `CS8618` off | Test methods are discovered by attribute (naming/static rules do not apply); `Is.EquivalentTo(new[] { … })` literals belong next to their assert; fixture fields are assigned in `[SetUp]` — the build already suppresses `CS8618` via NUnit.Analyzers' suppressor, which `dotnet format` does not run. |
| Analyzer packages use `PrivateAssets="all"` | They must not become dependencies of the shipped `ALCops.Analyzers` NuGet. |
| `GenerateDocumentationFile=true` + `CS1591` silenced | IDE0005 (unused usings) only reports in a command-line build when XML doc generation is on. The doc files are not packed (`ALCops.Analyzers.csproj` packs explicit DLL paths only). |
| `csharp_style_namespace_declarations` is `silent` | The codebase is mixed (file-scoped and block-scoped). Enforce only after a dedicated unification PR. |
| CI `dotnet format --verify-no-changes` runs *after* the cop builds, with `Configuration=Release`, and is blocking | Under `ContinuousIntegrationBuild` the test projects reference the cop DLLs by `HintPath` (`bin/$(Configuration)/<tfm>/`), so the workspace only compiles once those exist; `dotnet format` has no `--configuration` switch and defaults to Debug. Step-level `env:` overrides of `ContinuousIntegrationBuild` or `GITHUB_ACTIONS` do not work (project-level assignment wins / the runner protects `GITHUB_*`). |
| No `end_of_line` in `.editorconfig` | The repo relies on git `autocrlf`; the index is LF, Windows working trees are CRLF. An explicit EOL rule would make `dotnet format --verify-no-changes` fail locally on Windows. |
| `charset = utf-8` (no BOM); all `.cs` files are BOM-less | EditorConfig `utf-8` means *without* BOM; a BOM makes `dotnet format --verify-no-changes` red (`CHARSET`). |
| `src/**/Rules/**/*.al` marked `generated_code = true` and excluded from `dotnet format` | Fixtures are test inputs and must stay byte-stable (trailing whitespace, final newline included). |

## Verifying a change

1. Every cop for all TFMs: `dotnet build src/ALCops.{Cop}/ALCops.{Cop}.csproj -c Release -p:ContinuousIntegrationBuild=true --no-incremental` — must show no `warning` or `error` line of any id.
2. `dotnet format ALCops.sln --verify-no-changes --no-restore --severity warn --exclude "**/Rules/**/*.al"` — exit 0 (this is the CI gate). Read the findings, not the exit code: exit 2 with no diagnostics listed means whitespace findings (typically mixed CRLF/LF after a scripted edit; fix with `dotnet format whitespace --include <file>`), and on a clean tree the MSB3277 workspace warnings alone can produce exit 2.
3. `dotnet test ALCops.sln` — includes the convention tests.

## Known issues / limitations

| Issue | Status |
|---|---|
| RS rules from `Microsoft.CodeAnalysis.Analyzers` mostly key on the real `Microsoft.CodeAnalysis` types; the NAV SDK is a fork (`Microsoft.Dynamics.Nav.CodeAnalysis`), so many RS rules never fire on the cops. | Accepted. Record any RS rule that *does* fire against a NAV type in the Design decisions table before suppressing it. |
| `MSB3277` assembly-version conflicts on test projects (NAV SDK vs `Microsoft.Extensions.*` transitive refs). | Pre-existing, unrelated to analyzers. |

## Adding a suppression

1. Confirm the rule is wrong for this codebase, not just for one file. Single-site exceptions use `#pragma` with a reason.
2. Add `dotnet_diagnostic.XXXX.severity = none|suggestion` under the right section of `.editorconfig` with a one-line reason.
3. If the reason involves a NAV SDK type, add a row to the table above.
