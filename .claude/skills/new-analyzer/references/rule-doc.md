---
paths:
  - "src/ALCops.{Cop}/**/{AnalyzerClassName}*"
  - "src/ALCops.{Cop}.Test/Rules/{AnalyzerClassName}/**"
---

# {ID}: {AnalyzerClassName}

## Purpose

{One to three sentences: what the rule detects and why it matters to AL developers.}

Registers `{Register*Action}` on `{kinds}`; main type `{Class or Walker}`. {One line only; the code is the source of truth for how it works.}

**References:** {optional: Microsoft Learn links, GitHub issues or discussions, blog posts that motivated the rule}

## Design decisions

<!-- Admission test: a row is a CHOICE BETWEEN ALTERNATIVES plus the reason it was made, in at most two sentences.
     Not a row: implementation steps, helper inventories, changelog entries ("fixed in #n"), or facts restating the code. -->

| Decision | Rationale |
|---|---|
| {What was chosen, e.g. "Severity Info", "Local variables only", "Custom FixAllProvider"} | {Why, including the alternative that was rejected} |
| {Version gate or netstandard2.1 stub, if any} | {Which SDK API is missing and how the rule degrades} |

## Deliberate non-reports

<!-- What the rule intentionally does NOT flag (false-negative trade-offs). This is the first thing a false-positive triage reads. -->

- {Construct or situation the rule stays silent on, and why}

## Known issues

<!-- Accepted limitations and non-obvious workarounds. Omit the section when there are none; never write "None". -->

- {Limitation, its consequence, and why it is accepted or how it is worked around}

## SDK facts

<!-- Verified SDK behaviours the rule depends on that are not derivable from the analyzer code. Omit when none. -->

- {Fact} (verified against SDK {version} in `../nav-sdk-source`)

## Test notes

<!-- Only when the tests are non-default: version-gated fixtures, ruleset injection for isEnabledByDefault:false, required fixture sets, fixtures that must not compile. Omit otherwise. -->

- {Note}

## Settings

<!-- Only when the rule is configurable via alcops.json. Omit otherwise. -->

| Setting | Default | Effect |
|---|---|---|
| `{Name}` | `{value}` | {what it changes} |

## CodeFix: {AnalyzerClassName}CodeFixProvider

<!-- Only when a CodeFix exists. Same admission test. -->

| Decision | Rationale |
|---|---|
| {Fix shape, FixAll behaviour, trivia handling, cases intentionally left unfixed} | {Why} |
