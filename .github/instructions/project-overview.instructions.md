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
- Tests always target `net8.0` only (not multi-target)

### Meta-package

- **`ALCops.Analyzers`** (`src/ALCops.Analyzers/`): NuGet packaging project (not in the `.sln`). References all 6 cops + Common with `PrivateAssets="all"`. Always builds both `netstandard2.1` and `net8.0`. Packs all analyzer DLLs into `lib/netstandard2.1/` and `lib/net8.0/` in the NuGet package. Sets `IncludeBuildOutput=false` and `DevelopmentDependency=true`.

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
- **CI** (`GITHUB_ACTIONS == true` sets `ContinuousIntegrationBuild = true`): `netstandard2.1;net8.0`

This dual-target exists because BC Dev Tools ship in both frameworks. Older BC versions use `netstandard2.1`, newer ones use `net8.0`.

Some analyzers are **net8.0-only** when they depend on SDK APIs that are entirely absent in netstandard2.1. These use a `#if NETSTANDARD2_1` guard around the entire class body, compiling as an empty stub (no diagnostics, no-op initialize) on the older target. See `netstandard21-compatibility.instructions.md` for the pattern. Test-side, use `RequireMinimumVersion("16.0")` to skip tests when the netstandard2.1 SDK is loaded at runtime.

The `netstandard2.1` target requires:
- `System.Collections.Immutable` NuGet package (version 5.0.0)
- `Newtonsoft.Json` from BC Dev Tools (Common only, since `System.Text.Json` is unavailable)
- Conditional `#if NETSTANDARD2_1` / `#if !NETSTANDARD2_1` blocks in source code

Test projects always target `net8.0` only. They use a `NavTargetFramework` property (defaulting to `net8.0`) to select which binary TFM to reference when in CI mode.

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

The documentation site source lives at `/Users/arthur/repo/ALCops/alcops.dev/` (Hugo-based, separate from this repo).

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

GitVersion with `GitHubFlow/v1` workflow:
- `main` branch: `alpha` label, `Patch` increment, tracks release branches
- `release` branch: prevents increment when current commit is tagged (uses tag value)

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

## CI/CD

GitHub Actions workflows in `.github/workflows/`:
- `build-test.yml` : Reusable workflow for build and test
- `pull-request.yml` : Runs on PRs
- `build-and-release.yml` : Build and release pipeline
- `scheduled-build.yml` : Scheduled builds

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

1. Add the diagnostic ID to `DiagnosticIds.cs` (use the correct prefix)
2. Add message strings to the `.resx` file (Title, MessageFormat, Description)
3. Add the `DiagnosticDescriptor` to `DiagnosticDescriptors.cs` with the correct category, severity, and help URI
4. Create the analyzer class in `Analyzers/` decorated with `[DiagnosticAnalyzer]`
5. Optionally create a code fix in `CodeFixes/` decorated with `[CodeFixProvider]`
6. Create test class in the corresponding test project under `Rules/{RuleName}/`
7. Add `.al` test fixtures in `HasDiagnostic/` and `NoDiagnostic/` (and `HasFix/` if applicable)
8. Add documentation page at the docs site (`alcops.dev`)
