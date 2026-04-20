---
applyTo: 'src/ALCops.LinterCop/**/UnnecessaryRecordParameterInMethodCall*'
---

# LC0096: UnnecessaryRecordParameterInMethodCall

## Purpose

Detects redundant record parameters passed to methods where the same record variable is already the invocation instance. Covers two patterns:

1. **External call**: `MyRecord.MyProcedure(MyRecord)` from any context
2. **Internal call**: `MyProcedure(Rec)` inside tables, pages, and their extensions

**References:**
- [BusinessCentral.LinterCop LC0094](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0094) (original rule)
- [BC.LinterCop PR #1132](https://github.com/StefanMaron/BusinessCentral.LinterCop/pull/1132)

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `LC0096` |
| Category | Usage |
| Severity | Warning |
| Enabled by default | true |
| MessageFormat | `The record variable '{0}' is passed as an argument to a method that is already invoked on the same record. Remove the redundant parameter.` |
| Version gate | None |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | LinterCop (LC0096) | Code quality/readability rule, not runtime safety or app conventions |
| ID | LC0096 | LC0094 is taken by AllowInCustomizationsRedundancy; LC0095 is highest existing |
| Scope | Tables, Pages, Table Extensions, Page Extensions | All objects with implicit `Rec`. Pages: local methods only |
| Severity | Warning | Actionable code smell, not a runtime error |
| CodeFix | None (v1) | Removing the arg breaks compilation without also changing the callee signature |
| Module restriction | Current module only (object equality) | Avoids flagging calls to dependency methods the developer can't refactor |
| Event publishers | Skip | Passing `Rec` to events is idiomatic AL; event signatures are public contracts |
| Page local-only | Only flag `MyProcedure(Rec)` on pages when target method is `local` | Public/internal page methods accepting the source record is intentional API design for decoupling and testability. Tables flag all because the table IS the record. |
| Rec matching | `IsSynthesized` + `SemanticFacts.IsSameName` | Matches only the compiler-generated Rec variable (not user-declared globals). Name check discriminates Rec from xRec (both synthesized). |
| Implicit `with` | Not affected | Implicit with only adds `Rec.` as a scope prefix for field/method lookup. It does NOT inject `Rec` as a method argument. `MyProcedure(Rec)` requires explicit mention of `Rec` regardless of implicit with. |
| Category | Usage | Incorrect/discouraged use of AL constructs |
| netstandard2.1 | Full support | No net8.0-only APIs used |

## Architecture

### Registration strategy

Uses `RegisterOperationAction` on `OperationKind.InvocationExpression` for single-pass per-invocation analysis.

### Analysis flow (optimized for early exit)

1. `IsObsolete()` check (cheapest)
2. Cast to `IInvocationExpression`
3. `Arguments.IsEmpty` check
4. `TargetMethod.IsEvent` check (skip event publishers)
5. Branch on `Instance`:
   - **Non-null instance** → External call path (`AnalyzeExternalCall`)
   - **Null instance** → Internal call path (`AnalyzeInternalCall`)

### External call path

1. Check `Instance.Type.NavTypeKind == Record`
2. Check module restriction via `ContainingModule` object equality
3. Skip built-in methods
4. Resolve instance symbol
5. For each argument: resolve symbol via `ResolveArgumentSymbol`, then symbol identity check via `Equals()`
6. Report at argument location

### Internal call path

1. Get containing object syntax
2. Check it's a table, page, or extension thereof
3. **For pages/page extensions**: skip if target method is NOT `local` (public/internal methods are intentional API design)
4. Check module restriction
5. Skip built-in methods
6. For each argument: resolve symbol, check `Kind == GlobalVariable`, `IsSynthesized`, and `SemanticFacts.IsSameName(name, "Rec")`
7. Report at argument location

### Performance optimizations

- **Symbol-based checks only**: Uses `IOperation.GetSymbol()` (a property accessor on the bound tree, O(1)) instead of text-based `syntax.ToString()` comparisons
- **Early exits**: Non-record invocations, empty arguments, events, and built-in methods all exit before any symbol resolution
- **No SemanticModel retrieval**: Uses `IOperation.GetSymbol()` and `IArgument.Value.GetSymbol()` instead of `SemanticModel.GetSymbolInfo()`
- **Conversion unwrapping**: `ResolveArgumentSymbol` handles `IConversionExpression` wrapping in a single utility method

## Known issues and workarounds

### IConversionExpression wrapping

Arguments may be wrapped in `IConversionExpression` by the SDK. When `argument.Value.GetSymbol()` returns null, the analyzer unwraps through the conversion and calls `GetSymbol()` on the operand.

## Differences from original BC.LinterCop LC0094

| Aspect | BC.LinterCop LC0094 | ALCops LC0096 |
|---|---|---|
| Scope | Tables only | Tables, pages, table extensions, page extensions |
| Page methods | N/A (no page support) | Only local methods flagged (public/internal are intentional API) |
| Module check | String-based `Compilation.ModuleName` comparison | `ContainingModule` object equality |
| Rec matching | String comparison against "Rec" | `IsSynthesized` guard + `SemanticFacts.IsSameName` (matches compiler-generated Rec only, discriminates from xRec) |
| Performance | SemanticModel retrieval before type check | Type check and early exits before any symbol work |
| Helper dependency | External `HelperFunctions.IsOperationInvokedInTable` | Inline syntax check, no external helper |

## Test coverage

### HasDiagnostic (6 cases)

| Test case | Scenario |
|---|---|
| ExternalRecordMethodCall | `MyRecord.MyProcedure(MyRecord)` from a codeunit |
| InternalTableMethodCall | `MyProcedure(Rec)` inside a table |
| InternalPageMethodCall | `MyProcedure(Rec)` inside a page (local method) |
| InternalTableExtensionMethodCall | `MyProcedure(Rec)` inside a table extension |
| InternalPageExtensionMethodCall | `MyProcedure(Rec)` inside a page extension (local method) |
| MultipleArguments | `MyRecord.MyProcedure(OtherParam, MyRecord)` flags matching arg |

### NoDiagnostic (6 cases)

| Test case | Suppression reason |
|---|---|
| DifferentParameter | `MyRecord.MyProcedure(MyRecord2)` different variable |
| EventPublisher | `OnBeforeMyProcedure(Rec, IsHandled)` event publisher skipped |
| BuiltInMethods | `Clear(MyTable)` built-in method |
| PageRunModal | `Page.RunModal(Page::MyPage, MyTable)` built-in, not on the record |
| FieldAccessExpression | `MyTable.MyProcedure(MyTable."Name")` field value, not the record |
| PublicPageMethodWithRec | `CalculateInBackground(Rec)` on public page method (intentional API) |

## Phase 2 roadmap (not yet implemented)

- **CodeFix**: Remove the redundant parameter from both call site and method signature
- **RecordRef**: Extend to `RecordRef` variables
- **xRec detection**: Flag `MyRecord.MyProcedure(xRec)` in contexts where xRec semantics are not needed
- **Cross-module analysis**: Optional mode to flag cross-module calls (configurable)
