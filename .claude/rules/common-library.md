---
paths:
  - "src/ALCops.Common/**"
---

# ALCops.Common: Shared Library for AL Analyzers

## Project Role

ALCops.Common is the shared foundation library referenced by **all 13 projects** in the ALCops solution: 6 cop analyzers, their 6 test projects, and the aggregator project (ALCops.Analyzers). Any change here affects every analyzer; update all callers in the same change.

Target frameworks, LangVersion, nullable enforcement and conditional package references are defined in the csproj (the csproj is the source of truth). Always use `#if NETSTANDARD2_1` / `#if NET8_0_OR_GREATER` guards when APIs differ between target frameworks (e.g. `System.Text.Json` for net8.0, `Newtonsoft.Json` for netstandard2.1). See `.claude/rules/netstandard21-compatibility.md`.

## Directory Purposes

- `Extensions/` — Extension methods on SDK types; one static class per extended type or interface family (e.g. `OperationExtensions.GetSymbolSafe()` as the safe replacement for the SDK `GetSymbol()` bug, `IRecordTypeSymbol.IsTemporary()` / `ITableTypeSymbol.IsTemporary()` as the shared temporary-record detection).
- `Helpers/` — Higher-level utilities that wrap SDK functionality (AppSourceCop configuration and mandatory affixes, manifest access, OData name mangling, acronym registry and identifier rendering). Note: `ManifestHelper.GetManifest` **throws `FileNotFoundException` in test contexts** because `Microsoft.Dynamics.Nav.Analyzers.Common` isn't available; analyzers must catch this and treat as null manifest. `AppSourceCopConfigurationProvider.GetMandatoryNameAffixes` / `MandatoryAffixes.GetAffixes` are NOT cached — re-read AppSourceCop.json every call, so cache per compilation at the call site.
- `Reflection/` — Runtime access to internal/version-dependent SDK types; the most sensitive area of Common. `EnumProvider` wraps 60+ Nav.CodeAnalysis enums — never reference Nav.CodeAnalysis enum values directly, always go through `EnumProvider`.
- `Settings/` — Per-project analyzer configuration: `ALCopsSettings` (POCO with defaults) and `ALCopsSettingsProvider` (hierarchical `alcops.json` lookup, see Settings System). Schema parity rules: `.claude/rules/settings-schema.md`.
- `Diagnostics/` — Analyzer exception harness (`XX0000`); see `.claude/rules/analyzer-exception-harness.md`.
- `Permissions/` — Shared permission model for AC0031 (missing) and AC0032 (unused): `DatabaseOperation`, `RequiredPermissionDetector`, `PermissionResolver`, the deliberately separate `DataTransferOperations`, and `DataTransferTableResolver` (the flow-sensitive `SetTables` ↔ executor pairing both cops rely on; build it once per body and pass it in when a caller inspects several executors). Why the two method maps stay disjoint: the `DataTransferOperations.cs` XML doc; what an *unresolvable* `TryGetFromDataTransfer` obliges each cop to do: `.claude/rules/diagnostics/ac0032-table-data-access-unused-permissions.md`.
- `Constants.cs` — `PermissionNodeXPath` (XPath for permission set XML) plus `Comment`, `Locked`, `MaxLength` label property name strings matching the SDK's `LabelPropertyHelper`.
- `RecordMethodClassification.cs` — see `.claude/rules/record-method-classification.md`.

## Why Reflection Is Used Everywhere

The `Microsoft.Dynamics.Nav.CodeAnalysis` SDK treats many types, properties, and enum values as internal or changes their signatures between Business Central releases. Direct references would break compilation against older (or newer) SDK versions. The reflection pattern used throughout Common:

1. **Enum values**: `EnumProvider` wraps every enum value in `Lazy<T>` using `Enum.Parse`. In DEBUG builds, missing values throw; in RELEASE, they silently return `default(T)`.
2. **Properties**: `PropertyAccessor`, `SymbolHelper` use `Lazy<PropertyInfo?>` with `GetProperty()` and cache results.
3. **Methods**: `StringHelper`, `ManifestHelper` use `Lazy<MethodInfo?>` with `GetMethod()` and create typed delegates. `StringHelper` detects the SDK method signature at runtime (with/without bool parameter); `ManifestHelper` on netstandard2.1 tries two type paths for AL version compatibility.
4. **Static fields**: `VersionProvider` uses `GetField()` with a "never supported" fallback when a field does not exist in the loaded SDK version.
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

`ALCopsSettingsProvider` exposes a single entry point: `GetSettings(IFileSystem?)`. Behavior: virtual FS check → parent traversal → assembly fallback. Results are cached in a `ConcurrentDictionary` keyed by `IFileSystem.GetDirectoryPath()`; a `MemoryFileSystem` returning `""` bypasses the cache. JSON parsing is case-insensitive and allows comments and trailing commas.

### Error handling

- Inaccessible directory during parent traversal: stops traversal (treats as boundary)
- Unreadable/malformed `alcops.json` (invalid syntax, unknown enum values, wrong types): silently returns defaults via a `JsonException` catch in `DeserializeSettings` (see #328 for planned improvement)
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

## Guidelines

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
4. **For nested-class properties with a default instance** (e.g. `public MySettings MyGroup { get; set; } = new();`): JSON deserializers ignore NRT annotations and happily set the property to `null` when the JSON contains `"MyGroup": null`, which then NREs on the first consumer access — violating the "malformed alcops.json → defaults" contract (see [issue #328](https://github.com/ALCops/Analyzers/issues/328)). Keep the public property non-nullable and normalize in `ALCopsSettingsProvider.DeserializeSettings` after the deserialize call: `settings.MyGroup ??= new MySettings();`. Consumers then use the property directly without `!` or a duplicate fallback.
   - Add a regression fixture that injects `{"MyGroup": null}` and asserts the analyzer falls back to defaults without NRE (see `StatementBlockSpacingNull` test case in `StatementBlocksSeparatedByBlankLine.cs` for a template).
5. Document the new setting in the project README and update `alcops.schema.json` (`.claude/rules/settings-schema.md`).

### Changing the Public API

Common is a **private dependency**: it is compiled into every cop package and ALCops does not support third parties extending or consuming it. Public methods, properties and classes may therefore be removed, renamed, or have their signatures changed freely, as long as every in-repo caller is updated in the same change - no compatibility overloads, no deprecation cycle. Revisit this if ALCops.Common ever ships as a package external consumers depend on.

- Do not change default values in `ALCopsSettings` without discussion (users may depend on them).
- When adding reflection for a new SDK version, keep the fallback path for older versions.

### Testing
ALCops.Common has **no dedicated test project**. It is tested indirectly through the 6 cop test projects. When modifying Common:
- Run the full test suite (`dotnet test` at the solution level) to verify no regressions.
- If adding a new utility, write tests in the cop test project that will use it.
- Pay special attention to conditional compilation paths; CI builds both `net8.0` and `netstandard2.1`.
