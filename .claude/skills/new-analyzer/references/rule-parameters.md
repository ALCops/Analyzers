# Rule parameters — allowed values

Checklist for the `/new-analyzer` hard gate. Every row must be answered by the user (or quoted from the issue) before any file is touched; only the ID is computed.

| Parameter | Allowed values / notes |
|---|---|
| `Category` | Constants in `DiagnosticDescriptors.Category`: `Design`, `Naming`, `Style`, `Usage`, `Performance`, `Security`, `Internal` (`Internal` is reserved for the `XX0000` exception descriptor). |
| `DefaultSeverity` | `DiagnosticSeverity.Error` / `Warning` / `Info` / `Hidden`. Ask which; do not default to `Warning`. |
| `isEnabledByDefault` | `true` or `false` (opt-in). Opt-in rules need a Design-decision row explaining why, and their tests need the enable-by-ruleset setup described in `.claude/rules/testing.md` (§Testing rules that are `isEnabledByDefault: false`). |
| CodeFix | `now` (scaffold with `/new-codefix` right after), `later` (note in the rule doc's Roadmap), `never` (Design-decision row with the reason, e.g. "fix requires call-site rewrites"). |
| Configurable setting | Property name, type, default, and the `alcops.json` key. Requires `ALCopsSettings.cs` + `alcops.schema.json` in the same change (`.claude/rules/settings-schema.md`). |
| Version gate | The lowest BC runtime/SDK the rule applies to, expressed with a `VersionProvider` helper (e.g. `Spring2021OrGreater`, `Fall2024OrGreater`, `Fall2025OrGreater`). Derived from the SDK study: if the SDK member is missing on older TFMs, gate or stub; tests then use `RequireMinimumVersion(...)` / `SkipTestIfVersionIsTooLow(...)`. |
| Help URI | Always `https://alcops.dev/docs/analyzers/{copslug}/{id}/` — not a parameter, but confirm the cop slug (`applicationcop`, `documentationcop`, `formattingcop`, `lintercop`, `platformcop`, `testautomationCop`). |
