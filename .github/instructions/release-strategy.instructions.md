---
applyTo: '.github/**'
---

# Release Strategy

ALCops Analyzers uses a three-channel release strategy with GitVersion auto-computing all version numbers. NuGet packages are published to both NuGet.org and GitHub Packages.

## Release channels

| Channel | Source            | Trigger                    | Example                                |
|---------|-------------------|----------------------------|----------------------------------------|
| Alpha   | `main` branch     | Auto on every push         | `0.7.0-alpha.1`, `0.7.0-alpha.2`, ... |
| Beta    | `release/vX.Y.Z`  | Manual (workflow_dispatch) | `0.7.0-beta.1`, `0.7.0-beta.2`, ...   |
| Stable  | Tag `vX.Y.Z`      | Auto on tag push           | `0.7.0`                                |

### Alpha releases

Every push to `main` that passes CI automatically publishes an alpha package to NuGet.org and GitHub Packages. No GitHub Release is created. A weekly cleanup job unlists old alphas from NuGet.org (keeping the last 3 per channel) to prevent clutter.

### Beta releases

Created manually by running the CI/CD workflow via `workflow_dispatch` on a release branch (e.g., `release/v0.7.0`). The person running the workflow selects the release branch in the GitHub Actions UI. The workflow auto-creates a git tag (e.g., `v0.7.0-beta.1`) so that GitVersion's `ManualDeployment` mode increments `.N` sequentially per published beta, not per commit. Packages are published to NuGet.org and GitHub Packages. No GitHub Release is created.

### Stable releases

Triggered automatically when a tag matching `v*` is pushed (e.g., `v0.7.0`). Stable releases get a GitHub Release with an auto-generated changelog and the `.nupkg` attached. Packages are also published to NuGet.org and GitHub Packages. After a stable release, the weekly cleanup job unlists all superseded pre-releases.

## Version computation (GitVersion)

GitVersion with `GitHubFlow/v1` workflow auto-computes versions based on tags, branch names, and commit count.

### Configuration (`GitVersion.yml`)

```yaml
workflow: GitHubFlow/v1
branches:
  main:
    label: alpha
    increment: Minor
    tracks-release-branches: true
  release:
    mode: ManualDeployment
    label: beta
    prevent-increment:
      when-current-commit-tagged: true
```

### How versions are computed

- **Main branch** (`ContinuousDeployment`, GitHubFlow default): Takes the latest tag, bumps the minor version, appends `-alpha.N` where N is the commit count since the last version-relevant event. Every push gets a unique version. `tracks-release-branches: true` means creating a release branch causes main to auto-bump to the next minor.
- **Release branch** (`ManualDeployment`): Extracts the version from the branch name (e.g., `release/v0.7.0` produces `0.7.0-beta.N`). The `.N` only increments when a new tag exists. The CI/CD workflow auto-creates tags on each beta publish, producing sequential numbering (beta.1, beta.2, ...) with no gaps.
- **Tagged commit**: Uses the tag value directly (e.g., tag `v0.7.0` produces version `0.7.0`).

### Version progression example

```
v0.6.1 (current latest stable)
  |
  v (merges to main, auto-published)
  0.7.0-alpha.1 -> 0.7.0-alpha.2 -> ... -> 0.7.0-alpha.N
  |
  v (create release/v0.7.0 branch)
  main auto-bumps to: 0.8.0-alpha.1, 0.8.0-alpha.2, ...
  release branch (workflow_dispatch publishes + auto-tags):
    commit 1 -> publish -> tag v0.7.0-beta.1
    commit 2, 3 (not published, no tag, no version bump)
    commit 4 -> publish -> tag v0.7.0-beta.2  (sequential, no gaps)
  |
  v (tag v0.7.0 on release branch)
  0.7.0 (stable release, auto-published)
  cleanup unlists: all 0.7.0-alpha.*, all 0.7.0-beta.*
```

### Alpha vs beta numbering

- **Alpha** (`ContinuousDeployment`): Every push to main gets a unique `.N` based on commit count. Since every push auto-publishes, there are no gaps. Gaps could appear if commits only change non-source files (path-filtered), but this is acceptable.
- **Beta** (`ManualDeployment`): The `.N` only increments when the workflow auto-creates a tag. Between tags, GitVersion produces the same version with different build metadata (stripped by NuGet). This guarantees sequential `beta.1`, `beta.2`, ... with no gaps on NuGet.

### Overriding the increment

By default, main increments the Minor version. Override per commit message:
- `+semver: patch` (bump patch instead of minor)
- `+semver: major` (bump major, e.g., for v1.0.0)
- `+semver: minor` (explicit minor, same as default)

## Branch naming conventions

| Branch pattern    | Purpose                                   |
|-------------------|-------------------------------------------|
| `main`            | Development trunk, produces alpha releases |
| `release/vX.Y.Z`  | Release stabilization, produces betas     |
| `fix/<desc>`      | Bug fix branches (PR to main)             |
| `feat/<desc>`     | Feature branches (PR to main)             |
| `docs/<desc>`     | Documentation branches (PR to main)       |

## How to create a release

### 1. Create the release branch

```bash
git checkout main
git pull
git checkout -b release/v0.7.0
git push -u origin release/v0.7.0
```

This automatically bumps main to the next minor (e.g., `0.8.0-alpha.1`).

### 2. Stabilize on the release branch

Fix bugs by creating PRs to the release branch. Each merge can be published as a beta via workflow_dispatch.

### 3. Publish beta releases

1. Go to GitHub Actions > CI/CD workflow
2. Click "Run workflow"
3. Select the release branch (e.g., `release/v0.7.0`)
4. Click "Run workflow"

### 4. Create the stable release

```bash
git checkout release/v0.7.0
git tag v0.7.0
git push origin v0.7.0
```

The tag push automatically triggers CI/CD, which builds, tests, publishes to NuGet, creates a GitHub Release with changelog, and deletes the beta tags (e.g., `v0.7.0-beta.1`, `v0.7.0-beta.2`) from the repo.

### 5. Clean up local beta tags

```bash
git tag -d $(git tag -l "v0.7.0-beta.*")
```

The CI deletes beta tags from the remote, but local clones still have them. Delete them locally to prevent accidental re-push via `git push --tags`. The workflow also has trigger-level and job-level guards that reject prerelease tag pushes, but cleaning up locally is good hygiene.

### 6. Merge back to main

```bash
git checkout main
git merge release/v0.7.0
git push
```

The pull-request workflow skips CI for release-to-main merges (housekeeping, not feature work).

### Tag hygiene

After a stable release, sync your local tags with the remote:

```bash
git fetch --prune --prune-tags
```

This removes any local tags that no longer exist on the remote (including deleted beta tags). Recommended after every stable release, or periodically.

## Cleanup job

A weekly scheduled workflow (`scheduled-cleanup.yml`) runs every Sunday at 06:00 UTC. It can also be triggered manually.

### What it does

1. **Superseded pre-releases**: Unlists from NuGet.org and deletes from GitHub Packages all pre-release versions whose base version is at or below the latest stable (e.g., all `0.7.0-alpha.*` and `0.7.0-beta.*` after `0.7.0` is released)
2. **Old current pre-releases**: For pre-releases above the latest stable, keeps the last 3 per channel (alpha and beta independently) and unlists/deletes the rest. This prevents alphas (which have higher base versions) from pushing out betas.

### NuGet.org vs GitHub Packages cleanup

- **NuGet.org**: Unlists packages. Unlisted packages are hidden from search but remain downloadable by exact version pin. This is safe for users who have pinned a specific version.
- **GitHub Packages**: Permanently deletes old versions. Accepted trade-off for pre-releases.

## SemVer ordering

The `.N` suffix pattern (`alpha.1`, `alpha.2`, `beta.1`) was chosen over a patch-increment scheme (`v1.0.0-alpha`, `v1.0.1-alpha`, `v1.0.2-alpha`) for SemVer correctness.

With patch-increment pre-releases, `1.0.1-beta > 1.0.0` (stable), meaning a beta would sort higher than the stable release it precedes. The `.N` suffix keeps all pre-releases below their target stable version: `1.0.0-beta.99 < 1.0.0`.

## CI/CD workflows

| Workflow               | Trigger                          | Purpose                                |
|------------------------|----------------------------------|----------------------------------------|
| `build-test.yml`       | Reusable (called by others)      | Build and test all projects            |
| `pull-request.yml`     | Pull requests to main/release    | PR validation                          |
| `build-and-release.yml`| Push to main, tags, workflow_dispatch | Build, test, pack, publish      |
| `scheduled-build.yml`  | Daily cron                       | Cache keepalive                        |
| `scheduled-cleanup.yml`| Weekly cron, workflow_dispatch   | Unlist/delete old pre-releases         |

### build-and-release.yml release conditions

The release job runs when:
- `refs/heads/main` -> publish alpha packages
- `refs/heads/release/*` + workflow_dispatch -> publish beta packages
- `refs/tags/v*` (stable only, no `-` in tag) -> publish stable packages + create GitHub Release with changelog

Push to a release branch without workflow_dispatch runs build-and-test only (no publish). This gives CI feedback on commits without publishing every change.

GitHub Releases are only created for stable versions (tag pushes). Alpha and beta versions are NuGet-only.

### Prerelease tag push protection

The workflow has two layers of protection against accidental prerelease tag pushes (e.g., from `git push --tags` re-pushing deleted beta tags):

1. **Trigger filter**: `on.push.tags` uses `"!v*-*"` to exclude any tag with a hyphen (all SemVer prerelease tags). The workflow doesn't start at all.
2. **Job condition**: The release job's `if` adds `!contains(github.ref, '-')` to the tag branch, as defense-in-depth.

Beta tags are created inside the workflow using `GITHUB_TOKEN`, which doesn't trigger new runs, so the beta flow is unaffected.

## What external contributors should expect

- PRs are validated by `pull-request.yml` (build + test)
- PRs do not produce any release or package
- Once merged to main, the change appears in the next alpha automatically
- The maintainers decide when to cut a release branch and tag a stable version
- Pre-release versions may be unlisted from NuGet.org after a stable release, but remain downloadable by exact version
