---
applyTo: 'src/ALCops.LinterCop/**/UsePartialRecordsOnRead*'
---

# LC0095: UsePartialRecordsOnRead

## Purpose

Recommends using `SetLoadFields` (or `AddLoadFields`/`SetBaseLoadFields`) before read operations on local record variables. Without partial records, the runtime loads ALL normal fields including those from table extensions, causing unnecessary SQL joins and 2-9x slower data access.

**References:**
- [Discussion #155](https://github.com/ALCops/Analyzers/discussions/155)
- [Community discussion](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/218)
- [MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records)
- [MS Docs: Record.SetLoadFields](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-setloadfields-method)

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `LC0095` |
| Category | Performance |
| Severity | Info |
| Enabled by default | true |
| MessageFormat | `Use SetLoadFields before '{0}.{1}()' to improve performance by loading only the fields that are needed.` |
| Version gate | `Spring2021OrGreater` (SetLoadFields introduced in runtime 6.0, BC17) |

## Design decisions

These decisions were made during the initial design and should be preserved unless explicitly revisited:

| Decision | Choice | Rationale |
|---|---|---|
| Scope | Phase 1: local variables only | Global variables require cross-method analysis; deferred to Phase 2 |
| Function pass | Suppress on ANY function/event pass (including built-in methods like PAGE.Run) | Conservative: avoids false positives when the callee might access any field |
| Temporary tables | Skip | No SQL Server backing, no table extension join benefit. Matches AA0242 behavior |
| Severity | Info | Performance suggestion, not a correctness issue |
| Field access required | No (report on read operation alone) | User preference; may generate noise on patterns like `if Rec.Get(Key) then` |
| RecordRef | Included | Skip temp/table-type checks (not statically determinable for RecordRef) |
| Record table type | Normal only | Skip CDS, Exchange, temporary, and other non-SQL table types |

## Architecture

### Registration strategy

Uses `RegisterCodeBlockAction` (not `RegisterOperationAction`) to analyze entire method/trigger bodies in a single pass. This avoids redundant walks when multiple read operations exist on the same variable.

### Analysis flow

1. Extract local variables of type `Record` (non-temp, Normal table) or `RecordRef` from `IMethodSymbol.LocalVariables`
2. Get `IOperation` tree from the code block via `SemanticModel.GetOperation(body)`
3. Walk with `SetLoadFieldsWalker` (extends `OperationWalker`), tracking per-variable:
   - `ReadLocations`: Get, Find, FindFirst, FindLast, FindSet calls
   - `HasLoadFieldsCall`: SetLoadFields, AddLoadFields, SetBaseLoadFields present
   - `HasWriteOp`: Insert, Modify, ModifyAll, Delete, DeleteAll, Rename, TransferFields, Init, Copy
   - `PassedToFunction`: Variable appears as argument to ANY invocation, or a non-built-in method is called ON the variable
4. Report at each read location where none of the suppression conditions are met

### Key implementation detail: argument checking

The walker checks ALL invocations (both built-in and user-defined) for tracked variables in arguments. This is necessary because built-in methods like `PAGE.Run(PageId, Record)` pass the record to a page that will access its fields. The `GetVariableNameFromArgument` method only matches direct variable identifiers (e.g., `Item` in `PAGE.Run(PAGE::"Item Card", Item)`), not field access expressions (e.g., `MyTable."No."` in `SetRange`), so this doesn't cause false positives on record method arguments.

## Known issues and workarounds

### BoundObjectAccess InvalidCastException

The SDK's `OperationExtensions.GetSymbol()` throws `InvalidCastException` when the operation instance is a `BoundObjectAccess`. This type reports `Kind = FieldAccess` but doesn't implement `IFieldAccess`, so the SDK's internal cast fails. The analyzer uses a `TryGetSymbol()` wrapper that catches `InvalidCastException` and returns null.

This is an SDK bug. If a future SDK version fixes it, the try-catch becomes a no-op (harmless to keep).

## Method classification

### Read methods (trigger diagnostic)
`Get`, `Find`, `FindFirst`, `FindLast`, `FindSet`

### Load fields methods (suppress diagnostic)
`SetLoadFields`, `AddLoadFields`, `SetBaseLoadFields`

### Write/mutation methods (suppress entire variable)
`Insert`, `Modify`, `ModifyAll`, `Delete`, `DeleteAll`, `Rename`, `TransferFields`, `Init`, `Copy`

## Relationship to AA0242

AA0242 (CodeCop's `Rule0242PartialRecordsDetectJitLoads`) is the **complement** to LC0095:

- **AA0242**: Fires when `SetLoadFields` IS present but accessed fields are missing from it (detects JIT loads)
- **LC0095**: Fires when `SetLoadFields` is entirely absent (recommends adding it)

## Phase 2 roadmap (not yet implemented)

- **Global variables**: Analyze all methods in the object; check access modifiers (protected variables can't use SetLoadFields from outside); detect writes across any method
- **Parameter variables**: Analyze calling context to determine if the parameter is used only for reading
- **Cross-procedure tracing**: Track record variables passed by `var` reference through call chains
- **RecordRef enhanced**: Resolve the opened table at compile time when possible
- **SetBaseLoadFields/AddLoadFields recommendations**: Separate rule for recommending one over the other

## Test coverage

26 test cases organized as:

### HasDiagnostic (6 cases)
| Test case | Scenario |
|---|---|
| LocalRecordGet | Basic Get() without SetLoadFields |
| LocalRecordFindFirst | FindFirst() |
| LocalRecordFindSet | FindSet() + Next loop |
| LocalRecordFindLast | FindLast() |
| LocalRecordFind | Find() |
| LocalRecordMultipleReads | Multiple read ops on same variable (both should report) |

### NoDiagnostic (16 cases)
| Test case | Suppression reason |
|---|---|
| HasSetLoadFields | SetLoadFields already present |
| HasAddLoadFields | AddLoadFields already present |
| HasSetBaseLoadFields | SetBaseLoadFields already present |
| HasModify | Write operation (Modify) |
| HasInsert | Write operation (Insert) |
| HasDelete | Write operation (Delete) |
| HasDeleteAll | Write operation (DeleteAll) |
| HasModifyAll | Write operation (ModifyAll) |
| HasRename | Write operation (Rename) |
| PassedToFunction | Variable passed to user-defined function |
| PassedToEvent | Variable passed to IntegrationEvent publisher |
| PassedToPageRun | Variable passed to PAGE.Run() |
| TemporaryTable | Temporary record variable |
| GlobalVariable | Global variable (Phase 1: skip) |
| ParameterVariable | Parameter variable (not a local) |
| IsEmptyOnly | Only IsEmpty call, no read operation |

### HasFix (4 cases)
| Test case | Scenario |
|---|---|
| SingleField | One field accessed, inserts SetLoadFields with that field |
| MultipleFields | Two fields accessed, inserts SetLoadFields with both (sorted alphabetically) |
| QuotedFieldName | Field with spaces, properly quoted in SetLoadFields |
| NoFieldAccess | No field access found, falls back to primary key fields |

## CodeFix: UsePartialRecordsOnReadCodeFixProvider

### Purpose

Provides a QuickFix "ALCops: Add SetLoadFields" that inserts a `SetLoadFields` call immediately before the read operation, pre-populated with the fields accessed on the variable in the method body.

### Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Scope | Record variables only (not RecordRef) | RecordRef.SetLoadFields takes integer field numbers, not statically determinable |
| No field access case | Primary key fields as fallback | Establishes the partial records pattern; PK fields are always loaded |
| Field detection | In the CodeFix via SemanticModel | Zero analyzer overhead; `Document.GetSemanticModelAsync()` is available |
| Field ordering | Alphabetical (case-insensitive) | Deterministic output regardless of source order |
| Field filtering | Normal fields only | Skip FlowField, FlowFilter, Blob (SetLoadFields returns false for these) |
| Insertion position | New statement before the read operation's containing statement | Matches MS docs conventions |

### Architecture

1. **CodeFix receives** the diagnostic span
2. **Gets SemanticModel** via `document.GetSemanticModelAsync()` (proven pattern from PossibleOverflowAssigning CodeFix)
3. **Resolves the variable** and record type from the invocation instance
4. **Walks the operation tree** with `FieldAccessCollector` (OperationWalker) to find all Normal fields accessed on the variable
5. **Falls back to PK fields** when no Normal field accesses are found
6. **Sorts fields alphabetically** for deterministic output
7. **Builds** `Record.SetLoadFields(Record.Field1, Record.Field2)` syntax using `SyntaxFactory`
8. **Inserts** before the read statement using `root.InsertNodesBefore()`