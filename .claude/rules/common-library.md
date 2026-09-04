---
paths:
  - "src/ALCops.Common/**"
---

# ALCops.Common: Shared Library for AL Analyzers

## Project Role

ALCops.Common is the shared foundation library referenced by every cop, every test project, and the aggregator package. Any change here affects every analyzer; update all callers in the same change.

Target frameworks, LangVersion, nullable enforcement and conditional package references are defined in the csproj (the csproj is the source of truth). Always use `#if NETSTANDARD2_1` / `#if NET8_0_OR_GREATER` guards when APIs differ between target frameworks (e.g. `System.Text.Json` for net8.0, `Newtonsoft.Json` for netstandard2.1). See `.claude/rules/netstandard21-compatibility.md`.

## Directory Purposes

- `Extensions/` — Extension methods on SDK types, one static class per extended type. Home of `GetSymbolSafe()` and `GetReceiverTableType` (`.claude/rules/symbol-resolution.md`, `record-receiver-forms.md`) and the shared `IsTemporary()` checks.
- `Helpers/` — Utilities wrapping SDK functionality: AppSourceCop configuration and mandatory affixes (not cached; cache per compilation at the call site), manifest access (`ManifestHelper.GetManifest` throws `FileNotFoundException` in test compilations; treat as null), OData name mangling, acronym registry.
- `Reflection/` — Runtime access to internal or version-dependent SDK members; the most sensitive area of Common. `EnumProvider` is the only allowed way to name an SDK enum value.
- `Settings/` — `ALCopsSettings` (defaults) and `ALCopsSettingsProvider` (hierarchical `alcops.json` lookup, below). Load failures become CM0001. Schema parity: `.claude/rules/settings-schema.md`.
- `Analyzers/` — Common's own `CM` diagnostics. Hosting an analyzer in Common is loader-safe; a Common *base class* for cop analyzers is not (`.claude/rules/diagnostics/cm0001-configuration-could-not-be-loaded.md`, `analyzer-exception-harness.md`).
- `Diagnostics/` — the exception harness (`.claude/rules/analyzer-exception-harness.md`).
- `Permissions/` — the permission model shared by AC0031 and AC0032 (`RequiredPermissionDetector`, `PermissionResolver`, `DataTransferOperations`, `DataTransferTableResolver`) and the AZ AL Dev Tools-compatible ordering used by FC0004 (`PermissionEntryComparer`, `NaturalStringComparer`). Why `DataTransferOperations` stays out of `MethodOperationMap`: its XML doc. What an unresolvable `DataTransfer` obliges a cop to do: `.claude/rules/diagnostics/ac0032-table-data-access-unused-permissions.md`.
- `FlowTerminatingBuiltIns.cs` — classifies `Dialog.Error`, `Table.FieldError` and `FieldRef.FieldError` by exact built-in class and method (`symbol-resolution.md`). Collectible `Error(ErrorInfo)` is deliberately treated as terminating because collectibility depends on the enclosing `ErrorBehavior::Collect` scope.
- `RecordMethodClassification.cs` — `.claude/rules/record-method-classification.md`.
- `Constants.cs` — permission-set XPath and the label property names that mirror the SDK's `LabelPropertyHelper`.

## Why Reflection Is Used Everywhere

The `Microsoft.Dynamics.Nav.CodeAnalysis` SDK treats many types, properties, and enum values as internal or changes their signatures between Business Central releases. Direct references would break compilation against older (or newer) SDK versions. The reflection pattern used throughout Common:

1. **Enum values**: `EnumProvider` wraps every enum value in `Lazy<T>` using `Enum.Parse`. A value missing from the loaded SDK resolves to a fallback, identical in Debug and Release: `default(T)` for most enums, but for `SymbolKind` an out-of-range sentinel (`int.MaxValue`), because `default(SymbolKind)` is `Module` (an unresolved kind passed to `RegisterSymbolAction` would fire for the module symbol) and `Undefined` (-1) crashes the SDK driver's per-kind bucketing; the driver skips kinds above the loaded enum's maximum.
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

`ALCopsSettingsProvider` exposes two entry points: `GetSettings(IFileSystem?)` (what analyzers use) and `GetLoadResult(IFileSystem?)` (settings **plus** recorded `SettingsLoadFailure`s; `GetSettings` is a thin wrapper over it, and the CM0001 analyzer is its only failure consumer). Behavior: virtual FS check → parent traversal → assembly fallback. Load results (including failures) are cached in a `ConcurrentDictionary` keyed by `IFileSystem.GetDirectoryPath()`; a `MemoryFileSystem` returning `""` bypasses the cache. JSON parsing is case-insensitive and allows comments and trailing commas.

### Error handling

- Inaccessible directory during parent traversal: stops traversal (treats as boundary)
- Unreadable or malformed `alcops.json` (invalid syntax, unknown enum values, wrong types): returns defaults — that fallback contract is unchanged — and records an `Unreadable`/`Invalid` failure that `Analyzers/ConfigurationCouldNotBeLoaded` reports as CM0001. An unreadable app-folder file does **not** fall through to a parent-directory file.
- Unknown top-level keys (typo'd setting names): recognized settings still apply; one `UnknownSetting` failure per key. The known-key set is reflection-derived from `ALCopsSettings` properties (case-insensitive, `$schema` allowlisted), so new settings extend it automatically.
- `MemoryFileSystem` (in tests, `GetDirectoryPath()` returns `""`): only checks virtual FS, no parent traversal
- Only `IFileSystem` members present at the AL 12 interface floor may be called (`Exists`, `OpenRead`, `GetDirectoryPath`, …). `GetAbsolutePath` is not among them — the netstandard2.1 binary would throw `MissingMethodException` on old compilers.

Users configure settings by placing an `alcops.json` file in their AL project root or any parent directory:
```json
{
    "CognitiveComplexityThreshold": 20,
    "CyclomaticComplexityThreshold": 10,
    "MaintainabilityIndexThreshold": 15
}
```

Settings are cached per directory path for the analyzer session lifetime. There is no public cache-invalidation API, so an edited `alcops.json` takes effect only after the language server restarts; tests inject an isolated `IFileSystem` (typically `MemoryFileSystem` or a purpose-built `RelativeFileSystem`) to avoid contaminating the cache.

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
2. Add a new `private static readonly Lazy<T>` field using `ParseEnum<T>(nameof(...))` or a string literal for values that may not exist in all SDK versions. In the `SymbolKind` class use its `Parse(...)` helper so a missing member resolves to the out-of-range `Unresolved` sentinel, never `Module`. Before relying on `default(T)` for a new enum, check that its zero member is inert; if it is a real, dispatchable value, give that nested class its own fallback helper like `SymbolKind.Parse`.
3. Add a public static property that returns `_field.Value`.
4. If the enum value requires conditional compilation for different frameworks, use `#if` guards.

### How to Add a New Setting
1. Add a new property with a default value to `ALCopsSettings.cs`.
2. No changes needed to `ALCopsSettingsProvider.cs` for scalar / string / list / dictionary properties — JSON deserialization picks them up automatically.
3. **For enum-typed properties**, add a converter registration to `ALCopsSettingsProvider.cs`: `JsonStringEnumConverter` in `_jsonOptions.Converters` (net8+) and `StringEnumConverter` in `_jsonSettings.Converters` (netstandard2.1). Both are case-insensitive by default. Then add a schema-parity guard test that compares `Enum.GetNames(typeof(YourEnum))` with the `enum` array in `alcops.schema.json` (see `StatementBlockSpacingSchema` in `src/ALCops.FormattingCop.Test/Rules/StatementBlocksSeparatedByBlankLine/` for a template).
4. **For nested-class properties with a default instance** (e.g. `public MySettings MyGroup { get; set; } = new();`): JSON deserializers ignore NRT annotations and happily set the property to `null` when the JSON contains `"MyGroup": null`, which then NREs on the first consumer access — violating the "malformed alcops.json → defaults" contract. Keep the public property non-nullable and normalize in `ALCopsSettingsProvider.DeserializeSettings` after the deserialize call: `settings.MyGroup ??= new MySettings();`. Consumers then use the property directly without `!` or a duplicate fallback.
   - Add a regression fixture that injects `{"MyGroup": null}` and asserts the analyzer falls back to defaults without NRE (see `StatementBlockSpacingNull` test case in `StatementBlocksSeparatedByBlankLine.cs` for a template). An explicit `null` is normalized, not reported as CM0001.
5. Document the new setting in the project README and update `alcops.schema.json` (`.claude/rules/settings-schema.md`).

### Changing the Public API

Common is a **private dependency**: it is compiled into every cop package and ALCops does not support third parties extending or consuming it. Public methods, properties and classes may therefore be removed, renamed, or have their signatures changed freely, as long as every in-repo caller is updated in the same change - no compatibility overloads, no deprecation cycle. Revisit this if ALCops.Common ever ships as a package external consumers depend on.

- Do not change default values in `ALCopsSettings` without discussion (users may depend on them).
- When adding reflection for a new SDK version, keep the fallback path for older versions.

### Testing
`src/ALCops.Common.Test` holds unit tests for pure helpers (settings provider, acronym registry, `NaturalStringComparer`) plus the CM0001 analyzer tests (a manual `Compilation.Create` + `CompilationWithAnalyzers` harness in `Analyzers/`, because RoslynTestKit's marker-based asserts cannot match `Location.None`); everything else that needs an AL compilation is tested through the 6 cop test projects. The shared `Conventions/` tests run here too now that Common ships an analyzer. When modifying Common:
- Run the full test suite (`dotnet test` at the solution level) to verify no regressions.
- If adding a new utility, write tests in the cop test project that will use it.
- Pay special attention to conditional compilation paths; CI builds both `net8.0` and `netstandard2.1`.
