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
| Suppress same-field assignment to the current record inside its own validate trigger | Assigning a field to `Rec`/self inside that field's own `OnValidate`/`OnBeforeValidate`/`OnAfterValidate` is by design (default values, value transformation such as rounding); calling `Validate()` there is impossible/recursive. Issue #357 |
| Suppression is narrow: same field only, current record only | Cross-field cascades, `xRec`, and other same-table variables still fire (they are different records / different fields, so subscribers must run) |
| Do NOT exclude assignments after Init() | The Init+assign+Insert pattern should still use Validate where possible |
| No ChangeCompany handling | Use pragma for legacy ChangeCompany+write patterns |
| Register for CompoundAssignmentStatement | Guard with `!= default` for netstandard2.1 where the OperationKind doesn't exist |
| CodeFix only for simple `:=` assignments | Compound assignments (`+=`, `-=`) are more complex to rewrite (need binary expression expansion) |

## Architecture

- **Analyzer**: Registers for `OperationKind.AssignmentStatement` and `OperationKind.CompoundAssignmentStatement`
- **Detection**: Checks if `IAssignmentStatement.Target` is `IFieldAccess` with a non-temporary `IRecordTypeSymbol` instance
- **Own-validate suppression** (`IsAssignmentToOwnValidateField`): walks to the nearest `TriggerDeclarationSyntax`, requires its name to be `OnValidate`/`OnBeforeValidate`/`OnAfterValidate`, requires the assigned instance to be the current record, then compares the assigned field name (`IsSameName`) to the trigger's owner field
- **Current-record detection** (`IsCurrentRecordInstance`): true when the instance is a `this`/self reference — detected via `instance.Kind == EnumProvider.OperationKind.ThisReference` (guarded `!= default`), **not** the `IInstanceReferenceOperation` type — or when its symbol is named `Rec` (covers explicit `Rec.` and a page's implicit-with bare reference). `Rec`/`xRec` are reserved AL keywords, so the name is the only public discriminator between the current record and the `xRec` before-image. See the this/self note in `analyzer-development.instructions.md`.
- **Owner field resolution** (`ResolveTriggerOwnerField`): the trigger symbol's `ContainingSymbol` is the owner — an `IFieldSymbol` (table field), an `IControlSymbol` (page control, resolved via `RelatedFieldSymbol`), or a change-modify symbol for `modify(...)` extensions whose modified base field/control is read via the internal `Target` property (`PropertyAccessor.GetPropertyIfExists`), then resolved recursively
- **Location**: Reports on `fieldAccess.Syntax.GetIdentifierNameSyntax()` (the field identifier token)
- **CodeFix**: Navigates from diagnostic span to parent `AssignmentStatementSyntax`, rewrites to `ExpressionStatement(InvocationExpression(MemberAccess(Rec, "Validate"), ArgumentList(FieldName, Value)))`

## Test coverage

**HasDiagnostic (7 cases):** SimpleAssignment, CompoundAssignment, AfterInit, PrimaryKeyField, OnValidateDifferentFieldOnRec, OnValidateXRecSameField, OnValidateOtherRecordSameField.
**NoDiagnostic (12 cases):** TemporaryVariable, ValidateCall, NonRecordVariable, InsideOnValidateTrigger, TableFieldOnValidateSameField, TableExtensionFieldOnBeforeValidateSameField, TableExtensionFieldOnAfterValidateSameField, PageControlOnValidateSameField, PageExtensionControlOnBeforeValidateSameField, PageExtensionControlOnAfterValidateSameField, OnValidateSameFieldThisReference, PageControlOnValidateSameFieldBareReference.
**HasFix (1 case):** SimpleAssignment.

## Known issues

- CompoundAssignmentStatement OperationKind does not exist in netstandard2.1 SDK. Guarded with `!= default` check.
- The CodeFix does not handle compound assignments (`+=`, `-=`) — only simple `:=` is auto-fixable.
- `this`/self detection uses the `OperationKind.ThisReference` enum (via `EnumProvider`, guarded `!= default`), **not** the `IInstanceReferenceOperation` type. That type is absent from the netstandard2.1 compile floor (AL 12.0.13), and referencing it would force an `#if !NETSTANDARD2_1` guard that silently drops `this.` suppression on the netstandard2.1 binary serving AL 14.0–15.2. The enum approach works on every TFM with no guard. See AC0032 / PR #353.
- `OnBeforeValidate`/`OnAfterValidate` are only valid on **modified** fields/controls (`modify(...)`), not on newly-added extension fields (AL0162). The table/page extension fixtures therefore use `modify("Unit Price")` on a base field.
- A bare table self field reference (`"Field" := ...`, no `Rec.`) binds to the table object type (not `IRecordTypeSymbol`), so it never fires today and needs no suppression. Page bare references use implicit-with → `Rec`, so they fire and are suppressed.
- `xRec` and other same-table record variables keep firing — they are different records.

## Related

- PC0012 (FlowFilterFieldAssignment): Same detection pattern (IFieldAccess on assignment target), different filter (FieldClass == FlowFilter)
- PC0027 (TemporaryRecordTriggerInvocation): Related concept (trigger execution on temporary records)
- Discussion: https://github.com/ALCops/Analyzers/discussions/259
