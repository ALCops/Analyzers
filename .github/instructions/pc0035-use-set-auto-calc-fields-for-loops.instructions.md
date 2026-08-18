---
applyTo: 'src/ALCops.PlatformCop/**/UseSetAutoCalcFieldsForLoops*'
---

# PC0035: UseSetAutoCalcFieldsForLoops

## Purpose

Detects `CalcFields` calls inside loop bodies and recommends using `SetAutoCalcFields` before the loop instead. Each `CalcFields` inside a loop generates a separate SQL query per FlowField per iteration, while `SetAutoCalcFields` bundles FlowField calculation into the main SELECT query.

**References:**
- [Discussion #74](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/74)
- [MS Docs: Record.SetAutoCalcFields](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-setautocalcfields-method)
- [CalcFields vs SetAutoCalcFields](https://www.kauffmann.nl/2014/04/04/calcfields-vs-setautocalcfields/)

## Diagnostic properties

**PC0035** · Category: Performance · Severity: Warning · Enabled: true
Message: `Use '{0}.SetAutoCalcFields()' before the loop instead of '{0}.CalcFields()' inside the loop to avoid repeated FlowField calculations.`
Version gate: None (SetAutoCalcFields available since runtime 1.0)

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Loop types | FindSet/Find + repeat-until, while-do, report OnAfterGetRecord | All patterns that iterate over records |
| Variable matching | Only flag CalcFields on the variable driving the loop | Avoids false positives (Example #2 from spec) |
| Temporary records | Suppress (both `temporary` keyword and `TableType = Temporary`) | SetAutoCalcFields rewrites the SQL SELECT; temporary records are in-memory and never issue it, so the suggestion is a no-op. Detected via the `IRecordTypeSymbol.IsTemporary()` extension on `invocation.Instance.Type`. See issue #364 |
| Conditional paths | Skip entirely (if/case) | Cannot guarantee conditional CalcFields always executes; avoids false positives |
| Cross-method tracking | Out of scope v1 | Complex, may lack source code for dependencies |
| RecordRef | N/A | RecordRef does not have a CalcFields method in the SDK |
| ForEach loop | N/A | AL foreach only works with List/Array, not Record |
| SetAutoCalcFields suppression | No | Always flag CalcFields in loop even if SetAutoCalcFields exists |
| Severity | Warning | Stronger than Info because the perf impact in loops is significant |
| CodeFix | Yes | Insert SetAutoCalcFields before loop, remove CalcFields from body |

## Architecture

### Registration strategy

Uses `RegisterCodeBlockAction` to analyze entire method/trigger bodies. A custom `CalcFieldsInLoopWalker` (extending `OperationWalker`) walks the IOperation tree.

### Loop variable identification

- **repeat-until**: Extract variable name from `Next()` call in the until-condition
- **while-do**: Extract variable name from `FindSet()`/`Find()` in the while-condition
- **Report OnAfterGetRecord**: The DataItem name is the implicit loop variable

### Stack-based tracking

A `Stack<ImmutableHashSet<string>>` tracks loop variables at each nesting level. When entering a loop, the set of active loop variables is pushed. When exiting, it's popped. This correctly handles nested loops.

### Conditional path skipping

`VisitIfStatement` and `VisitCaseStatement` are overridden to increment a `_conditionalDepth` counter when inside a loop. CalcFields is only flagged when `_conditionalDepth == 0`. When entering a new loop (`PushLoop`), `_conditionalDepth` is saved and reset to 0, so CalcFields inside a nested loop (even one inside a conditional branch) is correctly flagged as unconditional relative to that inner loop. On `PopLoop`, the saved depth is restored.

This follows the same `_branchDepth` pattern used in `PartialRecordOperations` (PC0030/PC0031), adapted with save/restore semantics for loop nesting.

### CalcFields detection

`VisitInvocationExpression` checks: `IsInLoop() && _conditionalDepth == 0 && IsCalcFieldsCall(...)` and verifies the instance variable is in the current set of loop variables (at any nesting level). It then calls `IsTemporaryRecord(...)`, which resolves `invocation.Instance?.Type as IRecordTypeSymbol` and delegates to the `IRecordTypeSymbol.IsTemporary()` extension (`ALCops.Common.Extensions`). Temporary records (either the `temporary` keyword or a `TableType = Temporary` backing table) are skipped, because `SetAutoCalcFields` is a no-op on in-memory records.

### CodeFix strategy

1. Find the `ExpressionStatementSyntax` containing the CalcFields invocation
2. Find the insertion target (FindSet statement before repeat, or the loop statement itself)
3. Remove the CalcFields statement from the tree
4. Insert `SetAutoCalcFields(fields)` before the insertion target
5. Arguments are passed through directly from CalcFields (unqualified field names)

The receiver expression is reused verbatim from the CalcFields member access (trivia stripped), so qualified receivers like `this.Job` are preserved. Rebuilding it via `SyntaxFactory.IdentifierName(expression.ToString())` produced the quoted identifier `"this.Job"` (issue #428). An `SyntaxFactory.ElasticMarker` leading trivia is attached to the reused node: the SDK's `CodeAction` post-formats only elastic-annotated spans, and source nodes (unlike factory-created tokens) carry no elastic trivia, so without it the inserted statement loses its indentation.

The insertion target must be an element of a statement list (`BlockSyntax` or `RepeatStatementSyntax` body). When the target is a single-statement branch (e.g. the `if X.FindSet() then repeat...` is itself an unblocked then-branch of an outer `if`), `InsertNodesBefore` would throw `InvalidOperationException` in the SDK's `SyntaxReplacer`. The `InsertableOrNull` guard returns null in that case, so no CodeFix is offered (issue #398); the diagnostic still appears.

## Test coverage

**HasDiagnostic (8 cases):** FindSetRepeatUntil, FindRepeatUntil, WhileLoop, ReportOnAfterGetRecord, MultipleCalcFields, NestedLoop, NestedLoopInConditional, ThisQualifiedGlobalVariable.
**NoDiagnostic (10 cases):** DifferentVariable, CalcFieldsOutsideLoop, CrossMethodCall, SingleRecord, CalcFieldsInIfBlock, CalcFieldsInCaseBlock, CalcFieldsInIfElseBlock, TemporaryVariable, TemporaryTableType, ReportTemporaryTableType.
**HasFix (5 cases):** FindSetRepeatUntil, MultipleFields, IfFindSetRepeatUntil, IfFindSetBeginRepeatUntil, ThisQualifiedGlobalVariable.
**NoFix (2 cases):** UnblockedThenBranch, UnblockedThenBranchBeforeElse.

## Known limitations

- Cross-method CalcFields calls (passed record variable) are not detected
- Multiple CalcFields in the same loop are reported individually (not merged by the analyzer)
- The CodeFix handles one CalcFields at a time; use Fix All for multiple occurrences
- CalcFields inside conditional branches (if/case) within loops are intentionally not flagged, even if all branches call CalcFields (accepted false negative for zero false positives)
- No CodeFix is offered when the insertion target is not in a statement list (unblocked then-branch scenario, issue #398); wrapping the branch in begin..end was considered and rejected as over-engineering (pattern does not occur in the BaseApp)
