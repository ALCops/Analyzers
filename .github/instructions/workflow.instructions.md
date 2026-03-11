# ALCops Agent Workflow

Multi-agent pipeline for implementing new Roslyn analyzer diagnostics for Business Central AL code.

## Workflow Sequence

```
User Request ("I want a rule that...")
     │
     ▼
┌─────────────────────────────────────────────────┐
│              PHASE 1: PLANNING                  │
│                                                 │
│  @interview ──▶ @requirements-engineer          │
│                       │                         │
│                       ▼                         │
│              @solution-planner                  │
│                       │                         │
│                       ▼                         │
│              PLAN REVIEW LOOP (max 3)           │
│  @solution-planner ◀──▶ @requirements-engineer  │
└─────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────┐
│         PHASE 2: DEVELOPMENT (Iterative)        │
│                                                 │
│  @analyzer-developer ──▶ @code-reviewer         │
│         ▲                      │                │
│         │ ITERATE if           │                │
│         └──────── issues found │                │
└────────────────────────────────│────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────┐
│              PHASE 3: TESTING                   │
│                                                 │
│  @test-engineer ──▶ @test-reviewer              │
│        ▲                  │                     │
│        │ ITERATE if       │                     │
│        └──── gaps found   │                     │
│                           ▼                     │
│                     ✓ COMPLETE                  │
└─────────────────────────────────────────────────┘
                        │
                        ▼
              @docs-writer (optional)
```

## Agent Roster

### Phase 1: Planning

| Agent | Model | Purpose |
|-------|-------|---------|
| `@interview` | claude-sonnet-4.6 | Deep requirements gathering — asks clarifying questions about AL object types, edge cases, configurability |
| `@requirements-engineer` | claude-opus-4.6 | Rule analysis — determines cop, assigns ID, defines analysis strategy, produces formal requirements |
| `@solution-planner` | claude-opus-4.6 | Implementation planning — creates detailed plan with test cases, CodeFix approach, performance analysis |

### Phase 2: Development (Iterative)

| Agent | Model | Purpose |
|-------|-------|---------|
| `@analyzer-developer` | claude-opus-4.6 | Scaffold → analyzer → CodeFix → .resx → IDs → descriptors — full implementation |
| `@code-reviewer` | claude-opus-4.6 | Reviews code for correctness, patterns, performance, edge cases — sends back with feedback if issues |

### Phase 3: Testing

| Agent | Model | Purpose |
|-------|-------|---------|
| `@test-engineer` | claude-sonnet-4.6 | Creates test class + .al test files, runs TDD red/green cycle |
| `@test-reviewer` | claude-sonnet-4.6 | Reviews test quality and coverage, runs full regression suite |

### Support (On-Demand)

| Agent | Model | Purpose |
|-------|-------|---------|
| `@docs-writer` | claude-sonnet-4.6 | Generates documentation draft for the new diagnostic |

## Tool Access Matrix

| Agent | read | edit | search | execute |
|-------|------|------|--------|---------|
| interview | ✓ | | ✓ | |
| requirements-engineer | ✓ | | ✓ | |
| solution-planner | ✓ | | ✓ | |
| analyzer-developer | ✓ | ✓ | ✓ | ✓ |
| code-reviewer | ✓ | | ✓ | ✓ |
| test-engineer | ✓ | ✓ | ✓ | ✓ |
| test-reviewer | ✓ | | ✓ | ✓ |
| docs-writer | ✓ | ✓ | ✓ | |

> **Tool aliases:** `execute` maps to PowerShell on Windows / bash on Linux. `search` covers both file pattern matching (glob) and content search (grep).

## Iteration Rules

### Plan Review Loop (Phase 1)
- **Max iterations:** 3
- **Trigger:** requirements-engineer finds issues in the solution plan
- **Exit:** Both agents agree the plan is complete, OR 3 iterations reached (proceed with best version, note unresolved concerns)

### Development Loop (Phase 2)
- **Max iterations:** 3
- **Trigger:** code-reviewer finds correctness, pattern, or performance issues
- **Exit:** Code review passes, OR 3 iterations reached

### Testing Loop (Phase 3)
- **Max iterations:** 3
- **Trigger:** test-reviewer finds coverage gaps, missing edge cases, or test quality issues
- **Exit:** Test review passes and full regression suite is green, OR 3 iterations reached

## Output Artifacts

Each phase produces artifacts in `.dev/` (gitignored, local-only):

| Phase | File | Content |
|-------|------|---------|
| Interview | `.dev/00-interview.md` | Requirements gathered from user Q&A |
| Requirements | `.dev/01-requirements.md` | Formal rule specification (cop, ID, strategy, category, severity) |
| Planning | `.dev/02-solution-plan.md` | Implementation plan with test cases, CodeFix approach, performance notes |
| Development | Source files | Analyzer, CodeFix, .resx, IDs, descriptors |
| Testing | Test files | Test class, .al test fixtures |
| Documentation | `docs/draft-[ID].md` | Documentation draft for docs repo |

## Shared Instructions

Agent prompts reference these instruction files for patterns and project metadata:

- `.github/instructions/code-patterns.instructions.md` — Templates for analyzers, CodeFixes, tests, .resx, build commands
- `.github/instructions/project-reference.instructions.md` — Project structure, ID ranges, categories, helpers, settings
