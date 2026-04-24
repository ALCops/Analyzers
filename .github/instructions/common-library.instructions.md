---
applyTo: 'src/ALCops.Common/**'
---

# ALCops.Common: Shared Library for AL Analyzers

## Project Role

ALCops.Common is the shared foundation library referenced by **all 13 projects** in the ALCops solution: 6 cop analyzers, their 6 test projects, and the aggregator project (ALCops.Analyzers). Any change here affects every analyzer. Treat backward compatibility as a hard requirement.

## Build Configuration

- **Target frameworks**: `net8.0` locally, `netstandard2.1` + `net8.0` in CI (GitHub Actions)
- **LangVersion**: `Latest`
- **Nullable**: `enable` (enforced via `WarningsAsErrors` for CS8600-CS8605)
- **ImplicitUsings**: `enable`
- **SDK references**: `Microsoft.Dynamics.Nav.CodeAnalysis` and `Microsoft.Dynamics.Nav.Analyzers.Common` (loaded from `BcDevToolsDir`)
- **Conditional dependency**: `System.Collections.Immutable 5.0.0` and `Newtonsoft.Json` for `netstandard2.1` only

Always use `#if NETSTANDARD2_1` / `#if NET8_0_OR_GREATER` guards when APIs differ between target frameworks. Example: `System.Text.Json` for net8.0, `Newtonsoft.Json` for netstandard2.1.

## Directory Structure

### Extensions/
Extension methods on SDK types. Each file extends one type or interface family.

| File | Extends | Key Methods |
|------|---------|-------------|
| `AnalysisContextExtensions.cs` | `SymbolAnalysisContext`, `OperationAnalysisContext`, `SyntaxNodeAnalysisContext`, `CodeBlockAnalysisContext` | `IsObsolete()`, `IsDiagnosticEnabled(descriptor)` |
| `ApplicationObjectTypeSymbolInterfaceExtensions.cs` | `IApplicationObjectTypeSymbol` | `FindMethodByNameAcrossModules(name, compilation)`, `MethodImplementsInterfaceMethod(methodSymbol)` |
| `ArgumentInterfaceExtensions.cs` | `IArgument` | `GetTypeSymbol()` (handles ConversionExpression, InvocationExpression) |
| `CompilationExtensions.cs` | `Compilation` | `GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(kind, id)`, `GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(kind)`, `IsDiagnosticEnabled(descriptor)` |
| `FileSystemInterfaceExtensions.cs` | `IFileSystem` | `GetPermissionSetDocuments()`. Access via `Compilation.FileSystem` (returns `null` when unavailable). For tests, inject `MemoryFileSystem` via `AnalyzerTestFixtureConfig.FileSystem` (RoslynTestKit 1.1.0+). `MemoryFileSystem` keys use forward slashes; `GetDirectoryPath()` returns `""`. |
| `MethodSymbolInterfaceExtensions.cs` | `IMethodSymbol` | `MethodImplementsInterfaceMethod()`, `MethodImplementsInterfaceMethod(interfaceMethodSymbol)` |
| `StringExtensions.cs` | `string` | `QuoteIdentifierIfNeededWithReflection(useRelaxedIdentifierRules)` |
| `SymbolInterfaceExtensions.cs` | `ISymbol` | `GetContainingNamespaceQualifiedNameWithReflection()`, `GetPageTypeSymbol()`, `GetFlattenedControls()`, `GetFullyQualifiedObjectName(quoteIfNeeded)`, `IsObsolete()` |
| `SyntaxNodeExtensions.cs` | `LabelPropertyValueSyntax`, `LabelSyntax`, `SyntaxNode`, `CommaSeparatedIdentifierEqualsLiteralListSyntax` | `GetIntegerPropertyValue(property)`, `GetBooleanPropertyValue(property)` (overloaded for each syntax type) |
| `TypeSymbolInterfaceExtensions.cs` | `ITypeSymbol` | `GetTypeLength(ref isError)` |
| `OperationExtensions.cs` | `IOperation` | `GetSymbolSafe()` - Safe replacement for SDK `GetSymbol()` that handles `BoundApplicationObjectAccess` (`DATABASE::X`, `CODEUNIT::X`) via `IApplicationObjectAccess` interface check, and guards against `BoundObjectAccess` via `is not IFieldAccess`. No try/catch. See `analyzer-development.instructions.md` "SDK GetSymbol() Bug". |
| `IdentifierProperty.cs` | N/A (enum) | `Comment`, `Locked`, `MaxLength` |

### Helpers/
Higher-level utilities that wrap SDK functionality.

| File | Purpose |
|------|---------|
| `AppSourceCopConfigurationProvider.cs` | Adapter wrapping `Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration`. Exposes `MandatoryAffixes`, `MandatorySuffix`, `MandatoryPrefix`. Uses init-only setters on net8.0, regular setters on netstandard2.1. |
| `ManifestHelper.cs` | `GetManifest(Compilation)` returning `NavAppManifest?`. On net8.0 delegates directly; on netstandard2.1 uses reflection to create a typed delegate, trying two different type paths for AL version compatibility. **Throws `FileNotFoundException` in test contexts** because `Microsoft.Dynamics.Nav.Analyzers.Common` assembly isn't available. Analyzers must catch this and treat as null manifest. |
| `ODataNameHelper.cs` | `MangleIntoValidXmlIdentifier(string name)` returning `string?`. Accesses `NameTransformations.MangleIntoValidXmlIdentifier` in `Microsoft.Dynamics.Nav.AL.Common` via `Type.GetType()` + `GetMethod()` + `CreateDelegate()`. Returns null if the SDK method is unavailable (older SDK versions). Callers should check `IsAvailable` property to exit early. Used by PC0033 (DuplicateODataEntityName). |

### Reflection/
Runtime access to internal/version-dependent SDK types. This is the most sensitive area of Common.

| File | Purpose |
|------|---------|
| `CompilationHelper.cs` | Accesses non-public `ReferenceManager` and `CompiledModule` properties via `BindingFlags.Instance \| BindingFlags.NonPublic`. Provides `GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection()` and `GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection()`. |
| `EnumProvider.cs` | **~1900 lines.** Wraps 60+ Nav.CodeAnalysis enums (SymbolKind, SyntaxKind, NavTypeKind, PropertyKind, AttributeKind, ControlKind, etc.) using `Enum.Parse` with `Lazy<T>` caching. Never reference Nav.CodeAnalysis enum values directly in the codebase; always go through `EnumProvider`. |
| `PropertyAccessor.cs` | Extension methods `SetPropertyIfExists(name, value)` and `GetPropertyIfExists<T>(name, default)` on `object`. Walks inheritance hierarchy. Silent failure for missing properties. |
| `SymbolHelper.cs` | `GetContainingNamespaceQualifiedName(symbol)` and `ToDisplayStringWithReflection(symbol)` (netstandard2.1 only). Uses `Lazy<PropertyInfo?>` for cached reflection. |
| `StringHelper.cs` | `QuoteIdentifierIfNeeded(value, useRelaxedIdentifierRules)`. Detects SDK method signature at runtime (with/without bool parameter) for version compatibility. |
| `VersionProvider.cs` | Nested `VersionCompatibility` class with properties like `Fall2019OrGreater`, `Spring2024OrGreater`, `Fall2024OrGreater`. Uses `Type.GetField()` reflection to safely access static fields. Returns a "never supported" fallback when a field does not exist in the loaded SDK version. |

### Settings/
Per-project analyzer configuration.

| File | Purpose |
|------|---------|
| `ALCopsSettings.cs` | POCO with three properties: `CognitiveComplexityThreshold` (default 15), `CyclomaticComplexityThreshold` (default 8), `MaintainabilityIndexThreshold` (default 20). |
| `ALCopsSettingsProvider.cs` | Static provider with `ConcurrentDictionary` cache keyed by workspace path. Loads `alcops.json` from workspace directory, then falls back to assembly directory. JSON parsing is case-insensitive, allows comments and trailing commas. Use `GetSettings(workspacePath)` from analyzers. |

### Constants.cs
Three constants: `PermissionNodeXPath` (XPath for permission set XML), `Comment`, `Locked`, `MaxLength` (label property name strings matching the SDK's `LabelPropertyHelper`).

## Why Reflection Is Used Everywhere

The `Microsoft.Dynamics.Nav.CodeAnalysis` SDK treats many types, properties, and enum values as internal or changes their signatures between Business Central releases. Direct references would break compilation against older (or newer) SDK versions. The reflection pattern used throughout Common:

1. **Enum values**: `EnumProvider` wraps every enum value in `Lazy<T>` using `Enum.Parse`. In DEBUG builds, missing values throw; in RELEASE, they silently return `default(T)`.
2. **Properties**: `PropertyAccessor`, `SymbolHelper` use `Lazy<PropertyInfo?>` with `GetProperty()` and cache results.
3. **Methods**: `StringHelper`, `ManifestHelper` use `Lazy<MethodInfo?>` with `GetMethod()` and create typed delegates.
4. **Static fields**: `VersionProvider` uses `GetField()` with a "never supported" fallback.
5. **Internal members**: `CompilationHelper` uses `BindingFlags.NonPublic` to access `ReferenceManager` and `CompiledModule`.

**Key rule**: All `Lazy<T>` instances use `LazyThreadSafetyMode.PublicationOnly` for thread safety without locking overhead. Follow this pattern for any new reflection code.

## Settings System

Analyzers access settings via:
```csharp
var workspacePath = context.SemanticModel.Compilation.FileSystem?.GetDirectoryPath();
var settings = ALCopsSettingsProvider.GetSettings(workspacePath);
int threshold = settings.CognitiveComplexityThreshold;
```

Users configure settings by placing an `alcops.json` file in their AL project root:
```json
{
    "CognitiveComplexityThreshold": 20,
    "CyclomaticComplexityThreshold": 10,
    "MaintainabilityIndexThreshold": 15
}
```

Settings are cached per workspace path for the analyzer session lifetime. Call `ALCopsSettingsProvider.ClearCache()` only in tests.

## Coding Standards

- **Nullable annotations**: All public APIs must have correct nullability. The project treats CS8600-CS8605 as errors.
- **Extension method conventions**: One static class per extended type. Class named `{TypeName}Extensions`. Methods that use reflection append `WithReflection` to the method name (e.g., `QuoteIdentifierIfNeededWithReflection`, `GetContainingNamespaceQualifiedNameWithReflection`).
- **Conditional compilation**: Use `#if NETSTANDARD2_1` for older framework paths, `#if NET8_0_OR_GREATER` for newer ones. Keep both paths tested.
- **Reflection caching**: Always use `Lazy<T>` with `LazyThreadSafetyMode.PublicationOnly`. Never call `GetProperty()`/`GetMethod()`/`GetField()` in a hot path without caching.
- **Enum access**: Never reference `Microsoft.Dynamics.Nav.CodeAnalysis` enum values directly. Use `EnumProvider.{EnumName}.{Value}` instead.

## Guidelines for AI Agents

### When to Add to Common vs a Cop Project
- Add to Common if the utility is needed (or likely to be needed) by two or more cop projects.
- Add to Common if it wraps SDK internals or handles version compatibility.
- Keep it in the cop project if it is analyzer-specific logic (e.g., a particular diagnostic rule's helper).

### How to Add a New Extension Method
1. Find the appropriate file in `Extensions/` by the type you are extending. Create a new file only if no existing file covers that type.
2. Follow the naming convention: `{TypeName}Extensions` class, same namespace (`ALCops.Common.Extensions`).
3. If the method uses reflection, suffix the method name with `WithReflection`.
4. Add null checks; use nullable return types where the value may not exist.
5. If the method delegates to a reflection helper, put the reflection logic in `Reflection/` and expose a clean extension in `Extensions/`.

### How to Add a New Enum Value to EnumProvider
1. Open `Reflection/EnumProvider.cs` and find the nested class for the enum type.
2. Add a new `private static readonly Lazy<T>` field using `ParseEnum<T>(nameof(...))` or a string literal for values that may not exist in all SDK versions.
3. Add a public static property that returns `_field.Value`.
4. If the enum value requires conditional compilation for different frameworks, use `#if` guards.

### How to Add a New Setting
1. Add a new property with a default value to `ALCopsSettings.cs`.
2. No changes needed to `ALCopsSettingsProvider.cs` (JSON deserialization picks it up automatically).
3. Document the new setting in the project README.

### Backward Compatibility
- Do not remove or rename public methods, properties, or classes.
- Do not change method signatures. Add new overloads instead.
- Do not change default values in `ALCopsSettings` without discussion (users may depend on them).
- When adding reflection for a new SDK version, keep the fallback path for older versions.

### Testing
ALCops.Common has **no dedicated test project**. It is tested indirectly through the 6 cop test projects. When modifying Common:
- Run the full test suite (`dotnet test` at the solution level) to verify no regressions.
- If adding a new utility, write tests in the cop test project that will use it.
- Pay special attention to conditional compilation paths; CI builds both `net8.0` and `netstandard2.1`.
