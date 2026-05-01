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

### CalcFields detection

`VisitInvocationExpression` checks if the method name is `CalcFields` and if the instance variable is in the current set of loop variables (at any nesting level).

### CodeFix strategy

1. Find the `ExpressionStatementSyntax` containing the CalcFields invocation
2. Find the insertion target (FindSet statement before repeat, or the loop statement itself)
3. Remove the CalcFields statement from the tree
4. Insert `SetAutoCalcFields(fields)` before the insertion target
5. Arguments are passed through directly from CalcFields (unqualified field names)

## Test coverage

**HasDiagnostic (6 cases):** FindSetRepeatUntil, FindRepeatUntil, WhileLoop, ReportOnAfterGetRecord, MultipleCalcFields, NestedLoop.
**NoDiagnostic (4 cases):** DifferentVariable, CalcFieldsOutsideLoop, CrossMethodCall, SingleRecord.
**HasFix (2 cases):** FindSetRepeatUntil, MultipleFields.

## Known limitations

- Cross-method CalcFields calls (passed record variable) are not detected
- Multiple CalcFields in the same loop are reported individually (not merged by the analyzer)
- The CodeFix handles one CalcFields at a time; use Fix All for multiple occurrences
