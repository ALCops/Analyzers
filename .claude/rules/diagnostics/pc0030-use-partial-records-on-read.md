---
paths:
  - "src/ALCops.PlatformCop/**/{UsePartialRecordsOnRead,PartialRecordOperations}*"
  - "src/ALCops.PlatformCop.Test/Rules/UsePartialRecordsOnRead/**"
---

# PC0030: UsePartialRecordsOnRead

## Purpose

Recommends using `SetLoadFields` (or `AddLoadFields`/`SetBaseLoadFields`) before read operations on local record variables. Without partial records, the runtime loads ALL normal fields including those from table extensions, causing unnecessary SQL joins and 2-9x slower data access.

Registers `RegisterCodeBlockAction` on method/trigger bodies; main type `PartialRecordOperations` (shared with PC0031) with the `SetLoadFieldsWalker` operation walker.

**References:**
- [Discussion #155](https://github.com/ALCops/Analyzers/discussions/155)
- [Community discussion](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/218)
- [MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records)
- [MS Docs: Record.SetLoadFields](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-setloadfields-method)

## Design decisions

| Decision | Rationale |
|---|---|
| Local variables only | Global variables need cross-method analysis; parameters need the calling context |
| Complement to CodeCop AA0242: fires only when `SetLoadFields` is entirely absent | AA0242 already covers the case where `SetLoadFields` is present but misses accessed fields (JIT loads) |
| Severity Info | Performance suggestion, not a correctness issue |
| Report on the read operation alone; no field access required | User preference; accepted noise on patterns like `if Rec.Get(Key) then` |
| Any pass of the variable to any invocation, including built-in methods like `PAGE.Run`, suppresses it; only bare variable arguments count, not field accesses | Conservative: the callee might access any field, while `SetRange(MyTable."No.")` only reads one field |
| `RecordRef` variables are included, without temporary/table-type checks | The opened table is not statically determinable for a `RecordRef` |
| `RecordRef.SetTable(Record)` (single-argument overload) links the `RecordRef` to the target record's suppression state; the target must be local and resolvable | If the target record has write ops, load fields, is passed to a function, or is a setup/reference table, suggesting `SetLoadFields` on the `RecordRef` could interfere or would hit the same cache trade-off as the record itself |
| Flow-sensitive forward dataflow with fork/merge at branches, all flags (`HasLoadFields`, `HasFullRecordAccess`, `PassedToFunction`) flow-aware | Positional flags caused false negatives when `SetLoadFields` came after the read, `Clear` sat between them, or one branch had it; all flags share the same positional bug class |
| `HasLoadFields` merges with AND (intersection) | If any path lacks `SetLoadFields`, the read may execute without it |
| `HasFullRecordAccess`/`PassedToFunction` merge with OR (union) | If any path writes, assigns or passes the variable, suggesting `SetLoadFields` could interfere |
| Pre-fork uncovered reads merge by intersection, in-branch reads by union | A read before a fork that is retroactively cleared in any branch stays cleared (`if FindSet then repeat Modify`), while a read added inside a branch is genuinely uncovered on that path |
| Reads are evaluated immediately but cleared retroactively when a later write, pass or whole-record assignment is met | Handles `Get(); Modify()` where the full-record operation follows the read |
| Whole-record assignment with the variable as source (`TempMyTable := MyTable`) counts as full-record access | The copy reads all fields; a preceding partial read would produce a partial copy. Only bare variable references on the right-hand side, not field access |
| `exit(Rec)` clears uncovered reads on the current path only, sets no forward flag, and only for bare variable references | The path terminates at `exit`, so a forward flag would leak suppression past the enclosing branch via OR-merge and hide reads after an early-exit guard ([#429](https://github.com/ALCops/Analyzers/issues/429)); `exit(Rec."No.")` reads one field |
| `Clear(var)` and `var.Reset()` reset all flags; `var.SetLoadFields()` without arguments resets `HasLoadFields` only | `Clear`/`Reset` reinitialize the variable; a no-argument `SetLoadFields` only cancels the partial-records state |
| `while`/`for`/`foreach` merge the pre-loop state with the post-body state; `repeat-until` uses the post-body state directly | The former bodies might not execute, the latter always executes at least once |
| Single pass over loops, no fixed-point iteration | Acceptable approximation; fixed-point adds complexity for rare edge cases |
| `RecordRef` suppression stays method-level (`EverHad*` flags) rather than flow-sensitive | `SetTable` linkage is already complex; flow fixes concentrate on `Record` variables |
| All reads on setup and reference tables are suppressed | The NST record cache serves few-row tables from memory; `SetLoadFields` bypasses that cache and re-queries SQL every time, making the "optimization" net slower ([#531](https://github.com/ALCops/Analyzers/issues/531), [#297](https://github.com/ALCops/Analyzers/issues/297), [Duilio Tacconi](https://duiliotacconi.com/2026/09/07/partial-record-vs-nst-caching-the-strange-case-of-calculate-low-level-code/)). Recognizer `TableHelper.IsSetupOrReferenceTable`, signals cheapest first: a single Code-type PK field named `Primary Key`/`PrimaryKey` (the existing setup heuristic, [#283](https://github.com/ALCops/Analyzers/issues/283)/[#287](https://github.com/ALCops/Analyzers/issues/287)), `Code` or `Name` (~90% precision on w1-28.4); a namespace ending in `.Setup` (structural, via `GetContainingNamespaceQualifiedNameWithReflection`; ~90% precision); a parameterless `GetRecordOnce` method. Any `AutoIncrement` field in the primary key vetoes the match (growing log-style tables that happen to live in a Setup namespace; checked on every PK field so a compound `Code, "Entry No."` key is caught too). Field count, table-name suffixes and per-call context rejected: field count is meaningless (General Ledger Setup has 114 fields and one row), names are locale-dependent, context is cross-method. `IsSetupTable` stays separate for AC0013 (`FieldGroupsRequired`) which relies on its narrower meaning |
| Version gate `Spring2021OrGreater` (runtime 6.0, BC17); full netstandard2.1 support | `SetLoadFields` was introduced with runtime 6.0 |
| Bound receiver lookups (`Instance.GetSymbolSafe()`) require `SymbolKind.LocalVariable` before consulting the name-keyed map; text lookups (`GetVariableNameFromOperation`) stay name-based | `this.<Global>` and `this.<DataItem>` reach an object-scope symbol even when a same-named local exists, and that symbol's name equals the local's key: without the kind check a global write hid the local's read and a global read was reported against the local. A bare identifier always binds innermost, so name-based text lookups cannot alias. The synthesized `OnRun` `Rec` of a `TableNo` codeunit is a `LocalVariable` too, so it stays tracked |

## Deliberate non-reports

- Global variables and parameters: only locals are tracked.
- Temporary records (`temporary` keyword or `TableType = Temporary`) and non-SQL table types (CDS, Exchange, etc.): no SQL backing, no table-extension join to save; matches AA0242.
- Records passed to any invocation, including built-in methods such as `PAGE.Run`: the callee may access any field.
- Variables with any write (`Insert`, `Modify`, `ModifyAll`, `Delete`, `DeleteAll`, `Rename`, `TransferFields`, `Init`, `Copy`), whole-record assignment source, or `exit(Rec)` on the path.
- All reads on setup and reference tables, including reads through a `RecordRef` linked by `SetTable`: setup singletons (single Code-type PK named `"Primary Key"`/`"PrimaryKey"`, or `GetRecordOnce`), `.Setup` namespace tables, and tables with a single Code-type PK named `Code` or `Name`. Any `AutoIncrement` PK field excludes the table. These tables' rows stay resident in the NST record cache; `SetLoadFields` bypasses it and re-queries SQL, net slower.
- `Rec`, `this` and bare self forms inside tables and tableextensions: they are object-scope globals, never entered into the local-variable map; no realistic use for `SetLoadFields` on the object's own record ([#348](https://github.com/ALCops/Analyzers/issues/348)).
- `RecordRef.SetTable(Record, Boolean)` (ShareTable overload), `RecordRef.GetTable`, and `ClearAll()`: out of scope; revisit if users report false positives.

## Known issues

- Merge points deduplicate uncovered reads by `SourceSpan.Start` (`HashSet<int>`): without it, concatenating both branches doubled the list at each nesting level (2^K after K levels) and threw `OutOfMemoryException` in `FlowFlags.Clone()` on the Base App.
- Namespace false positives: the `AutoIncrement` veto removes `Payment Reporting Argument` and `Adv. Bonus Depreciation Setup`; `Close Income Statement Buffer`, `Inventory Period Entry` and `Warehouse Employee` (compound Code/Date keys, no `AutoIncrement`) stay recognized although they grow. Accepted: PC0030 merely goes quiet (no harmful advice, only missed advice).
- Compound-PK reference tables outside `.Setup` (`Inventory Posting Setup`, `Dimension Value`, `Currency Exchange Rate`, posting setups in a partner namespace) and small tables with a single Code PK named `No.` (`Resource Group`, `Item Charge`) are not recognized and still report. Accepted: the `No.` shape is 100% master data or document headers in the Base App, and a compound-PK signal without name matching does not exist.
- `ISymbol.ContainingNamespace` is available at all four SDK reference versions (ns2.0 12.0, net8 16.0, net10 18.0.36 and latest). Referenced modules rebuild their namespace tree from the symbol package (`ReferenceModuleSymbol.BuildNamespaceTree`), so Base App tables referenced from a partner app carry their namespace. A null namespace (pre-namespace runtimes) means the signal is absent, not a false match.

## Test notes

- The `this` fixtures are gated on runtime 14.0 (`SkipTestIfVersionIsTooLow`).

## CodeFix: UsePartialRecordsOnReadCodeFixProvider

| Decision | Rationale |
|---|---|
| `Record.SetLoadFields(...)` goes immediately before the read's own statement: the nearest ancestor statement that sits in a statement list (`BlockSyntax`, `RepeatStatementSyntax.Statements`, `CaseElseSyntax.ElseStatements`) gets an insert before it; one embedded as the single statement of an if/else branch, case line, `while`/`for`/`foreach`/`with` body is wrapped in `begin SetLoadFields; <statement> end`, including else-if chains (`end else begin`) | Matches the MS Docs convention. Climbing to the nearest `BlockSyntax` child instead put the call before a distant `repeat`, `case` or `if`, outside the branch that reads |
| A read in a loop or if condition targets the loop or if statement itself; a read inside `asserterror` climbs past it to the `asserterror` statement | The call must run before the condition is evaluated; the statement that is expected to fail stays unchanged |
| Custom `FixAllProvider.Create` with one bottom-up `ReplaceNodes` over the original tree; the single fix runs the same path | Nested reads (`if A.Get() then if B.Get() then`) give nested wraps whose edits overlap, and `BatchFixer` drops all but one of them. Edits are grouped by target statement and deduplicated by variable symbol (two reads of one variable in one statement get one call, the same variable in two branches gets one per branch), ordered by read position. The fix always starts from `GetDocumentDiagnosticsAsync`: the SDK passes empty `fixAllSpans` for every scope (`FixAllState.GetFixAllSpansAsync`, same at 12.0, net8 and net10), and any non-empty spans only filter those diagnostics by intersection |
| No fix when the statement to wrap `ContainsDirectives` (active or inactive `#if`, `#pragma`); directives outside it are preserved | Wrapping rebuilds the statement and moves its trivia; preserve-or-bail policy. Outside the statement, a wrap replaces only the whitespace before a following `else`, and only when it is pure whitespace: without that check an `#if` before the `else` was dropped and its `#endif` left unbalanced. An insert lands before the target's leading trivia, so it is not offered either when the target's first token carries a directive: an `#if` guarding the read (or the `#endif` of a preceding block) would leave the call on the other side of it, running unconditionally. The formatter pass re-indents a directive that leads the token after the block (`#endregion` gets the code's indentation), as Format Document would |
| `RegisterCodeFixesAsync` only resolves the read and its target statement; the field list is collected when the action runs, and the document's line break is looked up once per pass | Collecting fields walks the method body's operation tree, which the lightbulb query does not need |
| Wrap trivia: the block takes the statement's leading trivia and, when the statement ended with `;`, its own `;`. The `end` keyword gets a real line break, never the factory's elastic one; a trailing line comment moves behind `end` | The CodeAction formatter (`Formatter.Annotation` on the block) re-indents the wrapped statement and nested lines, but turns an elastic break before `end` into a blank line. The lexer leaves the line break after a line comment to the next token, so a comment kept inside would sit directly before `end` and comment it out |
| A then-branch followed by `else` puts the `else` on the `end` line (`end else`) only when whitespace alone separates them | The if statement is replaced as well so the `else` keyword's leading whitespace becomes elastic; a comment or directive there stays and the `else` keeps its own line |
| `Record` variables only, not `RecordRef` | `RecordRef.SetLoadFields` takes integer field numbers that are not statically determinable |
| Field list = Normal fields whose values are consumed in the method body, resolved in the CodeFix via `Document.GetSemanticModelAsync()`; sorted alphabetically (case-insensitive) | Zero analyzer overhead; deterministic output regardless of source order. FlowField, FlowFilter and Blob are skipped because `SetLoadFields` returns false for them |
| Two built-in selector sets are excluded from consumption: first-argument selectors (`SetRange`, `SetFilter`, `GetFilter`, `FieldCaption`, ...) and all-argument selectors (`SetCurrentKey`, `AddLoadFields`, ...); `TestField`/`FieldError` remain consumers | A field named as a filter or key selector is not read from the buffer, but a field passed as a filter *value* (`SetRange(F1, MyTable.F2)`) is. Only built-in methods are excluded because user-defined calls already suppress the diagnostic |
| Fallback to primary-key fields when no consumed field is found (including all-selector cases) | Establishes the partial-records pattern; PK fields are always loaded |
| Field and method receivers are matched by symbol identity with the diagnosed variable (`GetSymbolSafe()` then `Equals`), not by name | A `this.<Global>` receiver can share the local's name; name matching put the global's fields into the generated `SetLoadFields` list |
| The `SetLoadFields` receiver reuses the read's receiver expression (`WithoutTrivia()` plus a leading `SyntaxFactory.ElasticMarker`) instead of rebuilding it from the symbol name | Keeps the source spelling, quotes and casing (`"my table".Get()` gets `"my table".SetLoadFields(...)`); the elastic marker lets the CodeAction formatter indent the inserted statement. Same approach as the PC0035 fix |
