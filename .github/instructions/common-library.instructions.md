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
| `RecordTypeSymbolExtensions.cs` | `IRecordTypeSymbol` | `IsTemporary()` - `Temporary` keyword OR backing table `TableType = Temporary` (delegates to `ITableTypeSymbol.IsTemporary()`). Centralizes temporary-record detection shared by AC0013, AC0031, AC0032, PC0035, and other cops. Replaced the former `RequiredPermissionDetector.IsEffectivelyTemporary`. |
| `TableTypeSymbolExtensions.cs` | `ITableTypeSymbol` | `IsTemporary()` - `TableType == TableTypeKind.Temporary`. Replaced the former `RequiredPermissionDetector.IsTemporaryTable` and a private duplicate in `FieldGroupsRequired`. |
| `IdentifierProperty.cs` | N/A (enum) | `Comment`, `Locked`, `MaxLength` |

### Helpers/
Higher-level utilities that wrap SDK functionality.

| File | Purpose |
|------|---------|
| `AppSourceCopConfigurationProvider.cs` | Adapter wrapping `Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration`. Exposes `MandatoryAffixes`, `MandatorySuffix`, `MandatoryPrefix`. Uses init-only setters on net8.0, regular setters on netstandard2.1. |
| `ManifestHelper.cs` | `GetManifest(Compilation)` returning `NavAppManifest?`. On net8.0 delegates directly; on netstandard2.1 uses reflection to create a typed delegate, trying two different type paths for AL version compatibility. **Throws `FileNotFoundException` in test contexts** because `Microsoft.Dynamics.Nav.Analyzers.Common` assembly isn't available. Analyzers must catch this and treat as null manifest. |
| `ODataNameHelper.cs` | `MangleIntoValidXmlIdentifier(string name)` returning `string?`. Accesses `NameTransformations.MangleIntoValidXmlIdentifier` in `Microsoft.Dynamics.Nav.AL.Common` via `Type.GetType()` + `GetMethod()` + `CreateDelegate()`. Returns null if the SDK method is unavailable (older SDK versions). Callers should check `IsAvailable` property to exit early. Used by PC0033 (DuplicateODataEntityName). |
| `AcronymRegistry.cs` | Case-insensitive registry of canonical acronym casings (`LCY`, `OData`, `UoM`, `VAT`, ...). Exposes `DefaultAcronyms` (curated BC/web/data list as a flat array, excludes 2-letter abbreviations and `ID`), `Default` singleton, `Create(IEnumerable<string>?)` merge factory, `TryGetCanonical(word, out canonical)` (returns the preferred first-added variant), and `TryGetVariants(word, out variants)` (returns all registered variants for the case-insensitive key, ordered; first entry is canonical). Supports **multiple variants per key** — e.g. defaults list both `BoM`/`Bom` and `UoM`/`Uom`. User entries for a key **displace** built-in variants for that key (user list is authoritative per key; multiple user entries under the same key accumulate). Used by LC0098 (EventSubscriberNamingPattern); designed as shared infrastructure for future rules that render identifiers from natural-language input. |
| `IdentifierNameRenderer.cs` | Static renderer that turns natural-language input (BC field names, phrases, PascalCase identifiers) into a chosen `IdentifierCaseStyle` while honoring acronym canonical casing via an `AcronymRegistry`. Exposes `Render(input, style, mode, acronyms)` for a single spelling and `RenderVariants(input, style, acronyms)` returning the accepted Preserve + Normalize spellings (deduped when equal). Handles the `ID` abbreviation exception and the C# 2-letter-uppercase rule. Splits on whitespace, `_ - . / & ( ) +`, and PascalCase/camelCase boundaries. Used by LC0098; shared infrastructure for future identifier-generating rules. |
| `IdentifierCaseStyle.cs` | Enum consumed by `IdentifierNameRenderer`: `Pascal`, `Camel`, `Snake`, `Kebab`. |
| `AcronymRenderMode.cs` | Enum consumed by `IdentifierNameRenderer`: `Preserve` (keep canonical acronym casing) vs `Normalize` (apply C# first-upper/rest-lower to every word). |

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
| `ALCopsSettings.cs` | POCO with properties: `CognitiveComplexityThreshold` (default 15), `CyclomaticComplexityThreshold` (default 8), `MaintainabilityIndexThreshold` (default 20), `LanguagesToTranslate`, `NamingPatterns`, `SubscriberNamingPattern`, `UseSequentialGuidScope`, `ToolTipAllowedPunctuations`, `KnownAcronyms`, `StatementBlockSpacing`. |
| `ALCopsSettingsProvider.cs` | Static provider with `ConcurrentDictionary` cache keyed by directory path. Loads `alcops.json` using hierarchical lookup (see Settings System below). JSON parsing is case-insensitive, allows comments and trailing commas. Malformed JSON (invalid syntax, unknown enum values, wrong types) falls back to defaults silently via a `JsonException` catch in `DeserializeSettings`. Preferred API: `GetSettings(compilation.FileSystem)`. |

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

Analyzers access settings via the `IFileSystem` overload (preferred):
```csharp
var settings = ALCopsSettingsProvider.GetSettings(context.SemanticModel.Compilation.FileSystem);
int threshold = settings.CognitiveComplexityThreshold;
```

### Lookup hierarchy

Settings are resolved using `.editorconfig`-style upward traversal. The first `alcops.json` found wins (no merging):

1. **App folder** (where `app.json` lives) — checked via `IFileSystem.OpenRead("alcops.json")`
2. **Parent directories** — walks up the physical filesystem indefinitely until root or an inaccessible directory
3. **Assembly location** — directory where `ALCops.Common.dll` is located
4. **Defaults** — built-in default values from `ALCopsSettings`

This allows a multi-root workspace to share a single `alcops.json` at the workspace root:
```
/workspace/
├── alcops.json           ← shared settings (found by parent traversal)
├── App1/
│   ├── app.json
│   └── alcops.json       ← app-specific override (wins for App1)
└── App2/
    └── app.json          ← inherits from workspace-level
```

### Public API

`ALCopsSettingsProvider` exposes a single entry point: `GetSettings(IFileSystem?)`. All analyzer code obtains settings through `context.SemanticModel.Compilation.FileSystem`. Behavior: virtual FS check → parent traversal → assembly fallback. Results are cached by `IFileSystem.GetDirectoryPath()`; a `MemoryFileSystem` returning `""` bypasses the cache.

### Error handling

- Inaccessible directory during parent traversal: stops traversal (treats as boundary)
- Unreadable/malformed `alcops.json`: silently returns defaults (see #328 for planned improvement)
- `MemoryFileSystem` (in tests, `GetDirectoryPath()` returns `""`): only checks virtual FS, no parent traversal

Users configure settings by placing an `alcops.json` file in their AL project root or any parent directory:
```json
{
    "CognitiveComplexityThreshold": 20,
    "CyclomaticComplexityThreshold": 10,
    "MaintainabilityIndexThreshold": 15
}
```

Settings are cached per directory path for the analyzer session lifetime. There is no public cache-invalidation API; tests inject an isolated `IFileSystem` (typically `MemoryFileSystem` or a purpose-built `RelativeFileSystem`) to avoid contaminating the cache.

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
2. No changes needed to `ALCopsSettingsProvider.cs` for scalar / string / list / dictionary properties — JSON deserialization picks them up automatically.
3. **For enum-typed properties**, add a converter registration to `ALCopsSettingsProvider.cs`: `JsonStringEnumConverter` in `_jsonOptions.Converters` (net8+) and `StringEnumConverter` in `_jsonSettings.Converters` (netstandard2.1). Both are case-insensitive by default. Then add a schema-parity guard test that compares `Enum.GetNames(typeof(YourEnum))` with the `enum` array in `alcops.schema.json` (see `StatementBlockSpacingSchema` in `src/ALCops.FormattingCop.Test/Rules/StatementBlocksSeparatedByBlankLine/` for a template).
4. Document the new setting in the project README.

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
