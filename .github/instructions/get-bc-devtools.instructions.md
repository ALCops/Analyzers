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

For each new BC DevTools version, the script downloads `Microsoft.Dynamics.Nav.Analyzers.Common.dll` and inspects it using `System.Reflection.Metadata` (built into the .NET runtime, no NuGet package needed):

1. Open the DLL file via `PEReader` (no assembly loading into the AppDomain)
2. Get a `MetadataReader` from the PE reader
3. Read `AssemblyVersion` from the assembly definition
4. Walk custom attributes on the assembly definition to find `TargetFrameworkAttribute`
5. Decode the attribute blob to extract the framework name (e.g. `.NETCoreApp,Version=v10.0` → `net10.0`)

This approach reads PE metadata directly from the file bytes without loading the assembly into the runtime, making it immune to cross-runtime version mismatches (e.g. inspecting a net10.0 DLL from a net8.0 host)

### Caching strategy

- `TargetFramework.json` is cached in GitHub Actions cache (`actions/cache`) with content-based keys
- Cache key pattern: `tfm-json-{sha256hash}` with `restore-keys: tfm-json-`
- The scheduled daily build keeps the cache alive (evicted after 7 days without access)
- Stale entries (packages no longer in any source) are pruned automatically

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Assembly inspection method | `System.Reflection.Metadata` via `PEReader` + `MetadataReader` | Reads PE metadata directly from file bytes without loading the assembly into the runtime. Immune to cross-runtime version mismatches (e.g. net10.0 DLL on net8.0 host). Built into the .NET runtime, no NuGet package needed. |
| TFM parsing | Regex on `TargetFrameworkAttribute` value | Handles `.NETStandard`, `.NETCoreApp`, and `.NETFramework` monikers. Future .NET versions are handled automatically. |
| Cache key strategy | Content-based (SHA256 hash) | Only creates new cache entries when data actually changes |

## Maintenance

When a new .NET major version appears in BC DevTools assemblies:
1. Add a `tfm-net{major*10}0-version-lowest` output to `action.yml` (e.g. `tfm-net100-version-lowest` for net10.0)
2. Propagate the new output through `build-test.yml` workflow_call outputs and setup job outputs
3. Add conditional "Setup BC DevTools" steps for the new TFM in Build (`build-test.yml`) and Release (`build-and-release.yml`) jobs
4. Add the new TFM path to `$nugetTfmPaths` in `Get-BC-DevTools.ps1` for NuGet package analysis
5. The `System.Reflection.Metadata`-based TFM detection and TFM parsing require no changes (they handle any .NET version automatically)

## net10.0 pipeline support

The pipeline is prepared for net10.0 BC DevTools:

- **Detection**: `action.yml` computes `tfm-net100-version-lowest` from sources where TFM is `net10.0`
- **Download**: Conditional "Setup BC DevTools" steps in Build and Release jobs download net10.0 DevTools when available
- **NuGet path**: `setup-bc-devtools` accepts a `tfm` input (default `net8.0`) to resolve `tools/{tfm}/any` in NuGet packages
- **Analysis**: `Get-BC-DevTools.ps1` tries multiple NuGet TFM paths (`tools/net8.0/any`, `tools/net10.0/any`) when analyzing assemblies

## Known issues

| Issue | Workaround |
|---|---|
| Corrupted or non-.NET assemblies cannot be read by `PEReader` | Returns `"analysis-error"` sentinel; downstream consumers should handle non-parseable versions |

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
