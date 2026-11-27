---
paths:
  - "src/ALCops.ApplicationCop/Analyzers/AppManifestPublisher*"
  - "src/ALCops.ApplicationCop.Test/Rules/AppManifestPublisher/**"
---

# AC0037: AppManifestPublisher

## Purpose

Checks the manifest publisher against an optional team-configured regular-expression convention. This lets organizations keep publisher metadata consistent across apps without hard-coding one publisher into ALCops.

Registers a compilation action and reports on `publisher` in `app.json`.

## Design decisions

| Decision | Rationale |
|---|---|
| Design category, Info severity, disabled by default | This is an opt-in portfolio convention, not a universal correctness rule. |
| Reuse `NamingPattern` with `AllowPattern` and `DisallowPattern` | Regex permits both exact values and organization-specific matching without a second pattern model. |
| No missing-publisher check without a configured pattern | `publisher` is mandatory in `app.json`; absence is already a manifest validity error. |
| Keep the rule applicable from BC 12.0 | The manifest members used by the rule are available at the supported SDK floor; `ManifestHelper` handles manifest retrieval compatibility. |
| No CodeFix | The analyzer does not own the user's `app.json`. |

## Deliberate non-reports

- No diagnostic is reported when neither regex is configured.
- No diagnostic is reported if a manifest cannot be obtained.

## Known issues

- Invalid or timed-out regex settings are reported through this rule's message because the configured publisher convention could not be evaluated.

## SDK facts

- `NavAppManifest.AppPublisher` and `GetDiagnosticLocation(string)` are available at the SDK floor (nav-sdk-docs reference tables for `NavAppManifest`).

## Test notes

- The diagnostic is enabled explicitly in the test compilation because `isEnabledByDefault` is false.
- Tests provide `app.json` and `alcops.json` through `MemoryFileSystem`.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `Manifest.Publisher` | unset | Optional `AllowPattern` and `DisallowPattern` requirements for `publisher`. |
