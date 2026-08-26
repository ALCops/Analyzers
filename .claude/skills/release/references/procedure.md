# Release procedure

Used by `/release`. Every `git push`, tag push, and workflow dispatch below is outward-facing and gets its own confirmation. Background (channels, GitVersion computation, cleanup job, prerelease-tag protection): `.claude/rules/release-strategy.md`.

## 1. Create the release branch

```bash
git checkout main
git pull
git checkout -b release/v0.7.0
git push -u origin release/v0.7.0
```

This automatically bumps main to the next minor (e.g., `0.8.0-alpha.1`).

## 2. Stabilize on the release branch

Fix bugs by creating PRs to the release branch. Each merge can be published as a beta via workflow_dispatch. Push to a release branch without workflow_dispatch runs build-and-test only (no publish), giving CI feedback on commits without publishing every change.

## 3. Publish beta releases

1. Go to GitHub Actions > CI/CD workflow
2. Click "Run workflow"
3. Select the release branch (e.g., `release/v0.7.0`)
4. Click "Run workflow"

## 4. Create the stable release

```bash
git checkout release/v0.7.0
git tag v0.7.0
git push origin v0.7.0
```

The tag push automatically triggers CI/CD, which builds, tests, publishes to NuGet, creates a GitHub Release with changelog, and deletes the beta tags (e.g., `v0.7.0-beta.1`, `v0.7.0-beta.2`) from the repo. GitHub Releases are only created for stable versions (tag pushes); alpha and beta versions are NuGet-only.

## 5. Clean up local beta tags

```bash
git tag -d $(git tag -l "v0.7.0-beta.*")
```

The CI deletes beta tags from the remote, but local clones still have them. Delete them locally to prevent accidental re-push via `git push --tags`. The workflow also has trigger-level and job-level guards that reject prerelease tag pushes (see below), but cleaning up locally is good hygiene.

## 6. Merge back to main

```bash
git checkout main
git merge release/v0.7.0
git push
```

The pull-request workflow skips CI for release-to-main merges (housekeeping, not feature work).

## Tag hygiene

After a stable release, sync your local tags with the remote:

```bash
git fetch --prune --prune-tags
```

This removes any local tags that no longer exist on the remote (including deleted beta tags). Recommended after every stable release, or periodically.
