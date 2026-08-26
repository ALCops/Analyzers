---
name: fix-false-positive
description: Triage and fix a reported false positive or false negative in an ALCops rule — regression fixture first, minimal analyzer change, rule doc updated. Use when given a GitHub issue, a snippet of AL that is wrongly flagged (or wrongly not flagged), or asked to fix a rule's behaviour.
argument-hint: <issue number | URL | description of the wrong diagnostic>
---

# Fix a false positive / false negative

Input: `$ARGUMENTS`. If it is an issue number or URL, read it with `gh issue view <n> --comments` and extract the AL reproduction, BC/SDK version, and the diagnostic ID. Resolve the analyzer via `DiagnosticIds.cs` and read `.claude/rules/diagnostics/{id}-*.md` — the Design decisions and Known issues tables tell you whether the behaviour is a bug or a documented trade-off. If it is documented as intentional, say so and stop for a decision instead of changing behaviour.

## Steps

1. **Reproduce with a failing test first.** Add a fixture under `src/ALCops.{Cop}.Test/Rules/{RuleName}/` — `NoDiagnostic/{Case}.al` for a false positive, `HasDiagnostic/{Case}.al` for a false negative — with `[|...|]` markers, self-contained objects, and a `[TestCase("{Case}")]` entry. Run `dotnet test src/ALCops.{Cop}.Test/ --filter "FullyQualifiedName~{RuleName}"` and confirm it fails for the expected reason. Name the case after the scenario, not the issue number.
2. **Find the root cause** in the analyzer and the `ALCops.Common` helpers it uses. Check the decompiled SDK source when the cause is an SDK shape you did not expect (see `.claude/rules/analyzer-development.md` §NAV SDK Source Reference). Consider whether the same cause affects sibling rules sharing the helper.
3. **Fix minimally.** Prefer using binder/semantic-model information already available over new syntax heuristics; keep the diff small; respect `netstandard2.1` (`.claude/rules/netstandard21-compatibility.md`). Do not widen or narrow the rule beyond the reported case unless the rule doc's design decisions require it.
4. **Run the rule's full test set** and the cop's test project. Report real results; if something else breaks, explain why before adjusting fixtures.
5. **Document.** In `.claude/rules/diagnostics/{id}-*.md` add a Design-decision row (new intentional behaviour) or a Known-issues bullet (non-obvious workaround or accepted limitation). If the fix changed a shared helper, update the relevant `.claude/rules/*.md` too.
6. Commit as `fix({ID}): <what now behaves correctly>` on a `fix/{id}-<slug>` branch; reference the issue in the PR body (`Fixes #n`).
