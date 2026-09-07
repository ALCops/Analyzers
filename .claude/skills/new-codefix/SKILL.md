---
name: new-codefix
description: Add a CodeFixProvider for an existing ALCops diagnostic, including HasFix fixtures, FixAll coverage, and the rule doc's CodeFix section. Use when asked to add a code fix / quick fix / code action for a rule.
argument-hint: <ID or RuleName>   e.g. PC0035
---

# New CodeFix

**Not for:** a new diagnostic → `/new-analyzer`; an existing CodeFix that produces wrong output → `/fix-false-positive` (same workflow: failing `HasFix` case first).

Argument: `$ARGUMENTS` → resolve to `{Cop}`, `{RuleName}`, the analyzer file and `.claude/rules/diagnostics/{id}-*.md` via `DiagnosticIds.cs`. The analyzer must already exist; a CodeFix never introduces a diagnostic. `.claude/rules/codefix-development.md` and `testing.md` load when you open `CodeFixes/` and `*.Test/` files.

## Design, then confirm (gate)

Work out the fix, then **stop and confirm before editing any file**:

| Question | Notes |
|---|---|
| Which node does the diagnostic span point at, and what is the transformation? | add/remove/replace property, replace expression, text edit — name the parent node you navigate to. |
| Which reported cases are intentionally **not** fixed? | e.g. parameters owning `#if` trivia (LC0095), cases needing call-site rewrites. |
| FixAll: `BatchFixer` or custom `FixAllProvider`? | Custom when several diagnostics edit a shared ancestor node (`ParameterListSyntax`, property lists). |
| Does the fix need analyzer data via `Diagnostic.Properties` (`CodeFixProperties`)? | If yes, the analyzer and its tests change in the same PR. |
| Does the cop already reference `…CodeAnalysis.Workspaces.dll` and `System.Composition.AttributedModel.dll`? | `DocumentationCop` and `TestAutomationCop` currently have no `CodeFixes/`; adding one means adding these references to the `.csproj`. |
| Which nav-sdk-docs pages back the fix shape? | `/nav-sdk-docs:sdk-lookup` on the `SyntaxEditor`, `CodeAction` or `FixAllProvider` member you intend to use; the `docs/60-code-fixes/` pages name the pitfalls (`FindNode` ties, trivia, `BatchFixer` merge semantics). |

## Steps

1. **Resx:** add `{RuleName}CodeAction` (the fix title) to `ALCops.{Cop}Analyzers.resx`.
2. **Provider:** create `src/ALCops.{Cop}/CodeFixes/{RuleName}CodeFixProvider.cs` from `references/codefix-template.md`; `FixableDiagnosticIds` from `DiagnosticDescriptors.{RuleName}.Id`; preserve trivia; compare AL names via `SemanticFacts`; use `SyntaxFactory` per the reference section in `codefix-development.md`.
3. **Tests:** `HasFix/{Case}/current.al` + `expected.al` and the `HasFix` method from `references/hasfix-tests.md`; add `HasFixAll` with ≥2 markers on sibling nodes when the answer to the FixAll question was "custom".
4. **Run:** `dotnet build ALCops.sln`; `dotnet test src/ALCops.{Cop}.Test/ --filter "FullyQualifiedName~{RuleName}"`. Report real output. Then `/code-review` on the branch (`REVIEW.md` carries the code-fix checklist); fix or justify every correctness finding.
5. **Document:** append `## CodeFix: {RuleName}CodeFixProvider` with a Decision | Rationale table (fix shape, FixAll choice, trivia handling, intentionally unfixed cases) to the rule doc.
6. Commit `feat({ID}): add CodeFix …` on a `feat/` branch.

## Common Mistakes

| Mistake | Fix |
|---|---|
| `RoslynFixtureFactory.Create<T>` with the analyzer type in `HasFix` | `T` is the provider; pass the analyzer via `AdditionalAnalyzers = [_analyzer]`. |
| `TestCodeFix(..., DiagnosticIds.X)` | `TestCodeFix` takes the `DiagnosticDescriptor`; `TestFixAll` takes the ID string. |
| `expected.al` still contains `[|...|]` markers | Only `current.al` has markers. |
| `BatchFixer` when several fixes edit one ancestor node | Custom `FixAllProvider` + one-pass `RemoveNodes`/rewrite (see LC0095 in `codefix-development.md`). |
| `equivalenceKey` in `TestFixAll` differs from the provider's key | Must match exactly, otherwise FixAll silently short-circuits and the test passes for the wrong reason. |
| Fabricating tokens the source never had, e.g. a `;` before `else` (#395 PC0037) | Build the replacement from the existing nodes/tokens; assert with an `expected.al` that has the exact original formatting. |
| Dropping a qualified receiver (`Rec.`, `Customer.`) when rewriting an invocation (#441 PC0035) | Rewrite only the member/arguments; keep the receiver expression and its trivia. |
| Rewriting a node that may be missing (unblocked `then` branch, #398 PC0035) | Guard every navigation step; return the unchanged document instead of throwing. |
| Comparing AL identifiers with `StringComparison.OrdinalIgnoreCase` | `SemanticFacts` name comparison. |
| Forgetting the rule doc's CodeFix section | Step 5 is part of "done". |
