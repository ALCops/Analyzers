---
paths:
  - "src/ALCops.Common/**/ConfigurationCouldNotBeLoaded*"
  - "src/ALCops.Common/Settings/**"
  - "src/ALCops.Common.Test/Analyzers/ConfigurationCouldNotBeLoaded*"
---

# CM0001: ConfigurationCouldNotBeLoaded

## Purpose

Warns when an `alcops.json` configuration file is found but cannot be fully applied: the file is unreadable, its JSON is malformed, or it contains unknown top-level settings (typo'd key names). Without it the settings provider falls back to defaults silently and users never learn why their configuration has no effect.

Registers `RegisterCompilationAction` (no node or symbol kinds; reports at `Location.None`); main type `ConfigurationCouldNotBeLoaded`.

**References:** [#328](https://github.com/ALCops/Analyzers/issues/328); [discussion #483](https://github.com/ALCops/Analyzers/discussions/483) (remote `Extends` configuration); [inheritance review](https://github.com/ALCops/Analyzers/pull/500#pullrequestreview-5112244814) (atomic fallback and HTTP response-size limit).

## Design decisions

| Decision | Rationale |
|---|---|
| Hosted in `ALCops.Common.dll` itself, the only analyzer outside the six cops and the only `CM` diagnostic | One warning instead of six duplicates. Safe because every documented install path already lists `ALCops.Common.dll` as an analyzer reference and `alc` discovers analyzer types purely by the `[DiagnosticAnalyzer]` attribute. |
| Derives from the SDK `DiagnosticAnalyzer` directly, never the `ALCopsDiagnosticAnalyzer` harness | The AL1003 trap ([#389](https://github.com/ALCops/Analyzers/issues/389)) is a base class in a sibling DLL failing type-load; a base type inside the compiler's own assemblies always resolves. |
| Compilation action reporting at `Location.None`, not a compilation-start or per-declaration action | Compilation-level actions run under every partial-analysis pass and `CompilationStartAnalysisContext` has no `ReportDiagnostic`; alcops.json is not part of the compilation, so there is no source location (VS Code surfaces it against app.json, like AD0001). |
| Failures are stored in the settings cache (`ALCopsSettingsLoadResult`) and re-read by each compilation action | No accumulator pattern and no analyzer instance state; the diagnostic shares the settings' own cache lifetime. |
| Malformed or unreadable file yields defaults **plus** a failure record; the malformed-to-defaults invariant is unchanged | Consumers rely on always getting a usable `ALCopsSettings` (see `common-library.md`); CM0001 is purely additive. |
| Unknown-key scan is top-level only, case-insensitive, reflection-derived from `ALCopsSettings` properties, with `$schema` allowlisted; unknown keys do not discard the valid settings | Both serializers match properties case-insensitively and reflection keeps the key set self-maintaining. Nested typos are already caught by `alcops.schema.json` (`additionalProperties: false`) in schema-aware editors. |
| One diagnostic per unknown key | Each typo is independently fixable; `Unreadable`/`Invalid` are inherently single. |
| An unreadable app-folder file does **not** fall through to parent-directory traversal | The app-level file was intended to win; silently applying a parent file would mask the problem. Behavior change relative to pre-CM0001. |
| Virtual-file source path built from `GetDirectoryPath()` + file name, not `IFileSystem.GetAbsolutePath` | `GetAbsolutePath` does not exist on `IFileSystem` at the oldest SDK the netstandard2.1 binary runs on (AL 12); calling it would throw `MissingMethodException` there. |
| `MessageFormat` carries a free-text reason (`The ALCops configuration '{0}' could not be fully loaded: {1}`) | Remote `Extends` failures such as unreachable URLs, timeouts, credential-bearing URLs, and illegal chains reuse the same descriptor through the existing `Unreadable` and `Invalid` failure kinds. |
| A failed declared `Extends` discards both the base and local overrides, returning complete built-in defaults | Applying only the overrides would leave a partially configured project; the warning makes the complete fallback visible. Unknown setting names remain non-fatal, preserving recognized values as for local configuration. |
| HTTP responses are bounded through `HttpClient.MaxResponseContentBufferSize`, while keeping the five-second timeout | The byte limit is enforced during buffering even without Content-Length, so a large response cannot bypass the limit by using chunked transfer. Checking string length after downloading would already have allocated the oversized body. |
| A rejected URL omits `Uri.UserInfo` from its recorded source | The CM0001 message renders that source directly in IDE diagnostics and build logs; rejecting network access must not expose the credentials in the error message. |
| HTTP request failures remain stable within a compilation but are retried by a later compilation | A temporary outage must not permanently poison the workspace cache. All consumers use the compilation from CompilationStart so the SDK's different SemanticModel.Compilation object cannot split that snapshot. |
| Caller cancellation propagates without a diagnostic or cached result; cache waiters can cancel independently | Editing or reloading a workspace must stop both the pending HTTP body read and unnecessary waits behind another caller. Synchronous SDK callbacks still wait for the first uncached load; all async continuations avoid context capture. |
| JSON parsing, key lookup, merge and type validation share ALCopsSettingsDocument | Local and external configuration use the same comment/trailing-comma/casing policy on both serializer stacks. Retaining local type validation before network access prevents unnecessary requests for invalid local values. |
| Null, empty or comment-only local input uses defaults silently; an inherited document still requires an object | An empty local settings file declares no policy. A declared base is required input and must pass its own validation. |

## Deliberate non-reports

- A missing `alcops.json` is silent: `IFileSystem.Exists` is checked before `OpenRead` so not-found is distinguished from unreadable.
- Unknown keys nested inside `StatementBlockSpacing`, `NamingPatterns` and other sub-objects are not scanned; the JSON schema (`additionalProperties: false`) reports them in schema-aware editors.
- `$schema` is allowlisted as a top-level key.

## Known issues

- TOCTOU between `Exists` and `OpenRead`: a file deleted in between is reported as `Unreadable` with a file-not-found message. Rare, accepted.
- Successfully loaded settings and deterministic configuration errors have no invalidation. Correcting those still requires restarting the analyzer process; failed HTTP requests instead retry on a later compilation.

## SDK facts

- `IFileSystem.Exists` and `IFileSystem.OpenRead` are present at the AL 12 interface floor; `IFileSystem.GetAbsolutePath` is not.
- `CompilationStartAnalysisContext` has no `ReportDiagnostic`; only `CompilationAnalysisContext` can report at compilation level.

## Test notes

- Tests use a manual `Compilation.Create` + `CompilationWithAnalyzers` harness in `ALCops.Common.Test/Analyzers/` because RoslynTestKit's marker-based assertions cannot match `Location.None`.
- `ThrowingFileSystem` (Helpers) simulates exists-but-unreadable deterministically; real file locks are advisory-only on Linux.
- `{1}` may contain OS-localized exception text, so tests assert only substrings the code controls.
