---
name: new-codefix
description: Add a CodeFixProvider for an existing ALCops diagnostic, including HasFix fixtures, FixAll coverage, and the rule doc's CodeFix section. Use when asked to add a code fix / quick fix / code action for a rule.
argument-hint: <ID or RuleName>   e.g. PC0035
---

# New CodeFix

Argument: `$ARGUMENTS` → the diagnostic ID or rule name. Resolve to `{Cop}`, `{RuleName}`, and the analyzer file via `DiagnosticIds.cs`. The analyzer must already exist; a CodeFix never introduces a diagnostic.

Read `.claude/rules/codefix-development.md` and `.claude/rules/testing.md` (auto-loaded under `CodeFixes/` and `*.Test/`), plus the rule's own doc `.claude/rules/diagnostics/{id}-*.md`.

## Steps

1. **Design the fix.** Which node does the diagnostic span point at, what is the transformation (add/remove/replace property, replace expression, text edit), and is it safe for FixAll? Decide whether the analyzer must pass data through `Diagnostic.Properties` (`CodeFixProperties` contract) — if so, update the analyzer and its tests in the same change.
2. **Check the project references** `Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces.dll` and `System.Composition.AttributedModel.dll` (`DocumentationCop` and `TestAutomationCop` currently have no `CodeFixes/`; adding one means adding these references to the `.csproj`).
3. **Add the CodeAction title** to `ALCops.{Cop}Analyzers.resx` as `{RuleName}CodeAction`.
4. **Create** `src/ALCops.{Cop}/CodeFixes/{RuleName}CodeFixProvider.cs` following the standard structure in `codefix-development.md` (`[CodeFixProvider]`, `FixableDiagnosticIds` from `DiagnosticIds`, `RegisterCodeFixesAsync`, `GetFixAllProvider`). Use `SyntaxFactory` per the reference section; preserve trivia; compare AL names case-insensitively as documented.
5. **Tests.** Under `src/ALCops.{Cop}.Test/Rules/{RuleName}/HasFix/{Case}/` add `current.al` (with `[|...|]` marker) and `expected.al`; add the `HasFix` test method (`RoslynFixtureFactory.Create<{RuleName}CodeFixProvider>` with `AdditionalAnalyzers = [_analyzer]`, `fixture.TestCodeFix(current, expected, DiagnosticDescriptors.{RuleName})`). Add a FixAll test when multiple occurrences per document are realistic.
6. **Run:** `dotnet build ALCops.sln` and `dotnet test src/ALCops.{Cop}.Test/ --filter "FullyQualifiedName~{RuleName}"`. Report real output.
7. **Document.** Append a `## CodeFix: {RuleName}CodeFixProvider` section with a Decision | Rationale table to `.claude/rules/diagnostics/{id}-*.md` (fix shape, FixAll behaviour, trivia/formatting choices, cases intentionally not fixed).
8. Commit as `feat({ID}): add CodeFix …` on a `feat/` branch.
