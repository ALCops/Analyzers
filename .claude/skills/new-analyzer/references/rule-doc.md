---
paths:
  - "src/ALCops.{Cop}/**/{AnalyzerClassName}*"
---

# {ID}: {AnalyzerClassName}

## Purpose

{One to three sentences: what the rule detects and why it matters to AL developers.}

**References:** {optional — Microsoft Learn links, GitHub issues/discussions, blog posts that motivated the rule}

## Design decisions

| Decision | Rationale |
|---|---|
| {What was chosen, including deliberate false-negative trade-offs} | {Why} |
| {Version gate / netstandard2.1 stub, if any} | {Which SDK API is missing and how the rule degrades} |

## Architecture

- Registers `{Register*Action}` on `{SyntaxKind / OperationKind / SymbolKind}`.
- {Walkers, helpers, and extensions used from `ALCops.Common`, with file names.}
- {Dispatch strategy, caching, report location.}

## Known issues

- {Only if present. Non-obvious workarounds, accepted limitations, deferred edge cases.}

## CodeFix: {AnalyzerClassName}CodeFixProvider

{Only if a CodeFix exists.}

| Decision | Rationale |
|---|---|
| {Fix shape, FixAll behavior, formatting/trivia handling} | {Why} |

## Roadmap

- {Only if present. Deferred phases or rejected-for-now extensions.}
