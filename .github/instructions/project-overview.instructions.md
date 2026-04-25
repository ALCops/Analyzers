---
applyTo: '**'
---

# ALCops Analyzers: Project Overview

ALCops Analyzers is a collection of six custom code analyzers for the AL programming language (Microsoft Dynamics 365 Business Central). It is a .NET solution built on the `Microsoft.Dynamics.Nav.CodeAnalysis` SDK.

## Solution structure

The solution (`ALCops.sln`) contains 13 projects in the `src/` directory. A 14th project (`ALCops.Analyzers`) exists in `src/` but is not in the `.sln` file; it is a CI-only NuGet meta-package.

### Shared library

- **`ALCops.Common`**: Shared library referenced by all analyzer projects. Contains:
  - `Settings/` : `ALCopsSettings.cs`, `ALCopsSettingsProvider.cs` (per-project config via `alcops.json`)
  - `Extensions/` : Syntax node, symbol, compilation, and type extension methods
  - `Helpers/` : `ManifestHelper.cs`, `AppSourceCopConfigurationProvider.cs`
  - `Reflection/` : `CompilationHelper.cs`, `EnumProvider.cs`, `PropertyAccessor.cs`, `SymbolHelper.cs`, `VersionProvider.cs`, `StringHelper.cs`
  - `Constants.cs` : Shared constant values

### Analyzer projects (6)

Each analyzer project follows the same structure:

| Project | Diagnostic prefix | Help URI slug |
|---|---|---|
| `ALCops.ApplicationCop` | `AC` | `applicationcop` |
| `ALCops.DocumentationCop` | `DC` | `documentationcop` |
| `ALCops.FormattingCop` | `FC` | `formattingcop` |
| `ALCops.LinterCop` | `LC` | `lintercop` |
| `ALCops.PlatformCop` | `PC` | `platformcop` |
| `ALCops.TestAutomationCop` | `TA` | `testautomationCop` |

Standard files in each analyzer project:
- `DiagnosticIds.cs` : Static readonly string fields mapping rule names to IDs (e.g. `"LC0003"`, `"AC0001"`)
- `DiagnosticDescriptors.cs` : `DiagnosticDescriptor` instances using the `Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics` API, with categories (Design, Naming, Style, Usage, Performance, Security) and help URIs
- `ALCops.{CopName}Analyzers.resx` : Resource file for diagnostic messages (title, messageFormat, description). Generates a strongly-typed class `{CopName}Analyzers` via MSBuild
- `ALCops.{CopName}Analyzers.cs` : Auto-generated strongly-typed resource accessor (excluded during CI builds via `<Compile Condition="'$(ContinuousIntegrationBuild)' == 'true'" Remove="..."/>`)
- `Analyzers/` : One `.cs` file per analyzer rule. Each class is decorated with `[DiagnosticAnalyzer]` and extends `DiagnosticAnalyzer`
- `CodeFixes/` (when present) : One `.cs` file per code fix. Each class is decorated with `[CodeFixProvider]` and extends `CodeFixProvider`

CodeFixes directories exist in: ApplicationCop, FormattingCop, LinterCop, PlatformCop.
DocumentationCop and TestAutomationCop have no CodeFixes.

### Test projects (6)

Each test project follows the pattern `ALCops.{CopName}.Test`:
- Uses NUnit 4.x with `ALCops.RoslynTestKit` (custom Roslyn test kit package, version 0.4.1)
- `AssemblyInfo.cs` : Sets `[assembly: Parallelizable(ParallelScope.All)]`
- `Rules/` : One folder per rule, containing the test class and subfolders `HasDiagnostic/` and `NoDiagnostic/` (plus `HasFix/` if a code fix exists) with `.al` test fixture files
- Test classes extend `NavCodeAnalysisBase` and use `RoslynFixtureFactory.Create<T>()`
- Tests target `net10.0` by default, but dynamically switch to `net8.0` when `NavTargetFramework=net8.0` is passed

### Meta-package

- **`ALCops.Analyzers`** (`src/ALCops.Analyzers/`): NuGet packaging project (not in the `.sln`). References all 6 cops + Common with `PrivateAssets="all"`. Always builds `netstandard2.1`, `net8.0`, and `net10.0`. Packs all analyzer DLLs into `lib/netstandard2.1/`, `lib/net8.0/`, and `lib/net10.0/` in the NuGet package. Sets `IncludeBuildOutput=false` and `DevelopmentDependency=true`.

## Dependency graph

```
ALCops.Common
  ├── ALCops.ApplicationCop
  ├── ALCops.DocumentationCop
  ├── ALCops.FormattingCop
  ├── ALCops.LinterCop
  ├── ALCops.PlatformCop
  └── ALCops.TestAutomationCop

ALCops.{CopName}.Test → ALCops.{CopName} + ALCops.Common
  (via ProjectReference locally, via binary Reference in CI)

ALCops.Analyzers → all 6 cops + Common (PrivateAssets="all")
```

All analyzer projects reference `ALCops.Common` via `<ProjectReference>`.
Test projects use `<ProjectReference>` locally but switch to binary `<Reference>` in CI (controlled by `ContinuousIntegrationBuild`).

External SDK dependencies (not NuGet, local DLLs):
- `Microsoft.Dynamics.Nav.CodeAnalysis.dll`
- `Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces.dll` (cops with CodeFixes)
- `System.Composition.AttributedModel.dll` (cops with CodeFixes)
- `Microsoft.Dynamics.Nav.Analyzers.Common.dll` (Common only)

These are resolved from `$(BcDevToolsDir)/$(TargetFramework)/`, defaulting to `../../Microsoft.Dynamics.BusinessCentral.Development.Tools`.

## Multi-target framework strategy

Cop and Common projects multi-target conditionally:
- **Local dev** (`ContinuousIntegrationBuild != true`): `net8.0` only (fast builds)
- **CI** (`GITHUB_ACTIONS == true` sets `ContinuousIntegrationBuild = true`): `netstandard2.1;net8.0;net10.0`

This triple-target exists because BC Dev Tools ship in multiple frameworks. Older BC versions use `netstandard2.1`, current versions use `net8.0`, and BC 29.0+ use `net10.0`.

Some analyzers are **net8.0-only** when they depend on SDK APIs that are entirely absent in netstandard2.1. These use a `#if NETSTANDARD2_1` guard around the entire class body, compiling as an empty stub (no diagnostics, no-op initialize) on the older target. See `netstandard21-compatibility.instructions.md` for the pattern. Test-side, use `RequireMinimumVersion("16.0")` to skip tests when the netstandard2.1 SDK is loaded at runtime.

The `netstandard2.1` target requires:
- `System.Collections.Immutable` NuGet package (version 5.0.0)
- `Newtonsoft.Json` from BC Dev Tools (Common only, since `System.Text.Json` is unavailable)
- Conditional `#if NETSTANDARD2_1` / `#if !NETSTANDARD2_1` blocks in source code

Test projects target `net10.0` by default. When `NavTargetFramework=net8.0` is passed (from the CI test matrix), they dynamically switch to `net8.0`. For legacy TFMs (`netstandard2.0`/`netstandard2.1`), the test project still compiles as `net10.0` since test projects cannot target netstandard. They use a `NavTargetFramework` property (defaulting to `net10.0`) to select both the `TargetFramework` and the binary TFM to reference when in CI mode. `NavBinaryTfm` resolves to `$(NavTargetFramework)` for modern TFMs and `netstandard2.1` for legacy TFMs.

The `ALCops.Analyzers` meta-package always builds all three TFMs (`netstandard2.1;net8.0;net10.0`).

## Naming conventions

- **Projects**: `ALCops.{CopName}` and `ALCops.{CopName}.Test`
- **Namespaces**: Match project names (e.g. `ALCops.LinterCop`, `ALCops.Common.Settings`)
- **Diagnostic IDs**: 2-letter prefix + 4-digit number: `AC0001`, `DC0001`, `FC0001`, `LC0003`, `PC0001`, `TA0001`
- **Analyzer classes**: Named after the rule (e.g. `ObjectIdInDeclaration`), placed in `Analyzers/` folder
- **CodeFix classes**: Named `{RuleName}CodeFixProvider` (e.g. `ObjectIdInDeclarationCodeFixProvider`), placed in `CodeFixes/`
- **DiagnosticDescriptor fields**: Named after the rule in PascalCase, matching the `DiagnosticIds` field name
- **Resx keys**: `{RuleName}Title`, `{RuleName}MessageFormat`, `{RuleName}Description`
- **Test classes**: Named after the rule, placed in `Rules/{RuleName}/`
- **Test fixture files**: `.al` files in `HasDiagnostic/`, `NoDiagnostic/`, `HasFix/` subfolders

## Help URIs and documentation site

Each diagnostic has a help URI pointing to `https://alcops.dev/docs/analyzers/{copslug}/{id}/` where:
- `{copslug}` is the lowercase cop name (e.g. `lintercop`, `applicationcop`, `platformcop`)
- `{id}` is the lowercase diagnostic ID (e.g. `lc0003`, `ac0001`)

The documentation site source is a sibling repo (`../alcops.dev` relative to the Analyzers repo root, or `https://github.com/ALCops/alcops.dev`). It is a Hugo-based site. Every new diagnostic rule must have a corresponding documentation page at `content/docs/analyzers/{copslug}/{ID}.md`.

## Settings system

`ALCopsSettingsProvider` in `ALCops.Common/Settings/` loads configuration from an `alcops.json` file:
1. First looks in the AL workspace directory
2. Falls back to the directory containing `ALCops.Common.dll`

Settings are cached per workspace path via `ConcurrentDictionary`. Current configurable thresholds in `ALCopsSettings`:
- `CognitiveComplexityThreshold` (default: 15)
- `CyclomaticComplexityThreshold` (default: 8)
- `MaintainabilityIndexThreshold` (default: 20)

The settings provider uses `Newtonsoft.Json` on `netstandard2.1` and `System.Text.Json` on `net8.0`.

## Versioning

Three-channel release strategy (Alpha/Beta/Stable) using GitVersion. See `release-strategy.instructions.md` for full details.

## Branching policy

**Never commit directly to `main`.** The `main` branch is protected. All changes must go through a pull request:

1. Create a feature branch from `main` (e.g., `fix/lc0086-case-sensitive-matching`, `feat/pc0030-new-rule`)
2. Commit your changes to the feature branch
3. Push the branch and create a pull request via `gh pr create`
4. The PR triggers CI (build, test) via `pull-request.yml`
5. After review and CI pass, the PR is merged into `main`

Branch naming conventions:
- `fix/<description>` for bug fixes
- `feat/<description>` for new features
- `docs/<description>` for documentation-only changes
- `release/vX.Y.Z` for release stabilization branches

## CI/CD

GitHub Actions workflows in `.github/workflows/`:
- `build-test.yml` : Reusable workflow for build and test
- `pull-request.yml` : Runs on PRs
- `build-and-release.yml` : Build, test, pack, and publish (alpha/beta/stable channels)
- `scheduled-build.yml` : Daily cache keepalive builds
- `scheduled-cleanup.yml` : Weekly cleanup of old pre-release packages from NuGet.org and GitHub Packages

## Building locally

```bash
dotnet build ALCops.sln
dotnet test ALCops.sln
```

Requires BC Dev Tools at `../../Microsoft.Dynamics.BusinessCentral.Development.Tools` (relative to repo root) or override with `/p:BcDevToolsDir=<path>`.

## C# language settings

- `LangVersion`: `Latest`
- `Nullable`: `enable`
- `ImplicitUsings`: `enable`
- `WarningsAsErrors`: `CS8600;CS8602;CS8603;CS8604;CS8605` (nullable reference type warnings)

## Adding a new diagnostic rule

See `analyzer-development.instructions.md` for the step-by-step guide.
