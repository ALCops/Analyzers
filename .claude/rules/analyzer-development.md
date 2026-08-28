---
paths:
  - "src/ALCops.*/Analyzers/**"
---

# ALCops Analyzer Development Guide

ALCops Analyzers is a collection of 6 AL code analyzers ("cops") for Business Central, built on the `Microsoft.Dynamics.Nav.CodeAnalysis` SDK. Each cop lives in its own project and targets a specific concern.

## NAV SDK Source Reference (MANDATORY)

If the directory `../nav-sdk-source` exists (relative to this repo root), you **MUST** use it as your primary reference when:
- Investigating SDK types, methods, or behaviors
- Understanding how syntax nodes, symbols, or operations work
- Looking for implementation patterns or examples
- Debugging build failures on specific TFMs
- Discovering available API surface

**This is not optional.** The nav-sdk-source repo contains the decompiled source code of the entire NAV/BC SDK and is the authoritative source of truth for the APIs we build against.

| Content | Why it matters |
|---------|---------------|
| **NAV CodeAnalysis SDK** | The syntax tree, symbol, semantic model, and diagnostics APIs our analyzers are built on. Every type we use is defined here. |
| **Microsoft CodeCops** (CodeCop, AppSourceCop, UICop, PerTenantExtensionCop) | Production-quality reference implementations of DiagnosticAnalyzers and CodeFixProviders. Shows proven patterns for callbacks, symbol resolution, and fix registration. |
| **Compiler and tooling** (alc, Deployment, EditorServices) | Reveals internal behaviors: how compilation works, how the editor invokes analyzers, callback ordering. |

Read **all** instruction files in `../nav-sdk-source/.github/instructions/` for navigation details, patterns, and conventions. List the directory first, then read what is relevant to the task.

## Diagnostic ID Conventions

Each cop uses a 2-letter prefix followed by a 4-digit number:

| Cop               | Prefix | Example |
|--------------------|--------|---------|
| ApplicationCop     | AC     | AC0001  |
| DocumentationCop   | DC     | DC0001  |
| FormattingCop      | FC     | FC0001  |
| LinterCop          | LC     | LC0003  |
| PlatformCop        | PC     | PC0001  |
| TestAutomationCop  | TA     | TA0001  |

IDs are `public static readonly string` fields in `DiagnosticIds.cs`:

```csharp
namespace ALCops.PlatformCop;

public static class DiagnosticIds
{
    public static readonly string EditableFlowField = "PC0001";
    public static readonly string AutoIncrementInTemporaryTable = "PC0002";
    // ...
}
```

The constant name is a PascalCase description of the rule. Take the next available number for the cop; check existing IDs first to avoid gaps or collisions.

## DiagnosticDescriptors

Each cop's `DiagnosticDescriptors.cs` constructs `DiagnosticDescriptor` instances referencing the ID from `DiagnosticIds`, messages from the `.resx` (via the auto-generated resource class), a category from the inner `Category` class, and a help URI on alcops.dev.

```csharp
using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor AutoIncrementInTemporaryTable = new(
        id: DiagnosticIds.AutoIncrementInTemporaryTable,
        title: PlatformCopAnalyzers.AutoIncrementInTemporaryTableTitle,
        messageFormat: PlatformCopAnalyzers.AutoIncrementInTemporaryTableMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.AutoIncrementInTemporaryTableDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.AutoIncrementInTemporaryTable));

    // Help URI format: https://alcops.dev/docs/analyzers/<copname>/<id>/
    public static string GetHelpUri(string identifier)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "https://alcops.dev/docs/analyzers/platformcop/{0}/",
            identifier.ToLower());
    }

    internal static class Category
    {
        public const string Design = "Design";
        public const string Naming = "Naming";
        public const string Style = "Style";
        public const string Usage = "Usage";
        public const string Performance = "Performance";
        public const string Security = "Security";
    }
}
```

Key points:
- The descriptor field name must match the `DiagnosticIds` constant name.
- `title` uses the `*Title` resource, `messageFormat` uses `*MessageFormat`, `description` uses `*Description`.
- `messageFormat` supports `{0}`, `{1}` placeholders filled via `Diagnostic.Create(...)`.
- `defaultSeverity` is typically `Warning`. Use `Error` for definite runtime failures, `Info` for suggestions/metrics.
- `isEnabledByDefault` is `true` for most rules; `false` for opt-in rules (metrics, opinionated style).
- The `GetHelpUri` format varies per cop: replace `platformcop` with `lintercop`, `applicationcop`, etc.

## Resource Files (.resx)

Diagnostic messages live in `ALCops.<CopName>Analyzers.resx`; the build generates a strongly-typed class (e.g., `PlatformCopAnalyzers`, `LinterCopAnalyzers`) from it.

```xml
<data name="AutoIncrementInTemporaryTableTitle" xml:space="preserve">
  <value>AutoIncrement fields are not supported in temporary tables</value>
</data>
<data name="AutoIncrementInTemporaryTableMessageFormat" xml:space="preserve">
  <value>AutoIncrement is used in a table with TableType = Temporary, which will cause a runtime error. Remove AutoIncrement or make the table non-temporary.</value>
</data>
<data name="AutoIncrementInTemporaryTableDescription" xml:space="preserve">
  <value>AutoIncrement relies on SQL Server to generate the next value when inserting records. Temporary tables are only in-memory in Business Central and are not created on SQL Server, so SQL Server cannot generate AutoIncrement values. This results in runtime failures when code inserts into the temporary table.</value>
</data>
```

Required entries per rule:
- `<RuleName>Title` - short summary (the diagnostic title)
- `<RuleName>MessageFormat` - message shown to the user; may contain `{0}`, `{1}` placeholders
- `<RuleName>Description` - detailed explanation of why the rule exists

Optional:
- `<RuleName>CodeAction` - display text for an associated code fix (e.g., `ALCops: Remove redundant ApplicationArea`)

Parameterized messages use standard .NET format strings:
```xml
<data name="AccessPropertyExplicitlySetMessageFormat" xml:space="preserve">
  <value>{0} '{1}' does not explicitly have the Access property set.</value>
</data>
```

## Analyzer Class Pattern

All analyzers are decorated with `[DiagnosticAnalyzer]`.

> **Exception harness (XX0000).** Analyzers may instead derive from the per-cop
> bridge `{Cop}Analyzer` (for example `ApplicationCopAnalyzer`) so that an
> unhandled exception becomes a located `XX0000` diagnostic instead of `AD0001`
> on `app.json`. Adoption is a 3-line change: base type `: DiagnosticAnalyzer` →
> `: {Cop}Analyzer`, `SupportedDiagnostics` → `SupportedDiagnosticsCore`, and
> `Initialize(AnalysisContext)` → `InitializeAnalyzer(SafeAnalysisContext)` (no
> `Register*` changes). **Currently no production analyzer adopts the harness**
> (`CaptionRequired` was temporarily detached to fix issue #389 — the Common-based
> bridge made `alc` fail with `AL1003` at type-load; re-adoption needs a loader-safe
> fix first). See `.claude/rules/analyzer-exception-harness.md`. The template below
> shows the still-supported (and currently used) plain `DiagnosticAnalyzer` form.

### Minimal Template (Symbol-based)

```csharp
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.<CopName>.Analyzers;

[DiagnosticAnalyzer]
public sealed class MyNewAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MyNewRule);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            this.AnalyzeSymbol,
            EnumProvider.SymbolKind.Table  // register for relevant symbol kinds
        );

    private void AnalyzeSymbol(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())  // always skip obsolete symbols
            return;

        // Analysis logic here...

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MyNewRule,
            ctx.Symbol.GetLocation(),
            arg0, arg1));  // args fill {0}, {1} in messageFormat
    }
}
```

### Registration Methods

| Method | Context Type | Use When |
|--------|-------------|----------|
| `RegisterSymbolAction` | `SymbolAnalysisContext` | Analyzing declared symbols (tables, pages, fields, methods). Most common. |
| `RegisterOperationAction` | `OperationAnalysisContext` | Analyzing operations in method bodies (invocations, assignments). |
| `RegisterSyntaxNodeAction` | `SyntaxNodeAnalysisContext` | Analyzing specific syntax nodes (string literals, identifiers). |
| `RegisterCodeBlockAction` | `CodeBlockAnalysisContext` | Analyzing entire method/trigger bodies (complexity metrics). |
| `RegisterCompilationAction` | `CompilationAnalysisContext` | Analyzing the full compilation (manifest checks, cross-object analysis). |
| `RegisterCompilationStartAction` | `CompilationStartAnalysisContext` | Registering additional actions at compilation start (multi-pass analysis). |

### CompilationStart + SymbolAction (expensive resource loading)

When an analyzer needs to load expensive resources once per compilation (XLIFF files, settings, cross-object indexes), load them in `RegisterCompilationStartAction`, then register per-symbol actions that use the cached data:

```csharp
public override void Initialize(AnalysisContext context) =>
    context.RegisterCompilationStartAction(CompilationStart);

private void CompilationStart(CompilationStartAnalysisContext ctx)
{
    // 1. Load expensive resources once
    var fileSystem = ctx.Compilation.FileSystem;
    if (fileSystem is null)
        return;

    var settings = ALCopsSettingsProvider.GetSettings(fileSystem.GetDirectoryPath());

    // 2. Build index / cache
    var translationIndex = BuildIndex(fileSystem);
    if (translationIndex is null)
        return;

    // 3. Register per-symbol action that uses the cached data
    ctx.RegisterSymbolAction(
        symbolCtx => AnalyzeSymbol(symbolCtx, translationIndex, settings),
        EnumProvider.SymbolKind.Table,
        EnumProvider.SymbolKind.Page);
}
```

Key points:
- Exit early (don't register the symbol action) if resource loading fails or returns no data; the rule becomes a no-op for projects without the resource.
- Pass loaded data via lambda captures or a state object, not instance fields (analyzers should be stateless).
- `Compilation.FileSystem` returns `null` when no file system is available (e.g., some test or IDE contexts).

### Symbol Analysis (most common)

Registers for symbol kinds via `EnumProvider.SymbolKind.*`:

```csharp
public override void Initialize(AnalysisContext context) =>
    context.RegisterSymbolAction(
        this.AnalyzeAccessProperty,
        EnumProvider.SymbolKind.Codeunit,
        EnumProvider.SymbolKind.Table,
        EnumProvider.SymbolKind.Page
    );

private void AnalyzeAccessProperty(SymbolAnalysisContext ctx)
{
    if (ctx.IsObsolete())
        return;

    if (ctx.Symbol.GetProperty(EnumProvider.PropertyKind.Access) is not null)
        return;

    ctx.ReportDiagnostic(Diagnostic.Create(
        DiagnosticDescriptors.AccessPropertyExplicitlySet,
        ctx.Symbol.GetLocation(),
        ctx.Symbol.Kind.ToString(),
        ctx.Symbol.Name));
}
```

### Operation Analysis (method invocations)

Registers for operation kinds via `EnumProvider.OperationKind.*`:

```csharp
public override void Initialize(AnalysisContext context) =>
    context.RegisterOperationAction(
        AnalyzeInvocation,
        EnumProvider.OperationKind.InvocationExpression);

private void AnalyzeInvocation(OperationAnalysisContext ctx)
{
    if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
        return;

    if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
        !SemanticFacts.IsSameName(invocation.TargetMethod.Name, "Commit"))
        return;

    ctx.ReportDiagnostic(Diagnostic.Create(
        DiagnosticDescriptors.CommitRequiresComment,
        ctx.Operation.Syntax.GetLocation()));
}
```

### Syntax Node Analysis (raw syntax tree)

Registers for syntax kinds via `EnumProvider.SyntaxKind.*`:

```csharp
public override void Initialize(AnalysisContext context) =>
    context.RegisterSyntaxNodeAction(
        AnalyzeStringLiteral,
        EnumProvider.SyntaxKind.StringLiteralValue);

private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext ctx)
{
    if (ctx.IsObsolete() || ctx.Node is not StringLiteralValueSyntax stringLiteral)
        return;

    // Use ctx.SemanticModel for symbol resolution
    // Use ctx.ContainingSymbol for the enclosing symbol
    // Use ctx.Node for the syntax tree

    ctx.ReportDiagnostic(Diagnostic.Create(
        DiagnosticDescriptors.MyRule,
        ctx.Node.GetLocation(),
        args));
}
```

### Record method receiver forms (syntax level)

When detecting AL record (table data) method calls at the **syntax level**, a call like `Modify` can reach a record through **four** receiver forms. Analyzers that pattern-match only some of them produce false positives/negatives (see issue #343, AC0032):

| Form | Example | Syntax shape | How to resolve the record/table |
|---|---|---|---|
| Named variable | `MyTable.Modify()` | `MemberAccessExpressionSyntax` with `IdentifierNameSyntax` receiver | Variable map (locals/params/globals) or `GetSymbolInfo` on the receiver |
| Implicit `Rec` | `Rec.Modify()` | same as above (`Rec` is a normal identifier) | same as named variable |
| Bare implicit self | `Modify()` | `InvocationExpressionSyntax` whose `Expression` is `IdentifierNameSyntax` (no receiver) | The containing object: a table object's declared symbol is an `ITableTypeSymbol` |
| `this` self-reference | `this.Modify()` | `MemberAccessExpressionSyntax` whose receiver is the AL `this` keyword (runtime 14.0+) | `SemanticModel.GetOperation(receiverNode)?.Type` (in a table, `this` binds to the record). Detect it as "any non-`IdentifierNameSyntax` receiver" -- do **not** pattern-match `ThisExpressionSyntax` (absent at the netstandard2.1 floor) |

Key symbol-shape gotcha: a **table object's declared symbol is `ITableTypeSymbol`, which is NOT an `IRecordTypeSymbol`**. The record (`Rec`/`this`) is a separate `IRecordTypeSymbol` wrapper whose `OriginalDefinition` is the `ITableTypeSymbol`. Guard with `is ITableTypeSymbol` for the object/bare-self and `is IRecordTypeSymbol` for variable/`this` receivers.

Tests that exercise the `this` form must guard on runtime version 14.0 (e.g. `SkipTestIfVersionIsTooLow([...], testCase, "14.0", ...)`), since `this` is a Fall 2024 feature.

**Do not** reference `ThisExpressionSyntax` directly (and therefore do not `#if !NETSTANDARD2_1`-guard `this` handling). The public `ThisExpressionSyntax` type, the `SyntaxKind.ThisExpression` enum member, and `IInstanceReferenceOperation` are all **absent** from the netstandard2.1 compile floor (AL 12.0.13, predating the Fall 2024 `this` feature), so a `ThisExpressionSyntax` pattern-match forces a guard that silently drops `this` detection on the netstandard2.1 binary -- which is exactly what serves AL 14.0-15.2 (they ship a netstandard2.0 SDK). Instead, resolve the receiver via the **operation tree**, which is fully available at the floor (`GetOperation`, `IOperation`, `IOperation.Type`, `IInvocationExpression.Instance`):

```csharp
// Works on every TFM and AL version; never names ThisExpressionSyntax.
if (receiverExpression is not null && receiverExpression is not IdentifierNameSyntax)
{
    var receiverType = ctx.SemanticModel.GetOperation(receiverExpression, ct)?.Type;  // `this` binds to the record
    // ... build the permission from receiverType
}
```

This is the same mechanism AC0031 (`RequiredPermissionDetector.TryGetFromInvocation`) uses via `invocation.Instance.Type`. Keep the variable-map fast path first so `GetOperation` (the ~0.3ms call) only runs for the rare non-identifier receivers.

### Detecting `this`/self at the operation level (`OperationKind.ThisReference`)

When you already hold the bound `IOperation` (e.g. `IFieldAccess.Instance` inside a `RegisterOperationAction`) rather than syntax, detect a `this`/self reference via the **`OperationKind` enum**, not the `IInstanceReferenceOperation` type:

```csharp
// Works on every TFM. Never names IInstanceReferenceOperation.
var thisReferenceKind = EnumProvider.OperationKind.ThisReference;
if (thisReferenceKind != default && instance.Kind == thisReferenceKind)
    return true; // `this`/self
```

`IInstanceReferenceOperation`, `ThisExpressionSyntax`, and `SyntaxKind.ThisExpression` are all absent from the netstandard2.1 compile floor (AL 12.0.13). Referencing the interface forces an `#if !NETSTANDARD2_1` guard that silently drops `this.` detection on the netstandard2.1 binary that serves AL 14.0–15.2. `EnumProvider.OperationKind.ThisReference` resolves to `default` (the enum's `None`) on SDKs without the member — where no `this` code can exist anyway — so the `!= default` guard makes it a no-op there and exact elsewhere. Add the member to `EnumProvider` with the string-literal `ParseEnum<…>("ThisReference")` form (like `CompoundAssignmentStatement`), since `nameof(OperationKind.ThisReference)` will not compile at the floor.

Note that `this` yields a record-typed symbol (`BoundThisReference.ExpressionSymbol => Type`), so `GetSymbolSafe().Name` is the table name, **not** `"Rec"`. To distinguish the current record from the `xRec` before-image (both share the same record type), compare the global's name to the reserved keyword `"Rec"` — the `IsThis`/`HasImplicitWith` flags that the compiler uses live on the internal `SynthesizedGlobalVariableSymbol` and are not publicly reachable. See PC0037 `UseValidateForFieldAssignment.IsCurrentRecordInstance`.

## EnumProvider (Critical Pattern)

Never reference `Microsoft.Dynamics.Nav.CodeAnalysis` enum values directly. Always use `EnumProvider` from `ALCops.Common.Reflection`, which provides backward compatibility across SDK versions via reflection-based caching.

```csharp
// CORRECT
EnumProvider.SymbolKind.Table
EnumProvider.PropertyKind.Access
EnumProvider.MethodKind.BuiltInMethod
EnumProvider.SyntaxKind.StringLiteralValue

// WRONG - will break across SDK versions
SymbolKind.Table
PropertyKind.Access
```

Each `EnumProvider` nested class exposes a `CanonicalNames` property (`Lazy<ImmutableDictionary<string, string>>`) mapping case-insensitive enum value names to their canonical form. These are generated from `Enum.GetNames()` at runtime, so they are self-maintaining across SDK versions.

## Semantic Model APIs

The `SemanticModel` provides symbol resolution beyond pure syntax analysis. Use it for canonical names, type information, or cross-object references.

### Getting the SemanticModel

```csharp
// From SymbolAnalysisContext (most common)
var root = ctx.Symbol.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken);
var semanticModel = ctx.Compilation.GetSemanticModel(root.SyntaxTree);

// From SyntaxNodeAnalysisContext
var semanticModel = ctx.SemanticModel;
```

### GetSymbolInfo vs GetDeclaredSymbol

| Method | Purpose | Returns |
|--------|---------|---------|
| `GetSymbolInfo(node)` | Resolves a **reference** to a symbol | The symbol being referenced |
| `GetDeclaredSymbol(node)` | Gets the symbol **declared by** a node | The declared symbol itself |

### Resolution Matrix

Not all syntax node types resolve via the semantic model:

| Syntax Node Type | GetDeclaredSymbol | GetSymbolInfo | Returns Canonical? | Recommended Strategy |
|---|---|---|---|---|
| `PropertySyntax` | ✅ `IPropertySymbol` | ❌ | `PropertyKind`=canonical, `Value`/`ValueText`=SOURCE | `GetDeclaredSymbol` → `PropertyKind.ToString()` |
| `PropertyNameSyntax` | ❌ | ✅ `IPropertySymbol` | `PropertyKind`=canonical, `Name`=SOURCE | `GetSymbolInfo` → `PropertyKind.ToString()` |
| `EnumPropertyValueSyntax` | ❌ | ❌ | N/A | Dictionary lookup (see Dynamic SDK Discovery) |
| `SimpleNamedDataTypeSyntax` | ❌ | ❌ | N/A | `NavTypeKind` dictionary lookup |
| `MemberAttributeSyntax` | ❌ | ❌ | N/A | `AttributeKind.CanonicalNames` dictionary |
| `IdentifierNameSyntax` | ❌ | ✅ various | `Name`=canonical | `GetSymbolInfo` → `ISymbol.Name` |
| `QualifiedNameSyntax` | ❌ | ✅ various | `Name`=canonical | `GetSymbolInfo` → `ISymbol.Name` |
| `TriggerDeclarationSyntax` | ✅ `IMethodSymbol` | ❌ | `Name`=canonical | `GetDeclaredSymbol` → `IMethodSymbol.Name` |
| `FieldSyntax` | ✅ `IFieldSymbol` | ❌ | `Name`=SOURCE (user-defined) | N/A (user-defined names) |

### IPropertySymbol Deep-Dive

`IPropertySymbol` (from `GetDeclaredSymbol` on `PropertySyntax` or `GetSymbolInfo` on `PropertyNameSyntax`) has three key properties:

| Property | Returns | Example (source: `ACCESS = Public`) |
|---|---|---|
| `PropertyKind` | Canonical `PropertyKind` enum value | `PropertyKind.Access` |
| `PropertyKind.ToString()` | Canonical property name string | `"Access"` |
| `Value` | Source-text value (runtime type varies) | `SourceOptionSymbol` with `.ToString()` = `"Public"` |
| `ValueText` | Source-text value as string | `"Public"` (matches source, NOT necessarily canonical) |

**Critical**: `Value` and `ValueText` return **source text**, not canonical form. For enum property values, `Value` is a `SourceOptionSymbol` (internal type implementing `IOptionSymbol`), and its `.ToString()` also returns source text. Only `PropertyKind.ToString()` is reliable for canonical form.

### Identifier Resolution Batching

When resolving many identifiers via `GetSymbolInfo`, batch them:

```csharp
// Group identifiers by their text, resolve one representative per group
var groups = identifiers
    .ToLookup(node => node.Identifier.ValueText, StringComparer.Ordinal);

foreach (var group in groups)
{
    var representative = group.OrderBy(n => n.Position).Last();

    if (semanticModel.GetSymbolInfo(representative, ct).Symbol is not ISymbol symbol)
        continue;

    // Apply the canonical name to ALL nodes in the group
    foreach (var node in group)
        CompareIdentifier(ctx, node.Identifier, symbol.Name);
}
```

This avoids redundant `GetSymbolInfo` calls for identifiers that reference the same symbol.

## Operation-Level Symbol Resolution

Inside `RegisterOperationAction`, the bound operation tree already has symbols resolved. Prefer `IOperation.GetSymbolSafe()` over `SemanticModel.GetSymbolInfo()` there.

### `IOperation.GetSymbolSafe()` vs `SemanticModel.GetSymbolInfo()`

| Method | Context | Cost | Use When |
|---|---|---|---|
| `IOperation.GetSymbolSafe()` | Operation analysis | O(1) with type guard | You have an `IOperation` (invocation instance, argument value, field access) |
| `SemanticModel.GetSymbolInfo(node)` | Syntax analysis | Performs semantic resolution | You have a `SyntaxNode` and no operation tree |

**IMPORTANT: Always use `GetSymbolSafe()` instead of `GetSymbol()`.** The SDK's `GetSymbol()` crashes with `InvalidCastException` on `BoundApplicationObjectAccess` (`DATABASE::X`, `CODEUNIT::X`) and `BoundObjectAccess` because they report `Kind = FieldAccess` but don't implement `IFieldAccess`. See "SDK GetSymbol() Bug" below.

```csharp
// In an OperationAnalysisContext callback:

// Resolve the instance of a method invocation
var instanceSymbol = invocation.Instance.GetSymbolSafe();

// Resolve an argument's value
var argumentSymbol = argument.Value.GetSymbolSafe();

// Compare two symbols by identity (same variable, same declaration)
if (instanceSymbol is not null && instanceSymbol.Equals(argumentSymbol))
    // same symbol
```

### Unwrapping `IConversionExpression`

The SDK frequently wraps argument values in `IConversionExpression` for implicit type conversions (e.g., passing a `Record "Sales Header"` where the parameter type is `Record "Sales Header"`). Then `argument.Value.GetSymbolSafe()` returns null because the conversion itself has no symbol. Unwrap to reach the operand's symbol:

```csharp
private static ISymbol? ResolveArgumentSymbol(IArgument argument)
{
    var symbol = argument.Value.GetSymbolSafe();
    if (symbol is not null)
        return symbol;

    if (argument.Value is IConversionExpression conversion)
        return conversion.Operand.GetSymbolSafe();

    return null;
}
```

This pattern appears across the codebase (e.g., `TransferFieldsSchemaCompatibility`, `PossibleOverflowAssigning`, `PartialRecordOperations`, `UnnecessaryRecordParameterInMethodCall`). Always try unwrapping when `GetSymbolSafe()` returns null on an argument value.

## Dynamic SDK Discovery via PropertyInfoLookup

The SDK's `PropertyInfoLookup` class queries property metadata at runtime, enabling self-maintaining enum property value resolution without manual dictionaries.

### How It Works

`PropertyInfoLookup.Lookup(SymbolKind, PropertyKind)` is a **public static method** returning a `PropertyTypeInfo`. For properties that accept enum values, the result is an `EnumPropertyTypeInfo` (internal subclass) with an `Options` property containing `EnumPropertyMemberInfo` objects. Each `EnumPropertyMemberInfo.Name` is the **canonical form** of the option value.

```
PropertyInfoLookup.Lookup(SymbolKind.Codeunit, PropertyKind.Access)
  → EnumPropertyTypeInfo
    → Options: [EnumPropertyMemberInfo { Name="Public" }, EnumPropertyMemberInfo { Name="Internal" }]

PropertyInfoLookup.Lookup(SymbolKind.Page, PropertyKind.PageType)
  → EnumPropertyTypeInfo
    → Options: [{ Name="Card" }, { Name="List" }, { Name="RoleCenter" }, ...]
```

### Building a Self-Maintaining Dictionary

Since `EnumPropertyTypeInfo` and `EnumPropertyMemberInfo` are internal types, access them via reflection. The pattern iterates all `SymbolKind × PropertyKind` combinations at startup and merges options per `PropertyKind`. See `PropertyAccessor.cs` in `ALCops.Common/Reflection/` for the full implementation.

### Key Facts

- Discovers **60 PropertyKinds** with **253 unique option values** from the current SDK
- **Zero case collisions** across all option names (verified)
- **6 PropertyKinds** produce different options depending on SymbolKind: `Access`, `AllowInCustomizations`, `ObsoleteState`, `Scope`, `Subtype`, `Type`. Merging across all SymbolKinds handles this.
- New enum properties added in future AL SDK versions are **automatically discovered**
- All reflection uses `Lazy<T>` with `LazyThreadSafetyMode.PublicationOnly` per project conventions
- Wrap assembly type scanning in `try/catch (ReflectionTypeLoadException)` since some SDK types may fail to load (e.g., `System.Text.Json` version mismatch)

### When to Use This Pattern

Use `PropertyInfoLookup`-based discovery for canonical forms of **enum property values** (e.g., `Access = Public`, `PageType = Card`). For other canonical name needs:
- **Property names**: `IPropertySymbol.PropertyKind.ToString()` (semantic model, no dictionary needed)
- **Data type names**: `NavTypeKind` enum via `Enum.GetNames()` (self-maintaining)
- **Attribute names**: `EnumProvider.AttributeKind.CanonicalNames` (self-maintaining)
- **Symbol kind names**: `SymbolKind` enum via `Enum.GetNames()` (self-maintaining)
- **User-defined identifiers**: `GetSymbolInfo()` → `ISymbol.Name` (IntelliSense-equivalent)

## Common Utilities (ALCops.Common)

Browse `src/ALCops.Common/Extensions|Helpers|Reflection` before writing a helper; prefer existing ones.

### ManifestHelper in test contexts

`ManifestHelper.GetManifest` loads the `Microsoft.Dynamics.Nav.Analyzers.Common` assembly via reflection. In test contexts (minimal compilations without the full SDK runtime) this assembly isn't available, causing a `FileNotFoundException`. Analyzers calling it must handle this:

```csharp
NavAppManifest? manifest = null;
try
{
    manifest = ManifestHelper.GetManifest(ctx.Compilation);
}
catch (FileNotFoundException)
{
    // Expected in test contexts where Microsoft.Dynamics.Nav.Analyzers.Common isn't available
}
```

`CompilationWithAnalyzers` silently swallows all exceptions from analyzer callbacks, making this extremely hard to diagnose without explicit try-catch. The analyzer just appears to produce no diagnostics.

### Same-Module Checks via `ContainingModule`

When a rule should only flag calls to methods defined in the developer's own app (not dependencies or platform), compare `ContainingModule` using object equality. Do not compare app IDs or names as strings.

```csharp
private static bool IsInCurrentModule(OperationAnalysisContext ctx, IMethodSymbol targetMethod)
{
    var currentModule = ctx.ContainingSymbol.ContainingModule;
    var targetModule = targetMethod.ContainingModule;

    if (currentModule is null || targetModule is null)
        return false;

    return currentModule == targetModule;
}
```

`ContainingModule` is available on any `ISymbol`. `==` performs reference equality on the module objects, which is reliable because the compiler creates one module instance per app. This is simpler, faster, and immune to formatting differences compared to string-based app ID comparison.

### Version-Gating Analyzers

If a rule only applies to certain BC runtime versions, override `SupportedVersions`:

```csharp
public override VersionCompatibility SupportedVersions =>
    VersionProvider.VersionCompatibility.Fall2024OrGreater;
```

## Writing Tests

See `.claude/rules/testing.md`.

## Adding a New Rule

Use the `/new-analyzer` skill.

## Gathering All Extensions of a Kind (ConditionalWeakTable Cache Pattern)

When an analyzer needs all extension objects (page extensions, table extensions) targeting a specific base object, use a `ConditionalWeakTable<Compilation, CacheEntry>` to lazily cache the full set once per compilation. This avoids repeated expensive `GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection()` calls.

### Pattern

```csharp
// Cache all extensions per Compilation (lazy, thread-safe, GC-friendly)
private static ImmutableArray<IPageExtensionBaseTypeSymbol> GetCachedPageExtensions(Compilation compilation)
    => PageExtensionsCache.GetValue(compilation, static c => new PageExtensionsCacheEntry(c)).Value.Value;
private static readonly ConditionalWeakTable<Compilation, PageExtensionsCacheEntry> PageExtensionsCache = new();
private sealed class PageExtensionsCacheEntry(Compilation compilation)
{
    public Lazy<ImmutableArray<IPageExtensionBaseTypeSymbol>> Value { get; } =
        new Lazy<ImmutableArray<IPageExtensionBaseTypeSymbol>>(
            () => compilation
                .GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(EnumProvider.SymbolKind.PageExtension)
                .OfType<IPageExtensionBaseTypeSymbol>()
                .ToImmutableArray(),
            LazyThreadSafetyMode.ExecutionAndPublication);
}
```

### Matching extensions to base objects

Use `SameApplicationObject()` to compare extension targets with the base object. This handles cross-module symbols where reference equality fails:

```csharp
private static bool SameApplicationObject(ISymbol? source, ISymbol? target)
{
    if (source is null || target is null)
        return false;

    source = source.OriginalDefinition;
    target = target.OriginalDefinition;

    if (ReferenceEquals(source, target))
        return true;

    if (source is ISymbolWithId lId && target is ISymbolWithId rId)
        return lId.Id == rId.Id && source.Kind == target.Kind;

    return source.Equals(target);
}
```

### When to use

- Page extensions needing controls from sibling extensions targeting the same page (e.g., `DuplicateODataEntityName` for OData name collision)
- Table extensions needing fields from sibling extensions targeting the same table (e.g., `TransferFieldsSchemaCompatibility` for field schema comparison)
- Any cross-object analysis enumerating all extensions of a given `SymbolKind`

### Key points

- `ConditionalWeakTable` keys on the `Compilation`, so the cache is GC'd when the compilation is released
- The inner `Lazy<T>` with `ExecutionAndPublication` ensures single computation per compilation, even under concurrent `RegisterSymbolAction` callbacks
- `GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection` retrieves symbols from all referenced modules, not just the current one
- Always use `OriginalDefinition` when comparing symbols from different contexts (extension target vs base object)
- Requires `using System.Runtime.CompilerServices;`
- **Only use `ConditionalWeakTable<Compilation, ...>` within a single action type.** The `Compilation` instance from `SemanticModel.Compilation` (in `CodeBlockAction`, `OperationAction`, `SyntaxNodeAction`) is a **different object** from `ctx.Compilation` in `CompilationAction` or `CompilationStartAction`. `ConditionalWeakTable` uses reference equality, so data written by one action type is invisible to another. To share state across action types, use `CompilationStartAction` closures instead (see "Performance Anti-Patterns").

### Existing implementations

| File | Extension kind | Purpose |
|---|---|---|
| `TransferFieldsSchemaCompatibility.cs` | `TableExtension` | Gathers table extension fields for schema comparison |
| `DuplicateODataEntityName.cs` | `PageExtension` | Gathers sibling page extension controls for OData name collision detection |

## Common Pitfalls

- **Always use `EnumProvider`** for SDK enum values. Direct references break across BC versions.
- **Never compare property values as strings.** Use `ISymbol.GetEnumPropertyValue<T>(EnumProvider.PropertyKind.X)` to get typed enum values, then compare against `EnumProvider` constants. String comparisons against `ValueText` are fragile and bypass the type system:
  ```csharp
  // WRONG - fragile string comparison
  var prop = symbol.Properties.FirstOrDefault(p => p.PropertyKind == EnumProvider.PropertyKind.Subtype);
  if (prop?.ValueText == "Test") ...

  // CORRECT - typed enum comparison
  var subtype = symbol.GetEnumPropertyValue<CodeunitSubtypeKind>(EnumProvider.PropertyKind.Subtype);
  if (subtype == EnumProvider.CodeunitSubtypeKind.Test) ...
  ```
  Similarly, use `ISymbol.GetBooleanPropertyValue()` for boolean properties and `ISymbol.GetProperty()` for the `IPropertySymbol` itself. Add missing enum values to `EnumProvider` when needed.
- **Always check `IsObsolete()` first** in every analysis method (available on `SymbolAnalysisContext`, `OperationAnalysisContext`, `SyntaxNodeAnalysisContext`, `CodeBlockAnalysisContext`). Reporting on obsolete code creates noise.
- **Never compare raw syntax text to identify symbols.** Do not use `syntax.ToString()`, `node.Identifier.ValueText`, or similar text-based checks to determine what a variable, method, or expression refers to. These are fragile (case-sensitive, whitespace-dependent, miss implicit conversions) and wrong when the source text doesn't match the canonical symbol name. Resolve the symbol via `IOperation.GetSymbolSafe()`, `SemanticModel.GetSymbolInfo()`, or `SemanticModel.GetDeclaredSymbol()`, then compare via symbol identity (`symbol.Equals(other)`) or symbol properties (`symbol.Kind`, `symbol.Name`). Checking `ISymbol.Name` after resolution is acceptable because it is the compiler-resolved canonical form.
  - **Exception: when you must fall back to syntax text** (e.g., extracting a variable name from `IConversionExpression.Syntax` where `GetSymbol()` returns null), always call `.UnquoteIdentifier()` on the `ValueText` before comparing against symbol names. AL identifiers can be quoted (`"My Table"`); `ValueText` preserves quotes while `ISymbol.Name` strips them. Missing `UnquoteIdentifier()` causes silent failures on any quoted identifier. The extension lives in `Microsoft.Dynamics.Nav.CodeAnalysis.Utilities`.
- **Honor AL variable scoping in name-keyed variable maps.** AL resolves a bare identifier to the innermost scope: locals, parameters, and named return values **shadow** global (object-scope) variables of the same name. Analyzers that build name-keyed fast-path maps (e.g. `Dictionary<string, IRecordTypeSymbol>` or `HashSet<string>` of variable names) must consult the **entire local scope before any object-scope lookup**. Splitting one scope across multiple collections (e.g. a record map plus a RecordRef name set) makes this easy to get wrong: checking both RecordRef sets before the local record map caused a false whole-object bailout in AC0032 when a local `RecordRef: Record X` shadowed a global `RecordRef: RecordRef` (fixed in PR #448; pinned by the `RecordRefNameShadowedByLocalRecord` fixture). Two rules of thumb:
  1. Classify variables by **symbol type** (`IVariableSymbol.Type`), never by variable name.
  2. Lookup order: all local-scope collections first (in any order — names are unique within a method), then all object-scope collections.

  Also beware the exclusion-vs-absence trap: when a scope's map deliberately excludes some variables (e.g. temporary records), a shadowed excluded local falls through to the object scope and wrongly resolves to the global. Bound-symbol resolution (`IOperation` / `GetSymbolInfo`) is immune to all of this; name maps are a performance optimization that must replicate the compiler's scoping rules.
- **Use `CancellationToken`** via `ctx.CancellationToken.ThrowIfCancellationRequested()` in loops over large collections (e.g., all fields in a table).
- **Always use the `SemanticFacts` name comparison API for AL identifiers.** AL is case-insensitive. Use the appropriate `SemanticFacts` member from `Microsoft.Dynamics.Nav.CodeAnalysis` instead of raw `StringComparison.OrdinalIgnoreCase` / `StringComparer.OrdinalIgnoreCase`. This makes intent explicit and matches Microsoft's own CodeCops.

  | Scenario | Use | Instead of |
  |----------|-----|------------|
  | Direct equality (non-null) | `SemanticFacts.IsSameName(a, b)` | `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` or `a.Equals(b, StringComparison.OrdinalIgnoreCase)` |
  | Direct equality (nullable) | `a.IsSameName(b)` (extension from `ALCops.Common.Extensions`) | `is { } x && SemanticFacts.IsSameName(x, ...)` null-guard pattern |
  | Collection comparer | `SemanticFacts.NameEqualityComparer` | `StringComparer.OrdinalIgnoreCase` in HashSet/Dictionary/ImmutableHashSet/GroupBy/ToLookup |
  | Substring/prefix/suffix | `SemanticFacts.NameEqualityComparison` | `StringComparison.OrdinalIgnoreCase` in StartsWith/EndsWith/Contains/IndexOf |
  | Sorting | `SemanticFacts.NameComparer` | `StringComparer.OrdinalIgnoreCase` in OrderBy/Sort |

  **Nullable inputs:** When comparing values from `SyntaxToken.ValueText` (`string?`), use the `IsSameName` extension from `ALCops.Common.Extensions.StringExtensions`. It returns `false` when either argument is null, avoiding the verbose `is { } varName &&` pattern.

  **When NOT to use SemanticFacts (keep OrdinalIgnoreCase):**
  - Property value comparisons (enum values like "Always", "Never", "#All")
  - File path or assembly location comparisons
  - Non-AL text (diagnostic IDs, translation keys, manifest metadata, punctuation)
  - User-configured strings (affix lists from alcops.json settings)
  - Permission character strings (e.g., searching "RIMD" for a permission char)
  - Permission sort keys (`NaturalStringComparer` deliberately uses `InvariantCultureIgnoreCase` to match AZ AL Dev Tools; see the FC0004 rule doc)

  ```csharp
  // Direct equality - comparing AL method name (both non-null)
  if (SemanticFacts.IsSameName(targetMethod.Name, "SetRecord"))

  // Direct equality - nullable source (e.g. SyntaxToken.ValueText)
  if (attr.Name.Identifier.ValueText.IsSameName("IntegrationEvent"))

  // Collection of AL method names
  private static readonly HashSet<string> Methods = new(SemanticFacts.NameEqualityComparer)
  {
      "Find", "FindFirst", "FindLast", "FindSet"
  };

  // Substring check on AL identifier name
  if (field.Name.Contains("no", SemanticFacts.NameEqualityComparison))

  // Sorting AL identifier names
  fields.Sort(SemanticFacts.NameComparer);
  ```
- **Mark analyzer classes `sealed`** unless there is a specific reason for inheritance.
- **One analyzer class per rule or tightly related rule group.** `AnalyzeCountMethod` handles both `LC0081` (IsEmpty) and `LC0082` (FindWithNext) because they share the same analysis logic.
- **Use `ImmutableArray.Create()`** for `SupportedDiagnostics`, listing all descriptors the analyzer may report.
- **Location matters.** Report at the most specific location: the offending property, syntax node, or symbol, not the entire object.

## Performance Anti-Patterns

The Base Application has ~7,900 files and ~100,000 method/trigger bodies. A rule that adds 0.1ms per method adds 10 seconds to compilation. The following anti-patterns have caused real regressions.

### DO NOT use `RegisterOperationAction` for high-frequency invocations

`RegisterOperationAction(callback, OperationKind.InvocationExpression)` fires once per invocation expression in the entire compilation, each carrying SDK action-dispatch overhead. For the Base Application that is ~115,000 callbacks for invocation expressions alone. If your analyzer only cares about invocations in methods meeting certain criteria (e.g., the containing object has a `Permissions` property), most callbacks are wasted.

**Instead:** Use `RegisterCodeBlockAction` to get the full method body, apply cheap pre-filters first, then call `GetOperation(body)` once per qualifying method and walk the operation tree with an `OperationWalker` subclass. One semantic call per method instead of one per invocation.

```csharp
// WRONG - fires for every invocation in the entire compilation
context.RegisterOperationAction(
    AnalyzeInvocation,
    EnumProvider.OperationKind.InvocationExpression);

// BETTER - one callback per method, pre-filter before expensive work
compilationCtx.RegisterCodeBlockAction(ctx =>
{
    if (ctx.CodeBlock is not MethodOrTriggerDeclarationSyntax method)
        return;

    // Cheap pre-filter: skip methods in objects that can't produce diagnostics
    var obj = ctx.OwningSymbol?.GetContainingApplicationObjectTypeSymbol();
    if (obj?.GetProperty(EnumProvider.PropertyKind.Permissions) is null)
        return;

    // Walk the operation tree for this method body
    var operation = ctx.SemanticModel.GetOperation(method.Body, ctx.CancellationToken);
    if (operation is null)
        return;

    var walker = new MyOperationWalker(/* ... */);
    walker.Visit(operation);
});
```

See `PartialRecordOperations.cs` and `TableDataAccessUnusedPermissions.cs` for real examples.

### DO NOT call `GetOperation()` without pre-filtering

`SemanticModel.GetOperation()` costs ~0.05-0.1ms per call. Across 100K methods, that is 5-10 seconds. Most methods will not contain the patterns your analyzer cares about.

**Pre-filter at the syntax level first.** Walk the (already parsed, free) syntax tree looking for relevant invocation names, node kinds, or keywords. Only call `GetOperation()` if the syntax scan finds a potential match.

```csharp
// Scan syntax tree for DB method names before expensive GetOperation
bool hasPossibleMatch = false;
body.WalkDescendantsAndPerformAction(node =>
{
    if (hasPossibleMatch) return;
    if (!node.IsKind(EnumProvider.SyntaxKind.InvocationExpression)) return;

    var invocation = (InvocationExpressionSyntax)node;
    string? name = invocation.Expression switch
    {
        MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
        IdentifierNameSyntax id => id.Identifier.ValueText,
        _ => null
    };

    if (name is not null && IsRelevantMethodName(name))
        hasPossibleMatch = true;
});

if (!hasPossibleMatch) return; // Skip GetOperation entirely
```

Syntax-level method name checks are case-insensitive string comparisons, not semantic resolution. That is acceptable as a pre-filter because false positives only cost an unnecessary (inexpensive) `GetOperation` call. False negatives would be a correctness bug, so the name set must be comprehensive.

### DO NOT share state across action types via `ConditionalWeakTable<Compilation>`

`ConditionalWeakTable` uses reference equality on the `Compilation` key. The `Compilation` instance from `SemanticModel.Compilation` (in `CodeBlockAction`, `OperationAction`, `SyntaxNodeAction`) is a **different object** from `ctx.Compilation` in `CompilationAction`. Data stored by one action type is invisible to the other, causing silent data loss.

`ConditionalWeakTable<Compilation>` is safe within a single action type (e.g., only in `SymbolAction` callbacks, as in `DuplicateODataEntityName`). It fails when you write in `CodeBlockAction` and read in `CompilationAction`.

**Instead:** Use `CompilationStartAction` + `RegisterCompilationEndAction` with closures:

```csharp
public override void Initialize(AnalysisContext context)
{
    context.RegisterCompilationStartAction(OnCompilationStart);
}

private void OnCompilationStart(CompilationStartAnalysisContext compilationCtx)
{
    // Shared state, guaranteed same instance across all callbacks
    var accumulator = new ConcurrentDictionary<string, ConcurrentBag<MyData>>();

    // All inner actions capture the same accumulator via closure
    compilationCtx.RegisterCodeBlockAction(ctx => CollectData(ctx, accumulator));
    compilationCtx.RegisterSymbolAction(ctx => CollectMore(ctx, accumulator), ...);
    compilationCtx.RegisterCompilationEndAction(ctx => Analyze(ctx, accumulator));
}
```

`RegisterCompilationEndAction` runs after all `CodeBlockAction` and `SymbolAction` callbacks complete. This is the correct way to implement collect-then-analyze patterns.

## SDK GetSymbol() Bug

The BC SDK's `OperationExtensions.GetSymbol()` switches on `OperationKind.FieldAccess` and casts to `IFieldAccess`, but two internal bound types use that kind without implementing `IFieldAccess`:

| SDK Type | Implements | Triggered by | Returns |
|---|---|---|---|
| `BoundApplicationObjectAccess` | `IApplicationObjectAccess` (public) | `DATABASE::X`, `CODEUNIT::X`, `TABLE::X`, `XMLPORT::X`, `QUERY::X` | Integer (the object ID) |
| `BoundObjectAccess` | `IObjectAccess` (internal) | Object type references | Varies |

Both set `ExpressionKind => OperationKind.FieldAccess`, so `GetSymbol()` attempts `((IFieldAccess)operation).FieldSymbol` and throws `InvalidCastException`.

### Solution: Always use `GetSymbolSafe()` instead of `GetSymbol()`

`OperationSafeExtensions.GetSymbolSafe()` in `ALCops.Common/Extensions/OperationExtensions.cs` handles the bug with zero overhead:

```csharp
public static ISymbol? GetSymbolSafe(this IOperation operation)
{
    // BoundApplicationObjectAccess: DATABASE::X, CODEUNIT::X, etc.
    if (operation is IApplicationObjectAccess appObjAccess)
        return appObjAccess.ApplicationObjectTypeSymbol;

    // Guard BoundObjectAccess and future mismatches
    if (operation.Kind == EnumProvider.OperationKind.FieldAccess && operation is not IFieldAccess)
        return null;

    return operation.GetSymbol();
}
```

- `IApplicationObjectAccess` is a public SDK interface, so the type check works directly
- `IObjectAccess` is internal, so the `is not IFieldAccess` guard catches it generically
- No try/catch, no exception handling overhead on the happy path
- 25+ `GetSymbol()` call sites across all cops should migrate to `GetSymbolSafe()`

### How Microsoft's own analyzers handle this

CodeCop Rule 243 (decompiled) casts to `IApplicationObjectAccess` explicitly instead of relying on `GetSymbol()`:
```csharp
ICodeunitTypeSymbol obj = (SemanticFacts.GetBoundExpressionArgument(invocationExpression, 0)
    as IApplicationObjectAccess)?.ApplicationObjectTypeSymbol as ICodeunitTypeSymbol;
```
