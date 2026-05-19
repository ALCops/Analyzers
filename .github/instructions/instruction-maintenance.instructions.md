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
| Rule-specific | `{id}-{rule-name}.instructions.md` | `pc0030-use-partial-records-on-read.instructions.md` |
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

Include: purpose, compact diagnostic properties, design decisions table, architecture, known issues, test coverage (summary counts), CodeFix details (if applicable), roadmap/phase 2 items.

#### Test coverage format

Use summary count lines with comma-separated test case names. Do NOT use per-case descriptions, bullet lists, or detailed tables. The format must be:

```
## Test coverage

**HasDiagnostic (N cases):** CaseName1, CaseName2, CaseName3.
**NoDiagnostic (N cases):** CaseName1, CaseName2, CaseName3.
**HasFix (N cases):** CaseName1, CaseName2.
```

This keeps test names searchable while minimizing token consumption. See PR #220 for rationale.

### Development guide instruction files

Include: project structure, templates, step-by-step guides, API reference, common pitfalls.

## Existing instruction files

| File | Scope | Purpose |
|---|---|---|
| `project-overview` | `'**'` | Solution structure, dependencies, build config |
| `instruction-maintenance` | `'**'` | This file: how to maintain instruction files |
| `analyzer-development` | `'src/ALCops.*/Analyzers/**'` | How to write analyzers |
| `codefix-development` | `'src/ALCops.*/CodeFixes/**'` | How to write code fixes |
| `testing` | `'src/*.Test/**'` | Test conventions and patterns |
| `common-library` | `'src/ALCops.Common/**'` | Shared library guidelines |
| `record-method-classification` | `'src/ALCops.Common/RecordMethodClassification.cs'` | Record method categorization utility |
| `netstandard21-compatibility` | `'src/ALCops.*/**'` | netstandard2.1 compat patterns |
| `settings-schema` | `ALCopsSettings.cs` | Settings file schema |
| `cicd` | `'.github/**'` | CI/CD workflows |
| `release-strategy` | `'.github/**'` | Release channels, versioning, cleanup |
| `get-bc-devtools` | `'.github/actions/get-bc-devtools/**'` | BC DevTools discovery and caching |
| `ac0013-field-groups-required` | rule-scoped | AC0013 rule |
| `ac0026-allow-in-customizations-for-omitted-fields` | rule-scoped | AC0026 rule |
| `ac0031-table-data-access-requires-permissions` | rule-scoped | AC0031 rule |
| `ac0032-table-data-access-unused-permissions` | rule-scoped | AC0032 rule |
| `sdk-analyzer-infrastructure` | `'src/ALCops.*/Analyzers/**'` | NAV SDK internals: callback ordering, incremental compilation, GetOperation perf |
| `fc0004-permission-declaration-order` | rule-scoped | FC0004 rule |
| `lc0086-page-style-string-literal` | rule-scoped | LC0086 rule |
| `lc0091-translatable-text-should-be-translated` | rule-scoped | LC0091 rule |
| `lc0092-naming-pattern` | rule-scoped | LC0092 rule |
| `lc0096-unnecessary-record-parameter` | rule-scoped | LC0096 rule |
| `pc0029-use-sequential-guid` | rule-scoped | PC0029 rule |
| `pc0030-use-partial-records-on-read` | rule-scoped | PC0030 rule |
| `pc0031-partial-records-cause-jit-load` | rule-scoped | PC0031 rule |
| `pc0032-report-layout-property-length` | rule-scoped | PC0032 rule |
| `pc0033-duplicate-odata-entity-name` | rule-scoped | PC0033 rule |
| `pc0034-placeholder-argument-count-mismatch` | rule-scoped | PC0034 rule |
| `pc0035-use-set-auto-calc-fields-for-loops` | rule-scoped | PC0035 rule |
| `lc0095-parameter-not-referenced` | rule-scoped | LC0095 rule |
| `ac0006-run-page-implement-page-management` | rule-scoped | AC0006 rule |
