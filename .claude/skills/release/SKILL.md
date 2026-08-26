---
name: release
description: Walk through cutting an ALCops release (release branch, beta publishing, stable tag, cleanup, merge-back) per the GitVersion three-channel strategy. Only run when explicitly asked to release.
argument-hint: <version>   e.g. v1.2.0
disable-model-invocation: true
---

# Release

Target version: `$ARGUMENTS` (e.g. `v1.2.0`). Read `.claude/rules/release-strategy.md` for the channel model, GitVersion computation, tag hygiene, and the cleanup job before doing anything. Every push and every tag below is outward-facing: **show the exact command and wait for confirmation before running it.**

## Pre-flight

- `git status` clean, on `main`, `git pull` done, CI green on `main`.
- Confirm `GitVersion.yml` produces the expected version (`dotnet gitversion` if installed, otherwise reason from the branch/tag rules in the strategy doc).
- No local prerelease tags that could be pushed accidentally: `git tag -l "*-beta.*" "*-alpha.*"`; the workflow rejects prerelease tag pushes, but clean up anyway.

## Procedure

1. **Release branch:** `git checkout -b release/{version}` from `main`, push with `-u`. This bumps `main` to the next minor alpha automatically.
2. **Stabilize:** bug fixes go to the release branch via PRs (`fix/...` branches targeting `release/{version}`).
3. **Beta:** run the CI/CD workflow via `workflow_dispatch` on the release branch (`gh workflow run build-and-release.yml --ref release/{version}`); each run publishes a beta.
4. **Stable:** on the release branch, `git tag {version}` then `git push origin {version}`. The tag push builds, tests, publishes to NuGet.org, creates the GitHub Release with changelog, and deletes the remote beta tags.
5. **Local cleanup:** `git tag -d $(git tag -l "{version}-beta.*")` and `git fetch --prune --prune-tags`.
6. **Merge back:** `git checkout main && git merge release/{version} && git push` (CI is skipped for release-to-main housekeeping merges).

Report which steps were executed and which were left for the user, with the commands.
