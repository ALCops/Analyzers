---
applyTo: 'src/ALCops.Common/Diagnostics/**'
---

# Analyzer Exception Harness (XX0000)

The harness converts an unhandled exception thrown by any ALCops analyzer into a
**located `XX0000` diagnostic** (`AC0000`, `DC0000`, `FC0000`, `LC0000`,
`PC0000`, `TA0000`) at the object/line being analyzed, instead of the SDK's
generic `AD0001` on `app.json` line 1. This makes analyzer defects diagnosable:
the message names the failing analyzer and the exception, and the location points
at the input that triggered it.

## Why it exists

The NAV SDK's `AnalyzerExecutor` catches every analyzer exception and emits
`AD0001` at `Location.None`. There is **no global `onAnalyzerException` hook** an
analyzer can register from inside `Initialize`. The only interposition point is
the delegate passed to each `Register*Action`. The harness wraps those delegates
transparently so adopting analyzers need no per-callback try/catch (the manual
`Rule0000ErrorInRule` pattern used in BusinessCentral.LinterCop).

## Components (in `ALCops.Common/Diagnostics/`)

| Type | Role |
|---|---|
| `ALCopsDiagnosticAnalyzer` | Abstract base. Seals `Initialize`/`SupportedDiagnostics`; exposes `InitializeAnalyzer(SafeAnalysisContext)` and `SupportedDiagnosticsCore`. Auto-appends the cop's `AnalyzerExceptionDescriptor` to `SupportedDiagnostics` and captures `GetType().Name` for the message. |
| `SafeAnalysisContext` | `AnalysisContext` decorator. Overrides every public-abstract `Register*` method to forward to the inner context with the callback wrapped in try/catch. |
| `SafeCompilationStartContext` | Same decoration for registrations nested inside `RegisterCompilationStartAction`. |
| `AnalyzerExceptionReporter` | Builds the `XX0000` diagnostic (`Diagnostic.Create(descriptor, location ?? Location.None, analyzerName, exceptionType, exceptionMessage)`). |
| `{Cop}Analyzer` (per cop) | Thin bridge supplying `AnalyzerExceptionDescriptor => DiagnosticDescriptors.AnalyzerException`. Common cannot reference per-cop descriptors, so this lives in each cop project. |

## Adoption recipe (3 mechanical edits, no `Register*` changes)

```csharp
// 1. base type
public sealed class MyRule : ApplicationCopAnalyzer   // was : DiagnosticAnalyzer

// 2. supported diagnostics
protected override ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCore { get; } = ...
//   was: public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics

// 3. initialize
protected override void InitializeAnalyzer(SafeAnalysisContext context) => ...
//   was: public override void Initialize(AnalysisContext context)
```

Keep the `[DiagnosticAnalyzer]` attribute. Do **not** list the cop's
`AnalyzerException` descriptor in `SupportedDiagnosticsCore`; the base appends it.

As of this change only `CaptionRequired` (ApplicationCop) is converted. The other
analyzers adopt the harness incrementally via this same recipe.

## Location strategy per context

| Context | Location |
|---|---|
| `SymbolAnalysisContext` | `Symbol.GetLocation()` |
| `SyntaxNodeAnalysisContext` | `Node.GetLocation()` |
| `CodeBlockAnalysisContext` | `CodeBlock.GetLocation()` |
| `OperationAnalysisContext` | `Operation.Syntax.GetLocation()` |
| Compilation / SemanticModel / SyntaxTree | `Location.None` (message still names the cop + rule) |

Use `symbol.GetLocation()` (SDK `SymbolExtensions`), **not** `Symbol.Locations`
— `ISymbol` has no `Locations` property in this SDK.

## Design notes / known constraints

- **Operation actions are special.** The public `params`
  `AnalysisContext.RegisterOperationAction` routes through *internal* members we
  cannot override. `SafeAnalysisContext` therefore **`new`-hides** both operation
  overloads and forwards to `inner.RegisterOperationAction(wrapped, kinds)`. This
  only wins because adopting analyzers type their `InitializeAnalyzer` parameter
  as `SafeAnalysisContext`. Converting an operation-based analyzer requires no
  call-site change beyond the 3-line recipe.
- **`CompilationStartAnalysisContext`** exposes only public-abstract methods and
  no operation registration, so `SafeCompilationStartContext` intercepts nested
  registrations via virtual dispatch even when the callback variable is typed as
  the SDK base type — no call-site changes in CompilationStart analyzers.
- **Cancellation propagates.** All wrappers use
  `catch (Exception ex) when (ex is not OperationCanceledException)`.
- **SDK coupling (accepted, documented).** `SafeAnalysisContext` subclasses the
  SDK's abstract `AnalysisContext`. If a future SDK adds a new **public-abstract**
  `Register*` method, this type fails to **compile** until an override is added.
  That compile-time break is intentional — it forces wrapping of any new surface.
  Forwarding uses only the public registration API every analyzer already calls.
- **netstandard2.1.** The harness uses only APIs present on all TFMs
  (`netstandard2.1;net8.0;net10.0`); no `#if` guards. Verified to build on all
  three.
- **Performance.** try/catch with no throw is effectively free; one delegate
  indirection per callback, built once at registration. Negligible against
  ~100k method bodies.

## Fallback (not built)

If the decorator ever proves troublesome, the contingency is extension helpers
(`Register*ActionSafe`) that wrap callbacks at the call site. They are robust and
simple but require renaming every `Register*` call and a per-file
`SupportedDiagnostics` edit. Documented here only; not implemented.

## Test coverage

`ALCops.ApplicationCop.Test/Rules/AnalyzerExceptionHarness/` exercises the shipped
wrapping paths with test-only throwing analyzers.

**HasDiagnostic (3 cases):** SymbolAction, OperationAction, CompilationStartAction.
