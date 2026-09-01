---
name: release
description: Walk through cutting an ALCops release (release branch, beta publishing, stable tag, cleanup, merge-back) per the GitVersion three-channel strategy. Only run when explicitly asked to release.
argument-hint: <version>   e.g. v1.2.0
disable-model-invocation: true
---

# Release

**Not for:** hotfixes or ordinary changes — those go through normal `fix/` / `feat/` PRs (into `release/vX.Y.Z` while a release is being stabilized).

Target version: `$ARGUMENTS` (e.g. `v1.2.0`). Read `.claude/rules/release-strategy.md` (channels, GitVersion computation, cleanup job, prerelease-tag protection) and `references/procedure.md` (the exact commands) before doing anything.

## Gates

Every push, tag, and workflow dispatch is outward-facing and irreversible enough to deserve its own confirmation. **Show the exact command, wait for an explicit yes, run it, report the real output — one step at a time. Do not batch steps or confirm once for the whole release.** Violating the letter of the gates is violating the spirit of the gates.

| Gate | Confirm before |
|---|---|
| 1 | Pushing `release/{version}` (this bumps `main` to the next minor alpha). |
| 2 | Each beta `workflow_dispatch` on the release branch. |
| 3 | Pushing the stable tag `{version}` (publishes to NuGet.org, creates the GitHub Release, deletes remote beta tags). |
| 4 | Merging the release branch back into `main` and pushing. |

## Pre-flight (read-only, no confirmation needed)

- Clean `git status`, on `main`, `git pull` done, CI green on `main`.
- Expected version matches GitVersion's rules (`dotnet gitversion` if installed; otherwise reason from the strategy doc). A `release/v1.2.0` branch must produce `1.2.0-beta.N` and the tag `v1.2.0`.
- `git tag -l "*-beta.*" "*-alpha.*"` is empty locally; if not, delete those tags first — the workflow rejects prerelease tag pushes, but do not rely on it.

## Walkthrough

Follow `references/procedure.md` step by step, applying the gate table: 1 branch → 2 stabilize/beta → 3 stable tag → local cleanup (`git tag -d`, `git fetch --prune --prune-tags`) → 4 merge-back. End with a list of executed steps, their output, and anything left for the user.

## Common Mistakes

| Mistake | Fix |
|---|---|
| Confirming "the release" once and running all steps | One confirmation per gate; stop after each and report. |
| Tagging from `main` instead of the release branch | `git checkout release/{version}` before `git tag`. |
| `git push --tags` | Pushes local beta tags; push the single tag `git push origin {version}`. |
| Forgetting local beta-tag cleanup after the stable tag | `git tag -d $(git tag -l "{version}-beta.*")` then `git fetch --prune --prune-tags`. |
| Merging back via a PR | Direct merge + push; the pull-request workflow deliberately skips CI for release-to-main merges. |
| Expecting a GitHub Release for a beta | Releases are created for stable tags only; alpha/beta are NuGet-only. |
