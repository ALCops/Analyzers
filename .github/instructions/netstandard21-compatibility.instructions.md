---
applyTo: 'src/ALCops.*/**'
---

# netstandard2.1 Backward Compatibility

All analyzer and common projects multi-target in CI: `netstandard2.1;net8.0;net10.0`. Several C# 9+ features and newer SDK APIs are unavailable in `netstandard2.1`. Code that compiles fine locally (net8.0 only) will fail in CI without conditional compilation guards.

## C# language features that require guards

| Feature | Problem on netstandard2.1 | Guard pattern |
|---|---|---|
| `record` / `record struct` | CS0518: `IsExternalInit` is not defined | `#if NETSTANDARD2_1` with manual struct/class |
| `init` property accessors | CS0518: `IsExternalInit` is not defined | `#if NETSTANDARD2_1` with `set` accessor |
| `with` expressions on structs | Depends on `IsExternalInit` | Avoid or guard with `#if` |

## SDK API differences between target frameworks

Some `Microsoft.Dynamics.Nav.CodeAnalysis` APIs only exist in newer SDK versions (net8.0). When using these APIs, provide a netstandard2.1 fallback.

| API | Available in | netstandard2.1 workaround |
|---|---|---|
| `IFieldSymbol.Type` | net8.0 only | `fieldSymbol.OriginalDefinition.GetTypeSymbol()` (requires `using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols`) |

### Pattern for `IFieldSymbol.Type`

Requires `using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;` for the `GetTypeSymbol()` extension method on netstandard2.1.

```csharp
#if NETSTANDARD2_1
var fieldType = fieldSymbol.OriginalDefinition.GetTypeSymbol();
if (fieldType?.NavTypeKind == EnumProvider.NavTypeKind.Blob)
    return;
#else
if (fieldSymbol.Type?.NavTypeKind == EnumProvider.NavTypeKind.Blob)
    return;
#endif
```

Existing examples:
- `ALCops.PlatformCop/Analyzers/RecordGetProcedureArguments.cs` (lines 129-133)
- `ALCops.PlatformCop/Analyzers/TransferFieldsSchemaCompatibility.cs` (lines 552-556)
- `ALCops.PlatformCop/CodeFixes/UsePartialRecordsOnRead.cs` (FieldAccessCollector)

## Required pattern for record types

Use `#if NETSTANDARD2_1` to provide a manual struct for the older target, and a `record struct` for net8.0+:

```csharp
#if NETSTANDARD2_1
    private readonly struct MyInfo
    {
        public string Name { get; }
        public int Value { get; }

        public MyInfo(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
#else
    private readonly record struct MyInfo(string Name, int Value);
#endif
```

## Required pattern for init-only properties

Use `set` instead of `init` on netstandard2.1:

```csharp
#if NETSTANDARD2_1
    public string? MyProperty { get; set; }
#else
    public string? MyProperty { get; init; }
#endif
```

## Existing examples in the codebase

| File | Pattern |
|---|---|
| `ALCops.LinterCop/Analyzers/CognitiveComplexityRecursionGraphService.cs` | `MethodDeclarationInfo` record struct |
| `ALCops.PlatformCop/Analyzers/PartialRecordOperations.cs` | `ReadInfo` record struct |
| `ALCops.PlatformCop/Analyzers/TransferFieldsRelations.cs` | `ObjectName` and `TableRelation` record structs |
| `ALCops.Common/Helpers/AppSourceCopConfigurationProvider.cs` | `init` to `set` accessor |
| `ALCops.LinterCop/CodeFixes/BuiltInDateTimeMethod.cs` | `CodeFixProperties` record |
| `ALCops.LinterCop/CodeFixes/ObjectIdInDeclaration.cs` | `CodeFixProperties` record |

## Checklist before committing

1. Search your new/modified files for `record `, `record struct`, and `{ get; init` patterns.
2. If any are present, wrap them in `#if NETSTANDARD2_1` / `#else` / `#endif` guards.
3. The netstandard2.1 block must use a regular `struct` or `class` with explicit constructor and get-only properties (no `init`).
4. Search for `IFieldSymbol` `.Type` usage; use `OriginalDefinition.GetTypeSymbol()` on netstandard2.1 (requires `using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols`).
5. When in doubt, look at existing examples listed above.
