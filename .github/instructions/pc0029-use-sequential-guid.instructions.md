---
applyTo: 'src/ALCops.PlatformCop/**/UseSequentialGuid*'
---

# PC0029: UseSequentialGuid

## Purpose

Detects `CreateGuid()` calls whose result flows into a Guid field that is part of a table key, and suggests using `CreateSequentialGuid()` instead. Random GUIDs cause SQL index fragmentation; sequential GUIDs reduce it by 20-40%.

**References:**
- [MS Docs: Guid.CreateSequentialGuid](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/guid/guid-createsequentialguid-method)
- [Demiliani: Use Sequential GUIDs](https://demiliani.com/2025/11/21/dynamics-365-business-central-use-sequential-guids-when-possible/)
- [BC 2025 Wave 2 runtime 16.0](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-al-runtime)

## Diagnostic properties

**PC0029** · Category: Performance · Severity: Info · Enabled: true
Message: `Use 'CreateSequentialGuid()' instead of '{0}'. {1}`
Version gate: `Fall2025OrGreater` (runtime 16.0, currently commented out until SDK ships) · Full netstandard2.1 support

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | PlatformCop (PC0029) | Already has Guid rules (PC0015 GuidEmptyStringComparison), platform performance focus |
| Default scope | Key fields only | Security trade-off: sequential GUIDs are predictable; non-key fields may use random GUIDs intentionally |
| Configurable scope | `UseSequentialGuidScope` in alcops.json | Users can opt into `AllGuidFields` mode to flag all CreateGuid() calls |
| Keys checked | All declared keys (primary + secondary + extension keys) | All keys benefit from sequential GUIDs for SQL index performance |
| Temporary tables | Skip | No SQL backing, no index fragmentation benefit |
| Events | Skip | Event parameters flow to external unanalyzable code; passing Rec to events is idiomatic |
| Obsolete | Skip | Standard ALCops convention |
| Cross-procedure tracing | Unlimited depth, cycle detection, intra-module only | Covers helper methods like `SetPrimaryKey(CreateGuid())` |
| Cross-module procedures | Skip if no body (DeclaringSyntaxReference is null) | Symbol-only dependencies don't expose implementation |
| Cross-module tables | Always check key membership | Table symbols always expose field/key metadata regardless of module |
| Variable tracking | Yes | `v := CreateGuid(); Table.PK := v;` is a common pattern |
| Diagnostic location | At the `CreateGuid()` call site | Where the developer needs to make the change |
| CodeFix replacement | Always produce `Guid.CreateSequentialGuid()` | `CreateSequentialGuid()` requires the `Guid.` qualifier; bare `CreateGuid()` gets prefix added, `Guid.CreateGuid()` gets method name replaced |
| Category | Performance | SQL index fragmentation is a database performance concern |
| Severity | Info | Performance suggestion, not a correctness issue |
| Registration | RegisterCodeBlockAction | Single-pass analysis of method/trigger bodies |

## Architecture

### Registration strategy

Uses `RegisterCodeBlockAction` directly in `Initialize` to analyze entire method/trigger bodies in a single pass. Settings are loaded per code block from `ALCopsSettingsProvider`.

### Analysis flow (KeyFieldsOnly mode, default)

Single-pass `CreateGuidFlowWalker` (extends `OperationWalker`) visits the operation tree:

1. **VisitAssignmentStatement**: If `assignment.Value` is `CreateGuid()`:
   - If target is `IFieldAccess` -> `CheckFieldInKey()`
   - If target is a local variable -> `TraceVariable()` to find downstream key field assignments

2. **VisitInvocationExpression**: For each argument that is `CreateGuid()`:
   - If the invocation is `Validate` and arg[1] is `CreateGuid()` -> `CheckValidateTarget()`
   - If the invocation is a user procedure -> `TraceParameter()` with cycle detection

### Analysis flow (AllGuidFields mode)

Same walker reports every `CreateGuid()` call found in assignments or invocation arguments without flow analysis.

### Critical SDK quirk: OperationWalker reference equality

The BC SDK's `OperationWalker` does NOT preserve `IOperation` reference identity across separate walks of the same tree. A two-pass approach (collect nodes, then find parents) fails because `==` on `IOperation` returns false for logically identical nodes from different walks. The single-pass approach avoids this entirely by visiting parent structures (assignments, invocations) and checking their children inline.

### Cross-procedure tracing

`TraceParameter(method, paramIndex, visited)`:
1. Check cycle detection (`visited` HashSet of `IMethodSymbol`)
2. Get method body via `DeclaringSyntaxReference` (null for cross-module -> skip)
3. Walk body with `SymbolFlowTracer` tracking the parameter symbol
4. `SymbolFlowTracer` checks: direct field assignment, Validate call, nested procedure call (recursive)

`TraceVariable(symbol, methodBody, ...)`:
- Same `SymbolFlowTracer` but tracking a local variable instead of a parameter

### Key field resolution

`CheckFieldInKey(IFieldAccess)`:
1. Get field symbol, verify it's Guid type
2. Get record type from the instance, skip if Temporary
3. Resolve to `ITableTypeSymbol`, skip if not Normal table type
4. Iterate all keys (`.Keys` property), check if field is in any key by name comparison

## Known issues and workarounds

### IConversionExpression wrapping

Arguments and assignment values may be wrapped in `IConversionExpression` by the SDK. The `UnwrapConversion()` helper strips this layer before checking for `CreateGuid()` or resolving symbols.

### Fall2025OrGreater version gate

The `SupportedVersions` override is commented out because the `Fall2025OrGreater` field doesn't exist in the current SDK's `VersionCompatibility` class. Reflection returns a "never supported" fallback, which prevents the analyzer from running entirely. Uncomment when the SDK ships runtime 16.0.

### SymbolEqualityComparer

Does NOT exist in the BC SDK (unlike Roslyn). Use plain `HashSet<IMethodSymbol>()` for cycle detection.

### CreateSequentialGuid in test fixtures

`CreateSequentialGuid()` doesn't exist in the current test SDK, so test fixtures cannot reference it in AL code that gets compiled. The `AlreadySequentialGuid` NoDiagnostic test case was removed for this reason.

## Settings

```json
{
  "UseSequentialGuidScope": "KeyFieldsOnly"
}
```

| Value | Behavior |
|---|---|
| `"KeyFieldsOnly"` (default when null/unset) | Only flag `CreateGuid()` when value flows to a key field |
| `"AllGuidFields"` | Flag all `CreateGuid()` calls regardless of context |

Setting is defined as `public string? UseSequentialGuidScope` in `ALCopsSettings.cs`.

## CodeFix: UseSequentialGuidCodeFixProvider

### Purpose

Provides a QuickFix "ALCops: Use CreateSequentialGuid()" that replaces `CreateGuid()` with `CreateSequentialGuid()`, preserving any `Guid.` prefix.

### Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Prefix handling | Always output `Guid.CreateSequentialGuid()` | `CreateSequentialGuid()` requires the `Guid.` qualifier; detect `MemberAccessExpressionSyntax` to avoid `Guid.Guid.` duplication |
| Replacement strategy | Replace method name for `Guid.` form, wrap bare form in `MemberAccessExpression` | Handles both syntax shapes correctly |
| FixAll | Supported via `WellKnownFixAllProviders.BatchFixer` | Standard pattern |

### Architecture

1. Receive diagnostic span (the `CreateGuid()` invocation)
2. Read `HasGuidPrefix` from diagnostic properties
3. If `Guid.` prefix: find `MemberAccessExpressionSyntax`, build new one with `CreateSequentialGuid`
4. If no prefix: find `IdentifierNameSyntax`, replace with `CreateSequentialGuid`
5. Return document with updated syntax root

## Differences from related rules

| Aspect | PC0015 GuidEmptyStringComparison | PC0029 UseSequentialGuid |
|---|---|---|
| Focus | Guid comparison correctness | Guid creation performance |
| Analysis | Single expression check | Flow analysis with cross-procedure tracing |
| Severity | Warning | Info |
| CodeFix | Replace comparison | Replace method name |

## Test coverage

**HasDiagnostic (10 cases):** DirectAssignmentToPrimaryKey, ValidatePrimaryKeyField, DirectAssignmentToSecondaryKeyField, VariableAssignedToKeyField, CrossProcedureIntraModule, OnInsertTrigger, QualifiedCreateGuid, MultiLevelTracing, ValidateSecondaryKeyField, DatabaseObjectReference.
**NoDiagnostic (5 cases):** NonKeyGuidField, TemporaryTable, GuidVariableNotInKey, NonGuidKeyField, AssignedToGuidVariableUsedElsewhere.
**HasFix (2 cases):** SimpleCreateGuid, QualifiedCreateGuid.

## Phase 2 roadmap (not yet implemented)

- **RecordRef support**: Detect `RecordRef.SetTable()` followed by field assignment
- **Return value tracing**: Function returns CreateGuid(), caller assigns to key field
- **Global variable tracking**: Track global variables across methods in the same object
- **Parameter variable tracking**: Analyze calling context for parameters
- **Cross-module setting**: Optional mode to also flag cross-module procedure calls
- **AlreadySequentialGuid NoDiagnostic**: Add test when SDK ships CreateSequentialGuid
- **Suppress for security**: Attribute or comment-based suppression for intentionally random GUIDs
- **Event parameter tracing**: Optional mode to flag event arguments (currently skipped)
