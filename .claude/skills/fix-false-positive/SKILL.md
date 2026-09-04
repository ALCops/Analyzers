---
name: fix-false-positive
description: Triage and fix a reported false positive or false negative in an ALCops rule — regression fixture first, minimal analyzer change, rule doc updated. Use when given a GitHub issue, a snippet of AL that is wrongly flagged (or wrongly not flagged), or asked to fix a rule's behaviour.
argument-hint: <issue number | URL | description of the wrong diagnostic>
---

# Fix a false positive / false negative

**Not for:** new behaviour or a new diagnostic → `/new-analyzer`; a rule that lacks a fix → `/new-codefix`.

Input: `$ARGUMENTS`. For an issue number/URL, `gh issue view <n> --comments` and extract the AL reproduction, BC/SDK version, and diagnostic ID. Resolve the analyzer via `DiagnosticIds.cs` and read `.claude/rules/diagnostics/{id}-*.md` first: if the behaviour is listed as an intentional Design decision, say so and **stop for a decision** instead of changing it. Before hunting, scan `references/regression-catalog.md` — most reports are a recurrence of a known cause.

## Steps

1. **Failing fixture first.** `NoDiagnostic/{Case}.al` for a false positive, `HasDiagnostic/{Case}.al` for a false negative — `[|...|]` markers, self-contained objects, `[TestCase("{Case}")]` named after the scenario (not the issue number). Run `dotnet test src/ALCops.{Cop}.Test/ --filter "FullyQualifiedName~{RuleName}"` and confirm it fails for the expected reason.
2. **Root cause.** Read the analyzer and the `ALCops.Common` helpers it uses; check the decompiled SDK source when an SDK shape surprised you. Ask whether sibling rules sharing the helper have the same bug (fix or open an issue).
3. **Minimal fix.** Prefer binder/semantic information already available over new syntax heuristics; keep the diff small; respect `netstandard2.1`. Do not widen or narrow the rule beyond the reported case unless its Design decisions require it.
4. **Run** the rule's full test set, then the cop's test project. Report real results; explain any other breakage before touching fixtures.
5. **Document.** Add a Design-decision row (a new choice), a Deliberate non-reports bullet (a case the rule now intentionally skips), or a Known-issues bullet (workaround or accepted limitation) to the rule doc; update the relevant `.claude/rules/*.md` if a shared helper changed; add the cause to `references/regression-catalog.md` if it is new.
6. Commit `fix({ID}): <what now behaves correctly>` on `fix/{id}-<slug>`; PR body contains `Fixes #n`.

## Common Mistakes

| Mistake | Fix |
|---|---|
| Editing the analyzer before a failing fixture exists | Step 1 first; the fixture is the proof and the regression guard. |
| Fixture named `Issue438.al` | Name the scenario (`InternalInterfaceProcedure.al`); the issue goes in the PR body. |
| "Fixing" by excluding the whole construct (widening the false-negative surface) | Fix the cause; if a bail-out is genuinely required (e.g. `RecordRef` in AC0032, #448), record it as a Design decision. |
| Changing a documented Design decision silently | Stop and ask; then update the row if the decision changes. |
| Only the one temporary-table form from the report is handled | Cover all three forms (`TableType = Temporary`, `Record X temporary`, temporary page source). |
| Markdown-only issue link in the PR body | `Fixes #n` so GitHub links and auto-closes. |
| Sibling rules sharing the helper left unchecked | Grep for the helper; fix or file an issue (#449 is the precedent). |
| Rule doc not updated | Step 5 is part of "done". |
