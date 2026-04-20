---
applyTo: '**'
---

# Maintaining Instruction Files

This project uses `.github/instructions/*.instructions.md` files to give AI agents context about the codebase, conventions, and design decisions. These files must stay in sync with the code.

## When planning work, always consider instruction file maintenance

Every plan should include a step to create or update instruction files when the work involves any of the following:

| Trigger | Action |
|---|---|
| New analyzer rule | Create a rule-specific instruction file |
| New CodeFix for an existing rule | Update the rule's instruction file with CodeFix details |
| New project area or shared component | Create an area instruction file |
| Design decision added or changed | Update the relevant instruction file's design decisions table |
| New coding pattern established | Update the relevant development guide |
| Refactor that changes architecture | Update all affected instruction files |
| Test coverage changes (new categories, edge cases) | Update the test coverage section of the rule's instruction file |
| Bug fix with a non-obvious workaround | Document in the rule's "Known issues" section |

If none of these apply, explicitly note "No instruction file changes needed" in the plan.

## File naming conventions

| Type | Pattern | Example |
|---|---|---|
| Rule-specific | `{id}-{rule-name}.instructions.md` | `lc0095-use-partial-records-on-read.instructions.md` |
| Area-specific | `{area-name}.instructions.md` | `common-library.instructions.md` |
| Development guide | `{topic}-development.instructions.md` | `analyzer-development.instructions.md` |
| Process/CI | `{topic}.instructions.md` | `cicd.instructions.md` |

## applyTo scoping

Choose the narrowest scope that covers the relevant files:

| Scope | Pattern | Use when |
|---|---|---|
| Single rule | `'src/ALCops.{Cop}/**/{RuleName}*'` | Rule-specific context (analyzer + codefix + tests) |
| All analyzers | `'src/ALCops.*/Analyzers/**'` | Analyzer development conventions |
| All codefixes | `'src/ALCops.*/CodeFixes/**'` | CodeFix development conventions |
| All tests | `'src/*.Test/**'` | Testing conventions |
| Shared library | `'src/ALCops.Common/**'` | Common library guidelines |
| CI/CD | `'.github/**'` | Build, test, release workflows |
| Global | `'**'` | Project-wide conventions (use sparingly) |

## Content guidelines

### Rule-specific instruction files

These are the most common type. Include:

1. **Purpose**: What the rule checks and why it matters. Include references (GitHub discussions, MS Docs).
2. **Diagnostic properties**: ID, category, severity, message format, version gate.
3. **Design decisions table**: Document every non-obvious choice with Decision, Choice, and Rationale columns. This is the most valuable section for future maintainers.
4. **Architecture**: Registration strategy, analysis flow, key implementation details.
5. **Known issues and workarounds**: SDK bugs, edge cases, defensive coding patterns.
6. **Method/symbol classifications**: Tables of methods grouped by behavior (e.g., read methods, write methods, suppression methods).
7. **Relationship to other rules**: How this rule interacts with or complements other diagnostics.
8. **Test coverage**: Table of all test cases with scenario descriptions, organized by HasDiagnostic/NoDiagnostic/HasFix.
9. **Phase 2 / roadmap**: Planned but unimplemented features, to prevent agents from re-discovering the same ideas.
10. **CodeFix details** (if applicable): Design decisions, architecture, field detection strategy.

### Development guide instruction files

These cover conventions for a category of files. Include:

1. **Project structure**: Directory layout, file naming.
2. **Templates**: Canonical code patterns that new files should follow.
3. **Step-by-step guides**: How to add a new analyzer, codefix, test, etc.
4. **API reference**: Key SDK types, methods, and their usage patterns.
5. **Common pitfalls**: Mistakes to avoid, with explanations of why.

## Existing instruction files

| File | Scope | Purpose |
|---|---|---|
| `project-overview.instructions.md` | `'**'` | Solution structure, dependencies, build config |
| `analyzer-development.instructions.md` | `'src/ALCops.*/Analyzers/**'` | How to write analyzers |
| `codefix-development.instructions.md` | `'src/ALCops.*/CodeFixes/**'` | How to write code fixes |
| `testing.instructions.md` | `'src/*.Test/**'` | Test conventions and patterns |
| `common-library.instructions.md` | `'src/ALCops.Common/**'` | Shared library guidelines |
| `cicd.instructions.md` | `'.github/**'` | CI/CD workflows and scripts |
| `lc0095-use-partial-records-on-read.instructions.md` | `'src/ALCops.LinterCop/**/UsePartialRecordsOnRead*'` | LC0095 rule details |
| `lc0096-unnecessary-record-parameter.instructions.md` | `'src/ALCops.LinterCop/**/UnnecessaryRecordParameterInMethodCall*'` | LC0096 rule details |
| `lc0092-naming-pattern.instructions.md` | `'src/ALCops.LinterCop/**/NamingPattern*'` | LC0092 rule details |
| `lc0091-translatable-text-should-be-translated.instructions.md` | `'src/ALCops.LinterCop/**/TranslatableTextShouldBeTranslated*'` | LC0091 rule details |
