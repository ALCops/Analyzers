---
paths:
  - "src/ALCops.PlatformCop/**/UseSequentialGuid*"
  - "src/ALCops.PlatformCop.Test/Rules/UseSequentialGuid/**"
---

# PC0029: UseSequentialGuid

## Purpose

Detects `CreateGuid()` calls whose result flows into a Guid field that is part of a table key, and suggests using `CreateSequentialGuid()` instead. Random GUIDs cause SQL index fragmentation; sequential GUIDs reduce it by 20-40%.

Registers code-block actions from CompilationStart on method/trigger bodies, capturing its compilation for the shared settings snapshot; main type `CreateGuidFlowWalker`.

**References:**
- [MS Docs: Guid.CreateSequentialGuid](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/guid/guid-createsequentialguid-method)
- [Demiliani: Use Sequential GUIDs](https://demiliani.com/2025/11/21/dynamics-365-business-central-use-sequential-guids-when-possible/)
- [BC 2025 Wave 2 runtime 16.0](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-al-runtime)

## Design decisions

| Decision | Rationale |
|---|---|
| PlatformCop | Platform performance focus; already hosts the Guid rule PC0015 |
| Severity Info, category Performance | SQL index fragmentation is a performance suggestion, not a correctness issue |
| Default scope: key fields only, configurable to all Guid fields | Sequential GUIDs are predictable, so non-key fields may use random GUIDs intentionally; users who want everything flagged opt in via `UseSequentialGuidScope` |
| All declared keys count (primary, secondary, extension keys) | Every key benefits from sequential GUIDs for SQL index performance |
| Flow analysis: local and object-scope variable tracking plus cross-procedure tracing with unlimited depth and cycle detection, intra-module only | `v := CreateGuid(); Table.PK := v;` and helpers like `SetPrimaryKey(CreateGuid())` are common; cross-module procedures have no body (`DeclaringSyntaxReference` is null) while cross-module tables still expose key metadata, so key membership is always checked |
| A global counts as flowing to a key when any method or trigger of the containing object writes it to one; sibling bodies are found with `DescendantNodes()` on the object syntax, skipped when their text never spells the variable name, and otherwise bound on demand from the same semantic model | Execution order across bodies is unknowable and Info severity tolerates the over-report; `DescendantNodes()` also reaches page and report control triggers, which are not object members; the walk only runs when a `CreateGuid()` is assigned to a global |
| The tracer takes the sibling method as containing symbol when tracing a global | A global's `ContainingSymbol` is the object, whose `ContainingType` is null, so `GetReceiverTableType` could not resolve bare-self field access inside a table |
| Case-insensitive text pre-filter on `CreateGuid` before binding a body | Without it the rule binds and walks every body in the compilation; a false positive costs one bind and `IsCreateGuidCall` still decides by symbol |
| Common `UnwrapConversions()` instead of a private one-level unwrap | It also peels `IParenthesizedExpression`, so `(CreateGuid())` is detected |
| Single-pass walker that inspects assignments and invocations inline, instead of a collect-then-find-parents pass | The SDK `OperationWalker` does not preserve `IOperation` reference identity across walks (see SDK facts) |
| Key membership reads `PrimaryKey` first, then the declared `Keys` | `Keys` lists declared keys only; a table without a `keys` section has a synthesized primary key (lowest-Id field with a valid key type) that only `PrimaryKey` exposes, also for tables from referenced apps (`receiver-forms.md`) |
| Receiver resolution stays entirely in `GetReceiverTableType`, with no per-form code in the analyzer | The helper already covers the null instance of bare self in tables and tableextensions, the synthesized `Rec` global of the page family, the synthesized `Rec` local of a `TableNo` codeunit's `OnRun`, report dataitem access and `this`; fixtures pin every origin so a helper regression surfaces here |
| Diagnostic at the `CreateGuid()` call site | Where the developer makes the change |
| Version gate `Fall2025OrGreater` (runtime 16.0); full netstandard2.1 support | `CreateSequentialGuid()` ships with runtime 16.0 |

## Deliberate non-reports

- Fields of temporary tables: no SQL backing, so no index fragmentation.
- `CreateGuid()` passed to event parameters: they flow to unanalyzable external code, and passing `Rec` to events is idiomatic.
- Obsolete symbols (standard ALCops convention).
- Flows through cross-module procedures: symbol-only dependencies expose no body.
- Flows through `var` parameters or return values back to callers, and through `RecordRef.SetTable()`: caller lookup needs a compilation-wide scan that would re-run on every editor pass of the helper file.
- Intentionally random GUIDs (unpredictable public identifiers): no attribute or comment suppression; `#pragma warning disable PC0029` is the documented way.
- In the default `KeyFieldsOnly` scope, `CreateGuid()` assigned to non-key Guid fields.

## SDK facts

- `OperationWalker` does not preserve `IOperation` reference identity across separate walks of the same tree; `==` on logically identical nodes from different walks is false.
- `SymbolEqualityComparer` does not exist in the BC SDK (unlike Roslyn); cycle detection uses a plain `HashSet<IMethodSymbol>`.

## Test notes

- All three test methods call `RequireMinimumVersion("16.0")` because `CreateSequentialGuid()` must compile in the HasFix expected output and in the `AlreadySequentialGuid` NoDiagnostic fixture.
- The HasDiagnostic set covers every `Rec` origin and receiver form of `receiver-forms.md` (table trigger and procedure, tableextension, page, pageextension, request page, report and xmlport via request page, report dataitem, `TableNo` `OnRun`), plus tables without a `keys` section and a namespaced file with a fully qualified record type; the NoDiagnostic set pins the temporary check through a page `Rec` and a non-key field of a table without a `keys` section.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `UseSequentialGuidScope` | `KeyFieldsOnly` (when null/unset) | `AllGuidFields` flags every `CreateGuid()` call regardless of where the value flows |

## CodeFix: UseSequentialGuidCodeFixProvider

| Decision | Rationale |
|---|---|
| Always produce `Guid.CreateSequentialGuid()`: bare `CreateGuid()` gets the prefix added, `Guid.CreateGuid()` gets only the method name replaced | `CreateSequentialGuid()` requires the `Guid.` qualifier; detecting the existing `MemberAccessExpressionSyntax` avoids `Guid.Guid.` |
| FixAll via `WellKnownFixAllProviders.BatchFixer` | Standard pattern |
