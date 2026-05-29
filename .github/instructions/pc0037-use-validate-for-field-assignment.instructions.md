---
applyTo: 'src/ALCops.PlatformCop/**/UseValidateForFieldAssignment*'
---

# PC0037: UseValidateForFieldAssignment

## Purpose

Detects direct field assignments on non-temporary record variables and recommends using `Validate()` instead. Direct assignment bypasses OnValidate triggers and event subscribers, silently breaking business logic in an extensible platform.

## Diagnostic properties

| Property | Value |
|----------|-------|
| ID | PC0037 |
| Category | Design |
| Severity | Warning |
| Enabled | Yes |
| CodeFix | Yes |
| Message | `Use Validate() instead of direct field assignment on '{0}'.` |

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Flag all fields (including PK, system fields) | Community consensus: no exceptions, use pragma for justified cases |
| Exclude only `temporary` record variables | `IRecordTypeSymbol.Temporary == true` — temporary records don't persist and don't need subscriber execution |
| Do NOT exclude non-Normal table types | Christian Hovenbitzer: inherently temporary tables should still validate |
| Do NOT exclude assignments inside OnValidate triggers | Even cascading field assignments should use Validate to trigger subscribers on the target field |
| Do NOT exclude assignments after Init() | The Init+assign+Insert pattern should still use Validate where possible |
| No ChangeCompany handling | Use pragma for legacy ChangeCompany+write patterns |
| Register for CompoundAssignmentStatement | Guard with `!= default` for netstandard2.1 where the OperationKind doesn't exist |
| CodeFix only for simple `:=` assignments | Compound assignments (`+=`, `-=`) are more complex to rewrite (need binary expression expansion) |

## Architecture

- **Analyzer**: Registers for `OperationKind.AssignmentStatement` and `OperationKind.CompoundAssignmentStatement`
- **Detection**: Checks if `IAssignmentStatement.Target` is `IFieldAccess` with a non-temporary `IRecordTypeSymbol` instance
- **Location**: Reports on `fieldAccess.Syntax.GetIdentifierNameSyntax()` (the field identifier token)
- **CodeFix**: Navigates from diagnostic span to parent `AssignmentStatementSyntax`, rewrites to `ExpressionStatement(InvocationExpression(MemberAccess(Rec, "Validate"), ArgumentList(FieldName, Value)))`

## Test coverage

**HasDiagnostic (5 cases):** SimpleAssignment, CompoundAssignment, InsideOnValidateTrigger, AfterInit, PrimaryKeyField.
**NoDiagnostic (3 cases):** TemporaryVariable, ValidateCall, NonRecordVariable.
**HasFix (1 case):** SimpleAssignment.

## Known issues

- CompoundAssignmentStatement OperationKind does not exist in netstandard2.1 SDK. Guarded with `!= default` check.
- The CodeFix does not handle compound assignments (`+=`, `-=`) — only simple `:=` is auto-fixable.

## Related

- PC0012 (FlowFilterFieldAssignment): Same detection pattern (IFieldAccess on assignment target), different filter (FieldClass == FlowFilter)
- PC0027 (TemporaryRecordTriggerInvocation): Related concept (trigger execution on temporary records)
- Discussion: https://github.com/ALCops/Analyzers/discussions/259
