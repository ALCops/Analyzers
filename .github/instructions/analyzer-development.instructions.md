---
applyTo: 'src/ALCops.*/Analyzers/**'
---

# ALCops Analyzer Development Guide

ALCops Analyzers is a collection of 6 AL code analyzers for Business Central, built on the `Microsoft.Dynamics.Nav.CodeAnalysis` SDK. Each analyzer ("cop") lives in its own project and targets a specific concern.

## Project Structure

```
src/
  ALCops.ApplicationCop/       # AC prefix - Business logic and application patterns
  ALCops.DocumentationCop/     # DC prefix - Code documentation rules
  ALCops.FormattingCop/        # FC prefix - Code style and formatting
  ALCops.LinterCop/            # LC prefix - General linting and code quality
  ALCops.PlatformCop/          # PC prefix - Platform correctness and runtime safety
  ALCops.TestAutomationCop/    # TA prefix - Test codeunit conventions
  ALCops.Common/               # Shared utilities, extensions, reflection helpers
  ALCops.Analyzers/            # Umbrella package that bundles all cops
```

Each cop project follows the same internal layout:

```
ALCops.<CopName>/
  Analyzers/                   # Analyzer classes (one class per rule or closely related rules)
  CodeFixes/                   # Optional code fix providers
  DiagnosticIds.cs             # All diagnostic ID constants for this cop
  DiagnosticDescriptors.cs     # All DiagnosticDescriptor definitions for this cop
  ALCops.<CopName>Analyzers.resx  # Resource file for diagnostic messages
```

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

IDs are defined as `public static readonly string` in `DiagnosticIds.cs`:

```csharp
namespace ALCops.PlatformCop;

public static class DiagnosticIds
{
    public static readonly string EditableFlowField = "PC0001";
    public static readonly string AutoIncrementInTemporaryTable = "PC0002";
    // ...
}
```

The constant name is a PascalCase description of the rule. Choose the next available number in the sequence for the cop you are adding to. Check the existing IDs first to avoid gaps or collisions.

## DiagnosticDescriptors

Each cop has a `DiagnosticDescriptors.cs` file that constructs `DiagnosticDescriptor` instances. These reference:
- The ID from `DiagnosticIds`
- Messages from the `.resx` resource file (via the auto-generated resource class)
- A category from the inner `Category` class
- A help URI pointing to alcops.dev

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
- `messageFormat` supports `{0}`, `{1}` placeholders for parameterized messages passed via `Diagnostic.Create(...)`.
- `defaultSeverity` is typically `Warning`. Use `Error` for rules that catch definite runtime failures, `Info` for suggestions/metrics.
- `isEnabledByDefault` is `true` for most rules. Set to `false` for opt-in rules (metrics, opinionated style).
- The `GetHelpUri` format varies per cop: replace `platformcop` with `lintercop`, `applicationcop`, etc.

## Resource Files (.resx)

Diagnostic messages live in `ALCops.<CopName>Analyzers.resx`. The build auto-generates a strongly-typed class (e.g., `PlatformCopAnalyzers`, `LinterCopAnalyzers`) from this file.

Naming convention for each rule (using `AutoIncrementInTemporaryTable` as example):

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
- `<RuleName>Title` - short summary (used as the diagnostic title)
- `<RuleName>MessageFormat` - the message shown to the user, may contain `{0}`, `{1}` placeholders
- `<RuleName>Description` - detailed explanation of why this rule exists

Optional entry:
- `<RuleName>CodeAction` - display text for an associated code fix (e.g., `ALCops: Remove redundant ApplicationArea`)

For parameterized messages, use standard .NET format strings:
```xml
<data name="AccessPropertyExplicitlySetMessageFormat" xml:space="preserve">
  <value>{0} '{1}' does not explicitly have the Access property set.</value>
</data>
```

## Analyzer Class Pattern

All analyzers inherit from `DiagnosticAnalyzer` and are decorated with `[DiagnosticAnalyzer]`.

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

Choose the registration method based on what you need to analyze:

| Method | Context Type | Use When |
|--------|-------------|----------|
| `RegisterSymbolAction` | `SymbolAnalysisContext` | Analyzing declared symbols (tables, pages, fields, methods). Most common. |
| `RegisterOperationAction` | `OperationAnalysisContext` | Analyzing operations in method bodies (invocations, assignments). |
| `RegisterSyntaxNodeAction` | `SyntaxNodeAnalysisContext` | Analyzing specific syntax nodes (string literals, identifiers). |
| `RegisterCodeBlockAction` | `CodeBlockAnalysisContext` | Analyzing entire method/trigger bodies (complexity metrics). |
| `RegisterCompilationAction` | `CompilationAnalysisContext` | Analyzing the full compilation (manifest checks, cross-object analysis). |
| `RegisterCompilationStartAction` | `CompilationStartAnalysisContext` | Registering additional actions at compilation start (multi-pass analysis). |

### CompilationStart + SymbolAction (expensive resource loading)

When an analyzer needs to load expensive resources once per compilation (XLIFF files, settings, cross-object indexes), use `RegisterCompilationStartAction` to load them, then register per-symbol actions that use the cached data:

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
- Exit early (don't register the symbol action) if the resource loading fails or returns no data. This makes the rule a no-op for projects without the resource.
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
        invocation.TargetMethod.Name != "Commit")
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

## EnumProvider (Critical Pattern)

Never reference `Microsoft.Dynamics.Nav.CodeAnalysis` enum values directly. Always use `EnumProvider` from `ALCops.Common.Reflection`. This provides backward compatibility across SDK versions via reflection-based caching.

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

Each `EnumProvider` nested class exposes a `CanonicalNames` property (`Lazy<ImmutableDictionary<string, string>>`) that maps case-insensitive enum value names to their canonical form. These are auto-generated from `Enum.GetNames()` at runtime, so they're self-maintaining across SDK versions.

## Semantic Model APIs

The `SemanticModel` provides symbol resolution beyond what pure syntax analysis offers. Use it when you need canonical names, type information, or cross-object references.

### Getting the SemanticModel

```csharp
// From SymbolAnalysisContext (most common)
var root = ctx.Symbol.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken);
var semanticModel = ctx.Compilation.GetSemanticModel(root.SyntaxTree);

// From SyntaxNodeAnalysisContext
var semanticModel = ctx.SemanticModel;
```

### GetSymbolInfo vs GetDeclaredSymbol

These two methods serve different purposes and work on different node types:

| Method | Purpose | Returns |
|--------|---------|---------|
| `GetSymbolInfo(node)` | Resolves a **reference** to a symbol | The symbol being referenced |
| `GetDeclaredSymbol(node)` | Gets the symbol **declared by** a node | The declared symbol itself |

### Resolution Matrix

Not all syntax node types resolve via the semantic model. This matrix shows what works:

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

When resolving many identifiers via `GetSymbolInfo`, batch them for performance:

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

When working inside `RegisterOperationAction`, the bound operation tree already has symbols resolved. Prefer `IOperation.GetSymbolSafe()` over `SemanticModel.GetSymbolInfo()` in this context.

### `IOperation.GetSymbolSafe()` vs `SemanticModel.GetSymbolInfo()`

| Method | Context | Cost | Use When |
|---|---|---|---|
| `IOperation.GetSymbolSafe()` | Operation analysis | O(1) with type guard | You have an `IOperation` (invocation instance, argument value, field access) |
| `SemanticModel.GetSymbolInfo(node)` | Syntax analysis | Performs semantic resolution | You have a `SyntaxNode` and no operation tree |

**IMPORTANT: Always use `GetSymbolSafe()` instead of `GetSymbol()`.** The SDK's `GetSymbol()` crashes with `InvalidCastException` on `BoundApplicationObjectAccess` (`DATABASE::X`, `CODEUNIT::X`) and `BoundObjectAccess` because they report `Kind = FieldAccess` but don't implement `IFieldAccess`. See "SDK GetSymbol() Bug" section below.

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

The SDK frequently wraps argument values in `IConversionExpression` for implicit type conversions (e.g., passing a `Record "Sales Header"` where the parameter type is `Record "Sales Header"`). When this happens, `argument.Value.GetSymbolSafe()` returns null because the conversion itself has no symbol. Unwrap through the conversion to get the actual operand's symbol:

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

The SDK's `PropertyInfoLookup` class provides a static method to query property metadata at runtime, enabling self-maintaining enum property value resolution without manual dictionaries.

### How It Works

`PropertyInfoLookup.Lookup(SymbolKind, PropertyKind)` is a **public static method** that returns a `PropertyTypeInfo`. For properties that accept enum values, the result is an `EnumPropertyTypeInfo` (internal subclass) with an `Options` property containing `EnumPropertyMemberInfo` objects. Each `EnumPropertyMemberInfo.Name` is the **canonical form** of the option value.

```
PropertyInfoLookup.Lookup(SymbolKind.Codeunit, PropertyKind.Access)
  → EnumPropertyTypeInfo
    → Options: [EnumPropertyMemberInfo { Name="Public" }, EnumPropertyMemberInfo { Name="Internal" }]

PropertyInfoLookup.Lookup(SymbolKind.Page, PropertyKind.PageType)
  → EnumPropertyTypeInfo
    → Options: [{ Name="Card" }, { Name="List" }, { Name="RoleCenter" }, ...]
```

### Building a Self-Maintaining Dictionary

Since `EnumPropertyTypeInfo` and `EnumPropertyMemberInfo` are internal types, access them via reflection. The pattern iterates all `SymbolKind × PropertyKind` combinations at startup and merges options per `PropertyKind`:

```csharp
private static readonly Lazy<Dictionary<PropertyKind, ImmutableDictionary<string, string>>>
    _enumPropertyValuesByKind = new(() =>
{
    var result = new Dictionary<PropertyKind, ImmutableDictionary<string, string>>();

    var lookupMethod = typeof(PropertyInfoLookup)
        .GetMethod("Lookup", BindingFlags.Public | BindingFlags.Static);
    if (lookupMethod is null) return result;

    // Find internal types via safe assembly scanning
    Type[] sdkTypes;
    try { sdkTypes = typeof(PropertyInfoLookup).Assembly.GetTypes(); }
    catch (ReflectionTypeLoadException ex)
    { sdkTypes = ex.Types.Where(t => t != null).ToArray()!; }

    var enumPropTypeInfo = sdkTypes.FirstOrDefault(t => t?.Name == "EnumPropertyTypeInfo");
    var optionsProp = enumPropTypeInfo?.GetProperty("Options",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (enumPropTypeInfo is null || optionsProp is null) return result;

    PropertyInfo? nameProp = null;
    foreach (var sk in Enum.GetValues<SymbolKind>())
        foreach (var pk in Enum.GetValues<PropertyKind>())
        {
            try
            {
                var info = lookupMethod.Invoke(null, new object[] { sk, pk });
                if (info?.GetType() != enumPropTypeInfo) continue;

                if (optionsProp.GetValue(info) is not IEnumerable options) continue;

                if (!result.ContainsKey(pk))
                    result[pk] = ImmutableDictionary<string, string>.Empty;

                var builder = result[pk].ToBuilder();
                builder.KeyComparer = StringComparer.OrdinalIgnoreCase;

                foreach (var opt in options)
                {
                    nameProp ??= opt.GetType().GetProperty("Name");
                    if (nameProp?.GetValue(opt) is string name && !builder.ContainsKey(name))
                        builder[name] = name;
                }
                result[pk] = builder.ToImmutable();
            }
            catch { } // Silently skip version-incompatible combinations
        }

    return result;
}, LazyThreadSafetyMode.PublicationOnly);
```

### Key Facts

- Discovers **60 PropertyKinds** with **253 unique option values** from the current SDK
- **Zero case collisions** across all option names (verified)
- **6 PropertyKinds** produce different options depending on SymbolKind: `Access`, `AllowInCustomizations`, `ObsoleteState`, `Scope`, `Subtype`, `Type`. Merging across all SymbolKinds handles this.
- New enum properties added in future AL SDK versions are **automatically discovered**
- All reflection uses `Lazy<T>` with `LazyThreadSafetyMode.PublicationOnly` per project conventions
- Wrap assembly type scanning in `try/catch (ReflectionTypeLoadException)` since some SDK types may fail to load (e.g., `System.Text.Json` version mismatch)

### When to Use This Pattern

Use `PropertyInfoLookup`-based discovery when you need canonical forms of **enum property values** (e.g., `Access = Public`, `PageType = Card`). For other canonical name needs:
- **Property names**: Use `IPropertySymbol.PropertyKind.ToString()` (semantic model, no dictionary needed)
- **Data type names**: Use `NavTypeKind` enum via `Enum.GetNames()` (self-maintaining)
- **Attribute names**: Use `EnumProvider.AttributeKind.CanonicalNames` (self-maintaining)
- **Symbol kind names**: Use `SymbolKind` enum via `Enum.GetNames()` (self-maintaining)
- **User-defined identifiers**: Use `GetSymbolInfo()` → `ISymbol.Name` (IntelliSense-equivalent)

## Common Utilities (ALCops.Common)

### AnalysisContextExtensions

Always call `IsObsolete()` early in your analysis method to skip obsolete symbols. Available for all context types:

```csharp
ctx.IsObsolete()  // works on SymbolAnalysisContext, OperationAnalysisContext,
                  // SyntaxNodeAnalysisContext, CodeBlockAnalysisContext
```

Also available:
- `ctx.IsDiagnosticEnabled(descriptor)` - check if a diagnostic is suppressed (SyntaxNodeAnalysisContext only)

### SymbolInterfaceExtensions

```csharp
symbol.IsObsolete()                          // check all obsolete states (pending, removed, moved)
symbol.GetPageTypeSymbol()                   // cast to IPageTypeSymbol
symbol.GetFlattenedControls()                // get all controls from page or page extension
symbol.GetFullyQualifiedObjectName()         // "Namespace.ObjectName"
symbol.GetContainingNamespaceQualifiedNameWithReflection()
```

### ManifestHelper

Access the app manifest (app.json) from the compilation:

```csharp
var manifest = ManifestHelper.GetManifest(ctx.Compilation);
if (manifest is not null && manifest.Runtime >= RuntimeVersion.Spring2021)
    // feature is supported
```

**Important:** `ManifestHelper.GetManifest` loads `Microsoft.Dynamics.Nav.Analyzers.Common` assembly via reflection. In test contexts (minimal compilations without the full SDK runtime), this assembly isn't available, causing a `FileNotFoundException`. Analyzers that call `ManifestHelper.GetManifest` must handle this:

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

Note: `CompilationWithAnalyzers` silently swallows all exceptions from analyzer callbacks, making this extremely hard to diagnose without explicit try-catch. The analyzer just appears to produce no diagnostics.

### SyntaxNodeExtensions

Helpers for reading label/property syntax:
- `GetBooleanPropertyValue(IdentifierProperty.Locked)` - check if a label is locked
- `GetIntegerPropertyValue(IdentifierProperty.MaxLength)` - read MaxLength property

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

`ContainingModule` is available on any `ISymbol`. The `==` operator performs reference equality on the module objects, which is reliable because the compiler creates one module instance per app. This is preferable to string-based app ID comparison because it's simpler, faster, and immune to formatting differences.

### Version-Gating Analyzers

If a rule only applies to certain BC runtime versions, override `SupportedVersions`:

```csharp
public override VersionCompatibility SupportedVersions =>
    VersionProvider.VersionCompatibility.Fall2024OrGreater;
```

## Writing Tests

Tests use NUnit with `RoslynTestKit`. Each test lives in a folder matching the rule name under `Rules/`.

### Test Structure

```
ALCops.<CopName>.Test/
  Rules/
    <RuleName>/
      <RuleName>.cs              # Test class
      HasDiagnostic/             # AL files that SHOULD trigger the diagnostic
        <TestCase>.al
      NoDiagnostic/              # AL files that should NOT trigger the diagnostic
        <TestCase>.al
```

### Test Class Pattern

```csharp
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class AutoIncrementInTemporaryTable : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.AutoIncrementInTemporaryTable>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(AutoIncrementInTemporaryTable)));
        }

        [Test]
        [TestCase("AutoIncrementFieldInTemporaryTable")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AutoIncrementInTemporaryTable);
        }

        [Test]
        [TestCase("AutoIncrementFieldInTable")]
        [TestCase("RegularFieldInTemporaryTable")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.AutoIncrementInTemporaryTable);
        }
    }
}
```

### Test AL Files

Use `[|...|]` markers to indicate where the diagnostic should (or should not) fire:

```al
// HasDiagnostic/AutoIncrementFieldInTemporaryTable.al
table 50100 MyTable
{
    TableType = Temporary;

    fields
    {
        field(1; MyField; Integer)
        {
            [|AutoIncrement = true|];
        }
    }
}
```

```al
// NoDiagnostic/AutoIncrementFieldInTable.al
table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer)
        {
            [|AutoIncrement = false|];
        }
    }
}
```

## Step-by-Step: Adding a New Rule

Suppose you are adding rule `PC0029` to PlatformCop.

### 1. Add the diagnostic ID

In `src/ALCops.PlatformCop/DiagnosticIds.cs`, add:

```csharp
public static readonly string MyNewRule = "PC0029";
```

### 2. Add resource strings

In `src/ALCops.PlatformCop/ALCops.PlatformCopAnalyzers.resx`, add three `<data>` entries:

```xml
<data name="MyNewRuleTitle" xml:space="preserve">
  <value>Short title for the rule</value>
</data>
<data name="MyNewRuleMessageFormat" xml:space="preserve">
  <value>The {0} '{1}' has a problem because...</value>
</data>
<data name="MyNewRuleDescription" xml:space="preserve">
  <value>Detailed explanation of why this rule exists and what it checks.</value>
</data>
```

### 3. Add the diagnostic descriptor

In `src/ALCops.PlatformCop/DiagnosticDescriptors.cs`, add:

```csharp
public static readonly DiagnosticDescriptor MyNewRule = new(
    id: DiagnosticIds.MyNewRule,
    title: PlatformCopAnalyzers.MyNewRuleTitle,
    messageFormat: PlatformCopAnalyzers.MyNewRuleMessageFormat,
    category: Category.Design,
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: PlatformCopAnalyzers.MyNewRuleDescription,
    helpLinkUri: GetHelpUri(DiagnosticIds.MyNewRule));
```

### 4. Create the analyzer class

Create `src/ALCops.PlatformCop/Analyzers/MyNewRule.cs`:

```csharp
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class MyNewRule : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MyNewRule);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            this.Analyze,
            EnumProvider.SymbolKind.Table);

    private void Analyze(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        // Your analysis logic...

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MyNewRule,
            ctx.Symbol.GetLocation(),
            ctx.Symbol.Kind.ToString(),
            ctx.Symbol.Name));
    }
}
```

### 5. Write tests

Create the test folder structure:

```
src/ALCops.PlatformCop.Test/Rules/MyNewRule/
  MyNewRule.cs
  HasDiagnostic/
    <TestCase>.al
  NoDiagnostic/
    <TestCase>.al
```

Write the test class following the pattern above, using `RoslynFixtureFactory.Create<Analyzers.MyNewRule>()`.

### 6. Build and verify

Build the solution and run the tests to confirm the analyzer works.

## Gathering All Extensions of a Kind (ConditionalWeakTable Cache Pattern)

When an analyzer needs to find all extension objects (page extensions, table extensions) targeting a specific base object, use a `ConditionalWeakTable<Compilation, CacheEntry>` to lazily cache the full set once per compilation. This avoids repeated expensive `GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection()` calls.

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

- Analyzing page extensions that need controls from sibling extensions targeting the same page (e.g., `DuplicateODataEntityName` for OData name collision)
- Analyzing table extensions that need fields from sibling extensions targeting the same table (e.g., `TransferFieldsSchemaCompatibility` for field schema comparison)
- Any cross-object analysis where you need to enumerate all extensions of a given `SymbolKind`

### Key points

- `ConditionalWeakTable` uses the `Compilation` as key, so the cache is GC'd when the compilation is released
- The inner `Lazy<T>` with `ExecutionAndPublication` ensures single computation per compilation, even under concurrent `RegisterSymbolAction` callbacks
- `GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection` retrieves symbols from all referenced modules, not just the current one
- Always use `OriginalDefinition` when comparing symbols from different contexts (extension target vs base object)
- Requires `using System.Runtime.CompilerServices;` for `ConditionalWeakTable`

### Existing implementations

| File | Extension kind | Purpose |
|---|---|---|
| `TransferFieldsSchemaCompatibility.cs` | `TableExtension` | Gathers table extension fields for schema comparison |
| `DuplicateODataEntityName.cs` | `PageExtension` | Gathers sibling page extension controls for OData name collision detection |

## Common Pitfalls

- **Always use `EnumProvider`** for SDK enum values. Direct references break across BC versions.
- **Always check `IsObsolete()` first** in every analysis method. Reporting on obsolete code creates noise.
- **Never compare raw syntax text to identify symbols.** Do not use `syntax.ToString()`, `node.Identifier.ValueText`, or similar text-based checks to determine what a variable, method, or expression refers to. These are fragile (case-sensitive, whitespace-dependent, miss implicit conversions) and produce incorrect results when the source text doesn't match the canonical symbol name. Instead, resolve the symbol via `IOperation.GetSymbolSafe()`, `SemanticModel.GetSymbolInfo()`, or `SemanticModel.GetDeclaredSymbol()`, then compare using symbol identity (`symbol.Equals(other)`) or symbol properties (`symbol.Kind`, `symbol.Name`). Checking `ISymbol.Name` after resolution is acceptable because it's the compiler-resolved canonical form, not raw source text.
- **Use `CancellationToken`** via `ctx.CancellationToken.ThrowIfCancellationRequested()` in loops over large collections (e.g., iterating all fields in a table).
- **Mark analyzer classes `sealed`** unless there is a specific reason for inheritance.
- **One analyzer class per rule or tightly related rule group.** The `AnalyzeCountMethod` analyzer handles both `LC0081` (IsEmpty) and `LC0082` (FindWithNext) because they share the same analysis logic.
- **Use `ImmutableArray.Create()`** for `SupportedDiagnostics`, listing all descriptors the analyzer may report.
- **Location matters.** Report diagnostics at the most specific location: the offending property, syntax node, or symbol, not the entire object.

## SDK GetSymbol() Bug

The BC SDK's `OperationExtensions.GetSymbol()` has a known bug: it switches on `OperationKind.FieldAccess` and casts to `IFieldAccess`, but two internal bound types use that kind without implementing `IFieldAccess`:

| SDK Type | Implements | Triggered by | Returns |
|---|---|---|---|
| `BoundApplicationObjectAccess` | `IApplicationObjectAccess` (public) | `DATABASE::X`, `CODEUNIT::X`, `TABLE::X`, `XMLPORT::X`, `QUERY::X` | Integer (the object ID) |
| `BoundObjectAccess` | `IObjectAccess` (internal) | Object type references | Varies |

Both set `ExpressionKind => OperationKind.FieldAccess`, causing `GetSymbol()` to attempt `((IFieldAccess)operation).FieldSymbol` which throws `InvalidCastException`.

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

CodeCop Rule 243 demonstrates the correct pattern (from decompiled source):
```csharp
ICodeunitTypeSymbol obj = (SemanticFacts.GetBoundExpressionArgument(invocationExpression, 0)
    as IApplicationObjectAccess)?.ApplicationObjectTypeSymbol as ICodeunitTypeSymbol;
```

Microsoft casts to `IApplicationObjectAccess` explicitly instead of relying on `GetSymbol()`.
