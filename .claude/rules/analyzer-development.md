---
paths:
  - "src/ALCops.*/Analyzers/**"
---

# Analyzer Development

Core rules for every analyzer in the six cops. Sibling guides that load with this one: `sdk-analysis-scope.md` (how the host runs callbacks), `symbol-resolution.md` (symbols, canonical names, the `GetSymbol()` bug), `record-receiver-forms.md` (the four ways AL reaches a record), `analyzer-performance.md` (cost model and patterns). Creating a rule: `/new-analyzer`. Tests: `.claude/rules/testing.md`.

## NAV SDK source (mandatory)

First stop: the `nav-sdk-docs` plugin (enabled through `.claude/settings.json`; sibling checkout `../nav-sdk-docs`). Its `reference/` tables give every public member's availability at the `ns2.0 12.0`, `net8 16.0`, `net10 18.0.36` and latest SDK versions, its `docs/` pages explain how registrations, passes, symbols, operations, semantic model and code fixes behave with citations into the decompiled source, and `/nav-sdk-docs:sdk-lookup` answers a question from both. Take availability from the tables, never from memory or from the latest source; a member that is `no` at `ns2.0 12.0` needs a guard or a version gate.

`../nav-sdk-source` (sibling of this repo) holds the decompiled `Microsoft.Dynamics.Nav.CodeAnalysis` SDK, Microsoft's own CodeCops, and the compiler and editor host. It is the only documentation of syntax kinds, operation shapes, symbol members and driver behaviour. Read it before using an SDK API, when an SDK shape surprises you, and when a build fails on one TFM only. Its `.github/instructions/` folder explains how to navigate it; Microsoft's `Rule0xxx` classes are the reference implementations for callback shapes and fix registration.

The repo is version-controlled per AL release: `git -C ../nav-sdk-source tag` lists the releases, `git diff <refA>..<refB> -- Microsoft.Dynamics.Nav.CodeAnalysis/...` shows what changed in the binder or semantic model between two AL versions, and `git checkout <ref>` browses one version (`VERSION.json` records what is checked out; restore the original ref afterwards). Pin a "behaviour X changed in AL y" claim with such a diff, not with an empirical probe alone.

## Non-negotiables

- **Plain `DiagnosticAnalyzer`, `[DiagnosticAnalyzer]`, `sealed`.** Never derive from the `ALCopsDiagnosticAnalyzer` / `{Cop}Analyzer` harness (`analyzer-exception-harness.md`).
- **`IsObsolete()` first** in every callback (available on all four analysis contexts). Reporting on obsolete code is noise.
- **`EnumProvider` for every SDK enum value** (`ALCops.Common.Reflection`). Direct `SymbolKind.X` / `PropertyKind.X` references break on other SDK versions. A member missing from the loaded SDK resolves to an inert fallback: `default(T)` for most enums, an out-of-range sentinel for `SymbolKind` (because `default(SymbolKind)` is `Module`, and the driver ignores kinds above the enum's maximum). Guard with `!= default` only for enums whose zero member is `None`; never for `SymbolKind`.
- **Typed property access.** `GetEnumPropertyValue<T>(EnumProvider.PropertyKind.X)`, `GetBooleanPropertyValue()`, `GetProperty()`. Never compare `ValueText` strings for property values.
- **`GetSymbolSafe()`, never `GetSymbol()`**, on operations. `symbol-resolution.md` explains the SDK bug.
- **Symbols, not text, identify things.** Resolve via the operation tree or `SemanticModel`, then compare symbols or `ISymbol.Name` (`symbol-resolution.md`).
- **`SemanticFacts` for AL identifier comparison** (`IsSameName`, `NameEqualityComparer`, `NameEqualityComparison`, `NameComparer`); raw `OrdinalIgnoreCase` only for non-AL text. Table in `symbol-resolution.md`.
- **No mutable instance fields.** Analyzer instances are shared across passes and projects; per-compilation state lives in `CompilationStart` closures or parameters (`sdk-analysis-scope.md`).
- **Analyze and report inside one callback.** No accumulate-here, report-there patterns (`sdk-analysis-scope.md`).
- **Report at the most specific location**: the offending property, node, or symbol, not the whole object.
- **One analyzer class per rule or tightly coupled group** (`AnalyzeCountMethod` serves LC0081 and LC0082 because the analysis is shared). `SupportedDiagnostics` lists every descriptor the class can report.
- **`ctx.CancellationToken.ThrowIfCancellationRequested()`** inside loops over large collections.

## Choosing a registration

| Method | Context | Use when |
|---|---|---|
| `RegisterSymbolAction` | `SymbolAnalysisContext` | Declared symbols (objects, fields, methods). Most rules. |
| `RegisterOperationAction` | `OperationAnalysisContext` | One operation kind in bodies, when the rule needs no per-body state and the kind is not ubiquitous. Fires once per matching operation in the compilation (`analyzer-performance.md`). |
| `RegisterSyntaxNodeAction` | `SyntaxNodeAnalysisContext` | Raw syntax (literals, identifiers, trivia). Gives `SemanticModel` directly. |
| `RegisterCodeBlockAction` | `CodeBlockAnalysisContext` | Whole method or trigger bodies: metrics, flow analysis, any per-body state. |
| `RegisterCompilationAction` | `CompilationAnalysisContext` | Manifest and compilation-level checks only. |
| `RegisterCompilationStartAction` | `CompilationStartAnalysisContext` | Load expensive resources once (XLIFF, settings, indexes), then register self-contained inner actions that read them. Exit without registering when the resource is absent. |

`RegisterOperationBlockStartAction` exists in the SDK but its callback never runs; do not use it (`sdk-analysis-scope.md`).

## Version gating

A rule that only applies from a BC runtime onwards overrides `SupportedVersions`:

```csharp
public override VersionCompatibility SupportedVersions =>
    VersionProvider.VersionCompatibility.Fall2024OrGreater;
```

SDK members absent at the netstandard2.1 floor need a guard or an operation-tree alternative: `netstandard21-compatibility.md`.

## Test-context gotchas

- `ManifestHelper.GetManifest` throws `FileNotFoundException` in test compilations (the `Microsoft.Dynamics.Nav.Analyzers.Common` assembly is not loaded). Catch it and treat the manifest as null.
- `Compilation.FileSystem` is null in some test and IDE contexts; bail out, do not throw.
- The SDK swallows analyzer exceptions, and `HasDiagnostic`/`NoDiagnostic` fixtures still pass when the analyzer throws. A throwing analyzer looks like one that reports nothing (`testing.md`).

## Shared helpers

Browse `src/ALCops.Common/Extensions|Helpers|Reflection|Permissions` before writing a helper (`common-library.md`). Frequently needed: `GetSymbolSafe`, `GetReceiverTableType`, `IsTemporary()`, `GetContainingApplicationObjectTypeSymbol()`, `RecordMethodClassification`, `TableHelper.IsSetupTable`, `MandatoryAffixes`.
