---
paths:
  - "src/ALCops.Common/**/ConfigurationCouldNotBeLoaded*"
  - "src/ALCops.Common/Settings/**"
---

# CM0001: ConfigurationCouldNotBeLoaded

## Purpose

Warns when an `alcops.json` configuration file is found but cannot be fully applied: the file is unreadable, its JSON is malformed, or it contains unknown top-level settings (typo'd key names). Without it the settings provider falls back to defaults silently and users never learn why their configuration has no effect.

**References:** [issue #328](https://github.com/ALCops/Analyzers/issues/328); PR #500 (remote `Extends` configuration) defers its failure diagnostics here.

## Design decisions

| Decision | Rationale |
|---|---|
| Hosted in `ALCops.Common.dll` itself — the only analyzer outside the six cops, and the only `CM` diagnostic | One warning instead of six duplicates. Safe because every documented install path already lists `ALCops.Common.dll` as an analyzer reference, and `alc` discovers analyzer types purely by the `[DiagnosticAnalyzer]` attribute. |
| Derives from the SDK `DiagnosticAnalyzer` directly, never the `ALCopsDiagnosticAnalyzer` harness | The issue-#389 AL1003 trap is a *base class* in a sibling DLL failing type-load; a base type inside the compiler's own assemblies always resolves. |
| `RegisterCompilationAction`, reporting at `Location.None` | Compilation-level actions run under every partial-analysis pass; `CompilationStartAnalysisContext` has no `ReportDiagnostic`; alcops.json is not part of the compilation, so there is no source location (VS Code surfaces it against app.json, like AD0001). |
| Failures live in the settings cache (`ALCopsSettingsLoadResult`), re-read by each compilation action | No accumulator pattern, no analyzer instance state. The cache has no invalidation, so editing alcops.json may not refresh the diagnostic in the IDE until reload — same staleness the settings themselves already have. |
| Malformed/unreadable file → defaults **plus** failure record; the malformed→defaults invariant is unchanged | Consumers rely on always getting a usable `ALCopsSettings` (see `common-library.md`). CM0001 is purely additive. |
| Unknown-key scan is top-level only, case-insensitive, reflection-derived from `ALCopsSettings` properties, with `$schema` allowlisted | Both serializers match properties case-insensitively; reflection keeps the key set self-maintaining; nested typos are already caught by `alcops.schema.json` (`additionalProperties: false`) in schema-aware editors. Unknown keys do **not** discard the valid settings. |
| One diagnostic per unknown key | Each typo is independently fixable; `Unreadable`/`Invalid` are inherently single. |
| An unreadable app-folder file does **not** fall through to parent traversal | The app-level file was intended to win; silently applying a parent file would mask the problem. (Behavior change vs. pre-CM0001.) |
| Virtual-file source path built from `GetDirectoryPath()` + file name, not `IFileSystem.GetAbsolutePath` | `GetAbsolutePath` does not exist on `IFileSystem` at the oldest SDK the netstandard2.1 binary runs on (AL 12) — calling it would throw `MissingMethodException` there. |
| `MessageFormat` = `The ALCops configuration '{0}' could not be fully loaded: {1}` with free-text reason | Generic enough that PR #500 can plug remote failure reasons (unreachable URL, timeout, illegal `Extends` chain) into the same descriptor via new `SettingsLoadFailureKind` members. `{1}` may contain OS-localized exception text — tests only assert substrings we control. |

## Architecture

- `Settings/SettingsLoadFailure.cs` — `SettingsLoadFailureKind` (`Unreadable`, `Invalid`, `UnknownSetting`) + failure record (`Kind`, `Source`, `Detail`).
- `Settings/ALCopsSettingsLoadResult.cs` — settings + `ImmutableArray<SettingsLoadFailure>`; the cache value type.
- `Settings/ALCopsSettingsProvider.cs` — `GetLoadResult(IFileSystem?)` is the real loader; `GetSettings` is a thin wrapper so the seven analyzer call sites stayed untouched. `IFileSystem.Exists` is checked before `OpenRead` to distinguish not-found from unreadable (both verified present at the AL 12 interface floor).
- `Analyzers/ConfigurationCouldNotBeLoaded.cs` — `[DiagnosticAnalyzer]`, `RegisterCompilationAction`, one `ReportDiagnostic` per failure.
- Descriptor infrastructure is Common's first: `DiagnosticIds.cs`, `DiagnosticDescriptors.cs` (category `Configuration`), `ALCops.CommonAnalyzers.resx`.
- Tests use a manual `Compilation.Create` + `CompilationWithAnalyzers` harness in `ALCops.Common.Test/Analyzers/` — RoslynTestKit's marker-based assertions cannot match `Location.None`. `ThrowingFileSystem` (Helpers) simulates exists-but-unreadable deterministically (file locks are advisory-only on Linux).

## Known issues

- TOCTOU between `Exists` and `OpenRead`: a file deleted in between reports as `Unreadable` with a file-not-found message. Rare, accepted.
- The compilation-action + cached-failure design re-reports on every compilation, but a *stale* cache entry (file fixed after first load) keeps reporting until the analyzer process restarts — inherent to the no-invalidation settings cache.

## Roadmap

- Nested unknown-key detection (`StatementBlockSpacing` sub-keys, `NamingPatterns` targets) — would need per-type key maps; currently left to the JSON schema in editors.
- Remote `Extends` failure kinds once PR #500 lands (unavailable source, timeout, illegal chain) — extend `SettingsLoadFailureKind`, no descriptor change.
