---
applyTo: '.github/**'
---

# CI/CD Instructions for ALCops Analyzers

This document describes the CI/CD infrastructure for the ALCops Analyzers project. Use it as a reference when modifying workflows, actions, or build scripts.

## Project overview

ALCops Analyzers is a .NET-based suite of Roslyn-style code analyzers for Microsoft Dynamics 365 Business Central AL code. The analyzers are compiled against `Microsoft.Dynamics.BusinessCentral.Development.Tools` (BC DevTools) and tested against multiple BC versions. The final artifact is a NuGet package (`ALCops.Analyzers`).

Analyzer projects:
- `ALCops.Common` (shared library, no tests)
- `ALCops.ApplicationCop` + `ALCops.ApplicationCop.Test`
- `ALCops.DocumentationCop` + `ALCops.DocumentationCop.Test`
- `ALCops.FormattingCop` + `ALCops.FormattingCop.Test`
- `ALCops.LinterCop` + `ALCops.LinterCop.Test`
- `ALCops.PlatformCop` + `ALCops.PlatformCop.Test`
- `ALCops.TestAutomationCop` + `ALCops.TestAutomationCop.Test`

---

## 1. Workflow inventory

### `build-and-release.yml` (CI/CD)

**Triggers:**
- Push to `main` or `release/**` branches (path-filtered to source, project, and CI files; excludes `*.md`)
- Push of tags matching `v*`

**Permissions:** `contents: write`, `packages: write`, `pull-requests: read`

**Jobs:**

1. **`build-and-test`** - Calls the reusable `build-test.yml` workflow with `draft-check: false`.
2. **`release`** - Runs after `build-and-test` succeeds. Gated by condition: only on `main` branch or `v*` tags.
   - Checks out with full history (`fetch-depth: 0`) for GitVersion.
   - Sets up .NET 8.0.x SDK.
   - Sets up BC DevTools for both `netstandard2.1` and `net8.0` target frameworks (using the lowest version from build-and-test outputs).
   - Runs **GitVersion** (v6.3.x) to compute `SemVer`, `AssemblySemVer`, etc.
   - `dotnet pack` on `ALCops.Analyzers.csproj` with deterministic build settings (`ContinuousIntegrationBuild`, `EmbedUntrackedSources`).
   - On `v*` tags only: builds a changelog via `mikepenz/release-changelog-builder-action` and creates a GitHub Release with the `.nupkg` attached.
   - Publishes to **GitHub Packages** (`nuget.pkg.github.com`) using `GITHUB_TOKEN`.
   - Publishes to **NuGet.org** using `NUGET_API_KEY` secret.

**Artifacts produced:** `*.nupkg` in `./artifacts`.

### `build-test.yml` (Reusable build and test workflow)

**Triggers:** `workflow_call` (reusable) and `workflow_dispatch` (manual).

**Inputs:**
- `draft-check` (boolean, default `true`) - When true, skips the workflow for draft PRs.

**Outputs (propagated to callers):**
- `bc-devtools-sources` - JSON of all BC DevTools sources.
- `tfm-netstandard21-version-lowest` - Lowest BC DevTools version targeting netstandard2.0 (used for netstandard2.1 builds).
- `tfm-net80-version-lowest` - Lowest BC DevTools version targeting net8.0.

**Environment:** `DOTNET_NOLOGO: true`

**Jobs:**

1. **`setup`** - Restores `TargetFramework.json` from GitHub Actions cache, runs `get-bc-devtools` action (discovers all available BC DevTools versions from VS Marketplace, NuGet, and BC Artifacts), hashes and saves the cache, then runs `setup-test-matrix` to build the dynamic matrix.

2. **`build`** - Sets up .NET 8.0.x, installs BC DevTools for both TFMs (lowest versions), then builds each analyzer project individually (`dotnet restore` + `dotnet build --configuration Release`). Uploads each project's `bin/Release/` as a separate artifact.

3. **`test`** - Uses `strategy.matrix` from `setup` output (`fromJSON`). For each matrix entry (BC version + TFM), downloads build artifacts, restores test projects with `/p:NavTargetFramework=${{ matrix.tfm }}`, runs `dotnet test` for each analyzer's test project. Download and restore steps use `continue-on-error: true` (infrastructure issues shouldn't fail the job). Test run steps use `if: !cancelled()` without `continue-on-error`, so all cops run even if one fails, but test failures properly fail the job. Merges `.trx` results with `dotnet-trx-merge` and uploads per-version test results. Test reporting (check runs) is handled by the separate `test-report.yml` workflow.

### `pull-request.yml` (PR validation)

**Triggers:** `pull_request` targeting `main` or `release/**` (same path filters as CI/CD; excludes `*.md`).

**Permissions:** `contents: read`, `actions: read`

**Jobs:**
- Calls `build-test.yml` with `draft-check: true`.
- Skips entirely when merging from `release/**` into `main` (condition: `!(github.base_ref == 'main' && startsWith(github.head_ref, 'release/'))`).

### `test-report.yml` (Test reporting)

**Triggers:** `workflow_run` on `['Pull Request', 'CI/CD', 'Scheduled Build']` workflows, `types: [completed]`.

**Permissions:** `contents: read`, `actions: read`, `checks: write`, `pull-requests: write`

**Purpose:** Centralized test reporting for all workflows. Publishes test results via `dorny/test-reporter` as check runs. Runs as a separate `workflow_run`-triggered workflow so that fork PRs get test reports (fork PRs receive a read-only `GITHUB_TOKEN` that prevents check run creation from within the PR workflow).

**Jobs:**
- **`report`** - Downloads test result artifacts from the triggering workflow run, then publishes a unified test report via `dorny/test-reporter`. Only runs when the triggering workflow completed (not cancelled/skipped). Uses `fail-on-error: true` and `fail-on-empty: true`.

### `scheduled-build.yml` (Daily scheduled build)

**Triggers:**
- `schedule`: daily at 06:00 UTC (`0 6 * * *`).
- `workflow_dispatch` (manual).

**Purpose:**
1. Keeps the `TargetFramework.json` GitHub Actions cache alive (evicted after 7 days of inactivity).
2. Automatically picks up newly published BC DevTools versions.

**Jobs:** Calls `build-test.yml` with `draft-check: false`.

---

## 2. Custom actions

### `get-bc-devtools/` (Composite action)

**Purpose:** Discovers all available BC DevTools sources from three providers (VS Marketplace VSIX, NuGet, BC Artifacts), determines their target framework and assembly version, and outputs a unified JSON list.

**Inputs:**
- `json-path` (optional) - Path to the `TargetFramework.json` cache file. Defaults to `${{ runner.temp }}/TargetFramework.json`.

**Outputs:**
- `sources` - Compressed JSON array of all BC DevTools sources with metadata (version, packageType, packageVersion, tfm, uri, isLatest, isPreview, isBeta).
- `tfm-netstandard21-version-lowest` - Lowest version targeting netstandard2.0.
- `tfm-net80-version-lowest` - Lowest version targeting net8.0.

**Steps:**
1. Runs `Get-BC-DevTools.ps1` to collect all sources with TFM analysis.
2. Deduplicates by version, preferring NuGet > VSIX > BCArtifact.
3. Identifies lowest versions per TFM.
4. Runs `Display-Sources.ps1` to render a markdown table in the workflow log.

### `setup-bc-devtools/` (Composite action)

**Purpose:** Downloads and extracts BC DevTools DLLs for a specific version to a local folder for compilation.

**Inputs:**
- `sources` (required) - JSON string from `get-bc-devtools`.
- `version-number` (required) - Exact version to download.
- `target-path` (optional, default: `Microsoft.Dynamics.BusinessCentral.Development.Tools`) - Local extraction path.

**Steps:**
1. Finds the matching source entry by version.
2. Downloads the package (VSIX, NuGet `.nupkg`, or BC Artifact ZIP).
3. For BCArtifact type: first extracts `ALLanguage.vsix` from the outer ZIP.
4. Extracts the `Analyzers` folder from the archive using `shared/Extract-RequiredFiles.ps1`. Extraction paths differ by type:
   - VSIX: `extension/bin/Analyzers`
   - NuGet: `tools/net8.0/any`
   - BCArtifact: `extension/bin/Analyzers` (from the inner VSIX)

### `setup-test-matrix/` (Composite action)

**Purpose:** Builds the dynamic `strategy.matrix` JSON for the test job.

**Inputs:**
- `sources` (required) - JSON from `get-bc-devtools`.

**Outputs:**
- `matrix` - JSON object with `include` array. Each entry has at minimum `version` and `tfm` fields.
- `isempty` - `"true"` or `"false"`.

**Logic:**
- Deduplicates by version, preferring NuGet entries.
- Removes specific known-bad versions: `16.0.21.41228`, `16.0.21.42129`, `16.0.21.53261`, `16.0.21.57573`.
- Produces `{"include": [...]}` for `fromJSON()` consumption.

### `shared/` (Shared utilities)

Contains PowerShell scripts used by multiple actions:
- **`Extract-RequiredFiles.ps1`** - Extracts files from a specific path within a ZIP archive to a destination folder. Handles path normalization and traversal protection.
- **`Get-RemoteZipEntry.ps1`** - Extracts a single file from a remote ZIP using HTTP Range requests (avoids downloading full archives). Supports ZIP32/ZIP64. Uses `System.Net.HttpWebRequest` with `.AddRange()` to preserve Range headers across CDN redirects.

---

## 3. PowerShell scripts in `get-bc-devtools/`

| Script | Purpose |
|---|---|
| `Get-BC-DevTools.ps1` | Main orchestrator. Reads `TargetFramework.json` cache, calls `Get-Sources.ps1` to discover all sources, identifies missing versions, downloads DLLs via HTTP Range requests, reflects on assemblies to determine TFM and assembly version, updates the cache. |
| `Get-Sources.ps1` | Merges sources from `Marketplace.ps1`, `NuGet-Packages.ps1`, and `BC-Artifacts.ps1`. Enriches entries with cached TFM data from `TargetFramework.json`. Outputs unified JSON. |
| `Marketplace.ps1` | Queries the VS Marketplace API for the `ms-dynamics-smb.al` extension. Returns all versions >= 12.0.0, filtering pre-release versions older than the current stable release. Supports retry with exponential backoff. |
| `NuGet-Packages.ps1` | Queries NuGet flat container API for `Microsoft.Dynamics.BusinessCentral.Development.Tools`. Returns versions >= 12.0.0. Filters beta versions older than highest stable. |
| `BC-Artifacts.ps1` | Uses `BcContainerHelper` module to query BC Sandbox artifact URLs for `SecondToLastMajor`, `Current`, `NextMinor`, and `NextMajor` selects. Current and SecondToLastMajor are stable; NextMinor/NextMajor are beta. |
| `Display-Sources.ps1` | Renders a formatted markdown table of all sources to the workflow log for debugging. |

---

## 4. GitVersion configuration

**File:** `GitVersion.yml`

**Strategy:** `GitHubFlow/v1`

**Branch rules:**

| Branch pattern | Label | Increment | Notes |
|---|---|---|---|
| `main` | `alpha` | `Patch` | `tracks-release-branches: true` |
| `release` | (none) | (default) | `prevent-increment.when-current-commit-tagged: true` (uses exact tag value) |

**How it works:**
- Commits to `main` produce pre-release versions like `1.2.3-alpha.4`.
- Release branches use the tag value directly when tagged (no further incrementing).
- Tagging with `v*` on a release branch produces a stable release version.
- GitVersion 6.3.x is used with `gittools/actions/gitversion` v4.4.2.

---

## 5. Dependabot

**File:** `.github/dependabot.yml`

**Scope:** GitHub Actions dependencies only (`package-ecosystem: "github-actions"`).
- Directory: `/` (root)
- Schedule: weekly
- Open PR limit: 10
- Groups: `github-official` pattern matching `actions/*`

Dependabot does NOT manage NuGet dependencies. NuGet packages are managed via `Directory.Packages.props` (central package management).

---

## 6. Build matrix

The test matrix is dynamically generated from live BC DevTools source discovery:

1. `get-bc-devtools` action queries three providers (VS Marketplace, NuGet.org, BC Artifacts CDN).
2. Each source is analyzed for its target framework moniker (TFM) and AL assembly version.
3. `setup-test-matrix` deduplicates and filters into a matrix JSON.
4. The `test` job in `build-test.yml` uses `strategy.matrix: ${{ fromJSON(needs.setup.outputs.matrix) }}` with `fail-fast: false`.

Each matrix entry provides:
- `version` - The AL assembly version (e.g., `15.0.0.0`).
- `tfm` - The target framework moniker (e.g., `net8.0` or `netstandard2.0`).

Tests run with `/p:NavTargetFramework=${{ matrix.tfm }}` passed to both restore and test commands. BC DevTools are always extracted to the `net8.0` folder for testing regardless of original TFM.

The matrix can include 20+ versions spanning multiple BC releases. Known-bad versions are excluded in `setup-test-matrix/action.yml`.

---

## 7. Release process

### Pre-release (alpha) packages
- Every push to `main` (that passes path filters) triggers `build-and-release.yml`.
- GitVersion produces an `alpha` pre-release version (e.g., `1.2.3-alpha.4`).
- The NuGet package is published to both GitHub Packages and NuGet.org.
- No GitHub Release is created.

### Stable releases
1. Create a `release/**` branch from `main`.
2. Tag the commit with `v*` (e.g., `v1.2.3`).
3. Push the tag. This triggers `build-and-release.yml` because of the `tags: v*` trigger.
4. GitVersion uses the tag value directly (`prevent-increment.when-current-commit-tagged: true`).
5. A GitHub Release is created with:
   - Auto-generated changelog (via `mikepenz/release-changelog-builder-action`).
   - The `.nupkg` file attached.
   - `prerelease` flag set based on whether GitVersion detects a pre-release tag.
6. The package is published to GitHub Packages and NuGet.org.

### Release branch to main merges
- PRs from `release/**` into `main` skip the `pull-request.yml` workflow entirely (explicit condition in the workflow).

---

## 8. Secrets and variables

| Secret/Variable | Used in | Purpose |
|---|---|---|
| `GITHUB_TOKEN` | `build-and-release.yml` | Changelog generation, GitHub Release creation, GitHub Packages publish |
| `NUGET_API_KEY` | `build-and-release.yml` | NuGet.org publish. The workflow fails explicitly if this secret is not configured. |

No other custom secrets or environment variables are required. The BC DevTools sources are all public (VS Marketplace, NuGet.org, BC Artifacts CDN).

---

## 9. Guidelines for AI agents

### Adding a new workflow
1. Place the file in `.github/workflows/`.
2. Follow existing naming conventions (`kebab-case.yml`).
3. Reuse `build-test.yml` via `workflow_call` when possible instead of duplicating build logic.
4. Set explicit `permissions` at the workflow level (principle of least privilege).
5. Use the same path filters as existing workflows to avoid unnecessary runs.

### Adding a new analyzer project
1. Add build steps (restore + build + upload artifact) to the `build` job in `build-test.yml`.
2. Add test steps (download artifact + restore + test) to the `test` job in `build-test.yml`.
3. Follow the exact pattern of existing analyzers: `continue-on-error: true` on download/restore steps, `if: !cancelled()` on all cop steps (download, restore, test), no `continue-on-error` on test steps (so failures properly gate releases), pass `/p:NavTargetFramework=${{ matrix.tfm }}`, use the same TRX logger format.

### Modifying the build matrix
- The matrix is fully dynamic; you rarely need to change it manually.
- To exclude a version: add it to the `$unwantedVersions` array in `setup-test-matrix/action.yml`.
- To change the minimum version floor: update the `12.0.0` filter in both `Marketplace.ps1` and `NuGet-Packages.ps1`.
- To change source priority: edit the `$typePriority` hashtable in `get-bc-devtools/action.yml`.

### Testing CI/CD changes
- All reusable workflows support `workflow_dispatch` for manual testing.
- Use the `build-test.yml` workflow dispatch to test build and matrix changes in isolation.
- Check the `Display-Sources.ps1` output in the workflow log to verify source discovery.
- When modifying PowerShell scripts, test locally with `pwsh` first. The scripts are designed to work both in GitHub Actions and locally.

### Common pitfalls
- **BC DevTools source discovery depends on external APIs.** VS Marketplace, NuGet.org, and BC Artifacts CDN can all be temporarily unavailable. The scripts have retry logic, but transient failures may still occur.
- **`TargetFramework.json` cache** is persisted via GitHub Actions cache. The scheduled build keeps it alive. If the cache is evicted, the next run re-analyzes all versions (slower but self-healing).
- **Test steps use `if: !cancelled()` without `continue-on-error`.** This ensures all 6 cops are tested even if one fails, while properly failing the job when tests fail. Download and restore steps keep `continue-on-error: true` (infrastructure issues shouldn't directly fail the job). The `test` job failure gates the `release` job in `build-and-release.yml`.
- **Test reporting uses a separate `workflow_run` workflow.** `test-report.yml` is triggered by `workflow_run` for all workflows (Pull Request, CI/CD, Scheduled Build). This is required because fork PRs receive a read-only `GITHUB_TOKEN` that cannot create check runs. Using a centralized workflow also avoids duplicating `dorny/test-reporter` configuration. The `workflow_run` file must exist on the default branch (main) before it can trigger. See `dorny/test-reporter`'s [recommended setup for public repositories](https://github.com/dorny/test-reporter#recommended-setup-for-public-repositories).
- **Path filters exclude `*.md` files.** Documentation-only changes do not trigger CI.
- **Release branch PRs into main skip validation.** This is intentional to avoid redundant builds.
- **NuGet publish uses `--skip-duplicate`.** Re-publishing an existing version is a no-op, not an error.
- **The `dotnet pack` step only runs on `ALCops.Analyzers.csproj`**, not on individual analyzer projects. This is the metapackage that bundles all analyzers.
- **HTTP Range requests** (`Get-RemoteZipEntry.ps1`) avoid downloading full packages (often 100+ MB). If you change archive paths or add new package types, update the extraction logic in both `setup-bc-devtools/action.yml` and `Get-BC-DevTools.ps1`.
