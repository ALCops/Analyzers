---
applyTo: '.github/actions/get-bc-devtools/**'
---

# Get BC DevTools Action

## Purpose

The `get-bc-devtools` composite GitHub Action discovers all available BC DevTools sources (Marketplace VSIX, NuGet, BCArtifact), analyzes their assembly metadata (target framework and assembly version), and outputs a unified source list used by the build and test pipeline.

## Architecture

### Script pipeline

1. **`Get-Sources.ps1`** — Merges sources from three providers into a unified list, enriched with cached TFM data:
   - `Marketplace.ps1` — Queries VS Marketplace for ALLanguage VSIX versions
   - `NuGet-Packages.ps1` — Queries NuGet.org for `Microsoft.Dynamics.Nav.CodeAnalysis` packages
   - `BC-Artifacts.ps1` — Queries BC artifact feed for BCArtifact versions
2. **`Get-BC-DevTools.ps1`** — Main orchestrator. Reads `TargetFramework.json` cache, identifies missing versions, downloads and analyzes assemblies, updates the cache, then outputs enriched sources via `Get-Sources.ps1`.
3. **`action.yml`** — Composite action entry point. Calls `Get-BC-DevTools.ps1`, deduplicates sources by version, determines lowest version per TFM, and sets outputs.
4. **`Display-Sources.ps1`** — Renders a summary table to the workflow log.

### Assembly analysis (`Get-AssemblyInfo`)

For each new BC DevTools version, the script downloads `Microsoft.Dynamics.Nav.Analyzers.Common.dll` and inspects it using .NET reflection:

1. Load the assembly bytes via `Assembly.Load($bytes)` (no file lock)
2. Read `AssemblyVersion` from `GetName().Version`
3. Attempt to read `TargetFrameworkAttribute` via `GetCustomAttributesData()`
4. If custom attributes fail (e.g. cross-runtime inspection), fall through to reference-based detection
5. Reference-based detection: infer TFM from `System.Runtime` version (e.g. 8.x → net8.0, 10.x → net10.0) or `netstandard` reference

### Caching strategy

- `TargetFramework.json` is cached in GitHub Actions cache (`actions/cache`) with content-based keys
- Cache key pattern: `tfm-json-{sha256hash}` with `restore-keys: tfm-json-`
- The scheduled daily build keeps the cache alive (evicted after 7 days without access)
- Stale entries (packages no longer in any source) are pruned automatically

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| .NET SDK in Setup job | Install latest LTS (currently 10.0.x) via `actions/setup-dotnet` | Required for `GetCustomAttributesData()` to resolve `System.Runtime` for current BC DevTools assemblies. Must be updated when a new .NET major version appears in BC DevTools (e.g. .NET 11 expected ~November 2026). |
| Assembly inspection method | `System.Reflection.Assembly.Load($bytes)` with fallback | Direct metadata reading. `ReflectionOnlyLoad` tried first but unavailable on .NET Core |
| Cross-runtime TFM detection | `GetReferencedAssemblies()` fallback | Defense-in-depth: when `GetCustomAttributesData()` fails (e.g. new .NET version not yet installed), referenced assembly versions provide reliable TFM inference |
| TFM derivation from `System.Runtime` | Dynamic `net{major}.0` from version | Future-proof: automatically handles net8.0, net9.0, net10.0, etc. without code changes |
| Version sorting resilience | `[version]::TryParse()` with fallback to `0.0.0.0` | Prevents crashes from non-parseable version strings (e.g. `"analysis-error"` from failed analysis) |
| Cache key strategy | Content-based (SHA256 hash) | Only creates new cache entries when data actually changes |

## Maintenance

When a new .NET major version appears in BC DevTools assemblies:
1. Update `dotnet-version` in the Setup job of `build-test.yml` (e.g. add the new version or replace older ones)
2. Add a `tfm-net{major*10}0-version-lowest` output to `action.yml` (e.g. `tfm-net100-version-lowest` for net10.0)
3. Propagate the new output through `build-test.yml` workflow_call outputs and setup job outputs
4. Add conditional "Setup BC DevTools" steps for the new TFM in Build (`build-test.yml`) and Release (`build-and-release.yml`) jobs
5. Add the new TFM path to `$nugetTfmPaths` in `Get-BC-DevTools.ps1` for NuGet package analysis
6. The reference-based TFM fallback and dynamic `net{major}.0` derivation require no changes

## net10.0 pipeline support

The pipeline is prepared for net10.0 BC DevTools:

- **Detection**: `action.yml` computes `tfm-net100-version-lowest` from sources where TFM is `net10.0`
- **Download**: Conditional "Setup BC DevTools" steps in Build and Release jobs download net10.0 DevTools when available
- **NuGet path**: `setup-bc-devtools` accepts a `tfm` input (default `net8.0`) to resolve `tools/{tfm}/any` in NuGet packages
- **Analysis**: `Get-BC-DevTools.ps1` tries multiple NuGet TFM paths (`tools/net8.0/any`, `tools/net10.0/any`) when analyzing assemblies

## Known issues

| Issue | Workaround |
|---|---|
| `GetCustomAttributesData()` can fail if the installed .NET SDK is older than the assembly's target | Inner try/catch falls through to reference-based TFM detection via `GetReferencedAssemblies()` |
| `Assembly.Load($bytes)` can fail entirely for corrupted or incompatible assemblies | Returns `"analysis-error"` sentinel; sort and downstream consumers handle non-parseable versions |

## Key files

| File | Purpose |
|---|---|
| `Get-BC-DevTools.ps1` | Main orchestrator: cache management, assembly analysis, source enrichment |
| `Get-Sources.ps1` | Source merging: combines Marketplace, NuGet, BCArtifact with cache data |
| `action.yml` | GitHub Action interface: deduplication, TFM boundary detection, output setting |
| `BC-Artifacts.ps1` | BCArtifact feed query |
| `Marketplace.ps1` | VS Marketplace API query |
| `NuGet-Packages.ps1` | NuGet.org API query |
| `Display-Sources.ps1` | Log display formatting |
