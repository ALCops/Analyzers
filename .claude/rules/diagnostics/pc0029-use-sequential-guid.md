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
| Flow analysis: local variable tracking plus cross-procedure tracing with unlimited depth and cycle detection, intra-module only | `v := CreateGuid(); Table.PK := v;` and helpers like `SetPrimaryKey(CreateGuid())` are common; cross-module procedures have no body (`DeclaringSyntaxReference` is null) while cross-module tables still expose key metadata, so key membership is always checked |
| Single-pass walker that inspects assignments and invocations inline, instead of a collect-then-find-parents pass | The SDK `OperationWalker` does not preserve `IOperation` reference identity across walks (see SDK facts) |
| Diagnostic at the `CreateGuid()` call site | Where the developer makes the change |
| Version gate `Fall2025OrGreater` (runtime 16.0); full netstandard2.1 support | `CreateSequentialGuid()` ships with runtime 16.0 |

## Deliberate non-reports

- Fields of temporary tables: no SQL backing, so no index fragmentation.
- `CreateGuid()` passed to event parameters: they flow to unanalyzable external code, and passing `Rec` to events is idiomatic.
- Obsolete symbols (standard ALCops convention).
- Flows through cross-module procedures: symbol-only dependencies expose no body.
- In the default `KeyFieldsOnly` scope, `CreateGuid()` assigned to non-key Guid fields.

## SDK facts

- `OperationWalker` does not preserve `IOperation` reference identity across separate walks of the same tree; `==` on logically identical nodes from different walks is false.
- `SymbolEqualityComparer` does not exist in the BC SDK (unlike Roslyn); cycle detection uses a plain `HashSet<IMethodSymbol>`.

## Test notes

- All fixtures require runtime 16.0 (`RequireMinimumVersion("16.0")`) because `CreateSequentialGuid()` must compile in the HasFix expected output.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `UseSequentialGuidScope` | `KeyFieldsOnly` (when null/unset) | `AllGuidFields` flags every `CreateGuid()` call regardless of where the value flows |

## CodeFix: UseSequentialGuidCodeFixProvider

| Decision | Rationale |
|---|---|
| Always produce `Guid.CreateSequentialGuid()`: bare `CreateGuid()` gets the prefix added, `Guid.CreateGuid()` gets only the method name replaced | `CreateSequentialGuid()` requires the `Guid.` qualifier; detecting the existing `MemberAccessExpressionSyntax` avoids `Guid.Guid.` |
| FixAll via `WellKnownFixAllProviders.BatchFixer` | Standard pattern |
