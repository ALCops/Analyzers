---
paths:
  - "src/ALCops.PlatformCop/**/UseValidateForFieldAssignment*"
---

# PC0037: UseValidateForFieldAssignment

## Purpose

Detects direct field assignments on non-temporary record variables and recommends using `Validate()` instead. Direct assignment bypasses OnValidate triggers and event subscribers, silently breaking business logic in an extensible platform.

## Design decisions

| Decision | Rationale |
|---|---|
| Flag all fields (including PK, system fields) | Community consensus: no exceptions, use pragma for justified cases |
| Exclude `temporary` record *variables* of **persisted** (`TableType = Normal`) tables only | `IRecordTypeSymbol.Temporary == true` reflects the `temporary` variable keyword only (per the SDK `RecordTypeSymbol` ctor / `Binder`), not the table's `TableType`. A real table used as a scratch buffer may carry OnValidate side effects the caller never signed up for. Discussion #259 (Christian Hovenbitzer: "temporary Record variables should be excluded") |
| Fire on `TableType = Normal` **and** `TableType = Temporary`; on `TableType = Temporary` the variable keyword is **ignored** (fires with or without `temporary`); skip the externally-backed types | Inherently-temporary tables stay in scope: whoever wrote an OnValidate on a `TableType = Temporary` table knew it was temporary, so that logic is designed to run (discussion #259). BaseApp w1-28 confirms it: 22 of 211 such tables have OnValidate triggers and ~30 of those 75 triggers have external effects (`Employee.Modify()`, events, `Confirm`). The keyword is redundant there and MS sprinkles it by table vintage (0–37% of declarations), so deciding by keyword would be a loophole; issue #422. Consistent with PC0027, which exempts `TableType = Temporary` from its "don't Validate temp records" warning. Externally-backed integration tables (CRM, CDS, ExternalSQL, Exchange, MicrosoftGraph) persist to and sync from an external source; their `OnValidate` runs only for AL-side changes, not for changes from that source, so suggesting `Validate()` falsely implies it validates external input (issue #369). Using an include-list `{Normal, Temporary}` also auto-excludes any future external table type. Unresolved base table → stay silent (no false positives) |
| Suppress same-field assignment to the current record inside its own validate trigger | Assigning a field to `Rec`/self inside that field's own `OnValidate`/`OnBeforeValidate`/`OnAfterValidate` is by design (default values, value transformation such as rounding); calling `Validate()` there is impossible/recursive. Issue #357 |
| Suppression is narrow: same field only, current record only | Cross-field cascades, `xRec`, and other same-table variables still fire (they are different records / different fields, so subscribers must run) |
| Do NOT exclude assignments after Init() | The Init+assign+Insert pattern should still use Validate where possible |
| No ChangeCompany handling | Use pragma for legacy ChangeCompany+write patterns |
| Register for CompoundAssignmentStatement | Guard with `!= default` for netstandard2.1 where the OperationKind doesn't exist |
| CodeFix only for simple `:=` assignments | Compound assignments (`+=`, `-=`) are more complex to rewrite (need binary expression expansion) |

## Architecture

- **Analyzer**: Registers for `OperationKind.AssignmentStatement` and `OperationKind.CompoundAssignmentStatement`
- **Detection**: Checks if `IAssignmentStatement.Target` is `IFieldAccess` with a non-temporary `IRecordTypeSymbol` instance
- **Table-type gate**: Resolves `recordType.BaseTable?.TableType` **first**. `Temporary` → always in scope (keyword ignored, issue #422). Otherwise the `recordType.Temporary` keyword exempts the variable, and only `Normal` remains in scope. The externally-backed types (CRM/CDS/ExternalSQL/Exchange/MicrosoftGraph) and an unresolved base table are skipped (issue #369). Do not replace this with `IRecordTypeSymbol.IsTemporary()` from Common — that OR merges the two cases this rule must keep apart. No `#if` guard needed — `BaseTable`, `TableType`, `Normal`, and `Temporary` exist on every TFM.
- **Own-validate suppression** (`IsAssignmentToOwnValidateField`): walks to the nearest `TriggerDeclarationSyntax`, requires its name to be `OnValidate`/`OnBeforeValidate`/`OnAfterValidate`, requires the assigned instance to be the current record, then compares the assigned field name (`IsSameName`) to the trigger's owner field
- **Current-record detection** (`IsCurrentRecordInstance`): true when the instance is a `this`/self reference — detected via `instance.Kind == EnumProvider.OperationKind.ThisReference` (guarded `!= default`), **not** the `IInstanceReferenceOperation` type — or when its symbol is named `Rec` (covers explicit `Rec.` and a page's implicit-with bare reference). `Rec`/`xRec` are reserved AL keywords, so the name is the only public discriminator between the current record and the `xRec` before-image. See the this/self note in `.claude/rules/analyzer-development.md`.
- **Owner field resolution** (`ResolveTriggerOwnerField`): the trigger symbol's `ContainingSymbol` is the owner — an `IFieldSymbol` (table field), an `IControlSymbol` (page control, resolved via `RelatedFieldSymbol`), or a change-modify symbol for `modify(...)` extensions whose modified base field/control is read via the internal `Target` property (`PropertyAccessor.GetPropertyIfExists`), then resolved recursively
- **Location**: Reports on `fieldAccess.Syntax.GetIdentifierNameSyntax()` (the field identifier token)
- **CodeFix**: Navigates from diagnostic span to parent `AssignmentStatementSyntax`, rewrites to `ExpressionStatement(InvocationExpression(MemberAccess(Rec, "Validate"), ArgumentList(FieldName, Value)))`. Reuses the original `assignment.SemicolonToken` (a missing token when absent) instead of fabricating one, so then-branch assignments directly before `else` stay compilable (issue #395)

## Known issues

- `IRecordTypeSymbol.Temporary` is keyword-only (SDK `RecordTypeSymbol` ctor; `Rec` in a table object is hard-coded non-temporary even for `TableType = Temporary`), so the TableType branch must be explicit. `TableType = Temporary` tables are extensible (`SemanticFacts.IsTableTypeExtensible`), so the "extensions may add OnBefore/OnAfterValidate" argument applies to them too.

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
- Issue #422 (TableType = Temporary + keyword): https://github.com/ALCops/Analyzers/issues/422
