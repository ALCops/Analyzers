---
paths:
  - "src/ALCops.*/Analyzers/**"
  - "src/ALCops.*/CodeFixes/**"
---

# Symbol resolution and canonical names

How to get from syntax or operations to symbols, and which SDK members are misleading.

## Symbols, never text

`syntax.ToString()`, `Identifier.ValueText` and friends are case-sensitive, whitespace-dependent, and miss implicit conversions; the source spelling need not match the canonical name. Resolve first (`IOperation.GetSymbolSafe()`, `SemanticModel.GetSymbolInfo()`, `SemanticModel.GetDeclaredSymbol()`), then compare symbols (`symbol.Equals(other)`) or their compiler-resolved `Name` and `Kind`. When a text fallback is unavoidable (a variable name from `IConversionExpression.Syntax` when `GetSymbolSafe()` is null), call `.UnquoteIdentifier()` (`Microsoft.Dynamics.Nav.CodeAnalysis.Utilities`) on the `ValueText` first: AL identifiers may be quoted (`"My Table"`), `ValueText` keeps the quotes, `ISymbol.Name` does not.

## Getting a SemanticModel

- `SyntaxNodeAnalysisContext` and `CodeBlockAnalysisContext` expose `SemanticModel` directly.
- `SymbolAnalysisContext`: `ctx.Compilation.GetSemanticModel(symbol.DeclaringSyntaxReference.GetSyntax(ct).SyntaxTree)`.
- `OperationAnalysisContext` exposes only `Compilation`; the bound operation tree already carries symbols, so you rarely need one.
- `Compilation.GetSemanticModel` is **uncached** (a new model per call). Obtain it once per callback, not per node.

## Which API resolves what

| Method | Purpose |
|---|---|
| `GetDeclaredSymbol(node)` | The symbol a declaration node introduces (objects, methods, fields, variables, properties). Cheap for methods: it resolves the signature without binding the body. |
| `GetSymbolInfo(node)` | The symbol a reference resolves to. `CandidateSymbols` holds the overloads when binding failed. |
| `GetOperation(node)` | The bound operation tree; `IOperation.Type` is how you get a type. Expensive outside `CodeBlockAction` (`analyzer-performance.md`). |
| `GetTypeInfo(node)` | **Internal** in this SDK. Use `GetOperation(node)?.Type`, or switch on `GetSymbolInfo`: `IVariableSymbol.Type`, `IParameterSymbol.ParameterType`, `IMethodSymbol.ReturnValueSymbol?.ReturnType`. |

Not every node resolves:

| Node | `GetDeclaredSymbol` | `GetSymbolInfo` | Canonical name via |
|---|---|---|---|
| `PropertySyntax` | `IPropertySymbol` | no | `PropertyKind.ToString()` |
| `PropertyNameSyntax` | no | `IPropertySymbol` | `PropertyKind.ToString()` |
| `EnumPropertyValueSyntax`, `SimpleNamedDataTypeSyntax`, `MemberAttributeSyntax` | no | no | dictionaries: `PropertyAccessor` option names, `NavTypeKind` names, `EnumProvider.AttributeKind.CanonicalNames` |
| `IdentifierNameSyntax`, `QualifiedNameSyntax` | no | various | `ISymbol.Name` |
| `TriggerDeclarationSyntax` | `IMethodSymbol` | no | `IMethodSymbol.Name` |
| `FieldSyntax` | `IFieldSymbol` | no | user-defined; `Name` is source |

`IPropertySymbol.PropertyKind` is canonical; `Value`, `ValueText` and `Value.ToString()` are **source text** (the value is a `SourceOptionSymbol`). Only `PropertyKind.ToString()` is safe for the property name; enum property values need the `PropertyAccessor` dictionary.

### Canonical enum property values (`PropertyInfoLookup`)

`PropertyInfoLookup.Lookup(SymbolKind, PropertyKind)` returns an `EnumPropertyTypeInfo` whose `Options` carry the canonical names of enum property values (`Access = Public`, `PageType = Card`). The types are internal, so `ALCops.Common/Reflection/PropertyAccessor.cs` reads them via reflection once, merging options across all `SymbolKind`s (a few kinds such as `Access`, `Subtype` and `Type` differ per symbol kind). New SDK enum properties are discovered automatically. Wrap assembly scans in `try/catch (ReflectionTypeLoadException)`: some SDK types fail to load. Other canonical sources: property names via `PropertyKind.ToString()`, data types via `NavTypeKind` names, attributes via `EnumProvider.AttributeKind.CanonicalNames`, user identifiers via `GetSymbolInfo().Symbol.Name`.

## Batching identifier resolution

When many identifiers may refer to the same symbol, group by `Identifier.ValueText` (ordinal), resolve one representative per group with `GetSymbolInfo`, and apply the result to the group. See `CasingMismatch` in FormattingCop.

## Operation-level resolution

Inside an operation tree prefer `IOperation.GetSymbolSafe()` (a type check) over `GetSymbolInfo` (a semantic query).

**`GetSymbolSafe()` exists because the SDK's `GetSymbol()` throws.** `OperationExtensions.GetSymbol()` switches on `OperationKind.FieldAccess` and casts to `IFieldAccess`, but `BoundApplicationObjectAccess` (`DATABASE::X`, `CODEUNIT::X`, `TABLE::X`, and the other object-access forms) and the internal `BoundObjectAccess` report that kind without implementing the interface: `InvalidCastException`. `ALCops.Common/Extensions/OperationExtensions.cs` returns `IApplicationObjectAccess.ApplicationObjectTypeSymbol` for the first, null for any other `FieldAccess`-kind operation that is not an `IFieldAccess`, and delegates otherwise, with no try/catch on the happy path. Microsoft's CodeCop 243 sidesteps the same bug by casting to `IApplicationObjectAccess` directly.

**Unwrap `IConversionExpression`.** Arguments are often wrapped in an implicit conversion whose own symbol is null; fall back to `conversion.Operand.GetSymbolSafe()` (`TransferFieldsSchemaCompatibility`, `PossibleOverflowAssigning`, `PartialRecordOperations`, `UnnecessaryRecordParameterInMethodCall`).

**Same-module check.** "Defined in the developer's own app" is `ctx.ContainingSymbol.ContainingModule == targetMethod.ContainingModule` (reference equality; the compiler creates one module object per app). Do not compare app ids or names.

## Method symbols

`IMethodSymbol.LocalVariables` (`IVariableSymbol.Name`, `.Type`) and `.Parameters` (`IParameterSymbol.Name`, `.ParameterType`, `.IsVar`) come pre-typed from `GetDeclaredSymbol(methodSyntax)`; `ReturnValueSymbol` (`.ReturnType`, `.IsNamed`) is the possibly named return value. This is the basis of the variable-map pattern in `analyzer-performance.md`.

Facts about method symbols that look discriminating but are not:

- **Bad calls.** When binding fails, a single candidate keeps the real `IMethodSymbol`; several candidates yield an `ErrorMethodSymbol` (`MethodKind.Method`, `Name` = callee, `ContainingSymbol` = receiver type or the first candidate's type). The result is still a `BoundCall : IInvocationExpression` with `IOperation.IsInvalid == true`; `GetSymbolInfo(...).CandidateSymbols` returns the original overloads. Branch on `IsInvalid` and receiver kind, not on symbol type.
- **`Location` is null for more than built-ins.** `ReferenceMethodSymbol` (procedures from referenced apps) has null `DeclaringSyntaxReference` and `Location`, as do built-ins and `ErrorMethodSymbol`; `IsSynthesized` is not overridden. None of these separate "built-in" from "external procedure".
- **Built-in identity is class plus method, not name.** `MethodKind.BuiltInMethod` plus a name can match a future built-in on another class; anchor classification to the exact built-in class (`Dialog`, `Table`, `FieldRef`, and so on) and method. See `FlowTerminatingBuiltIns` in Common. Bare `Error(...)` is a static built-in on the internal `Dialog` class; the `Table` and `FieldRef` built-in classes have `NavTypeKind.None`, so `NavTypeKind` cannot anchor a clean binding.
- AL forbids object members that shadow a built-in, so a bare built-in name inside an object is never a user procedure.

## Key symbol interfaces

| Interface | Members you will need |
|---|---|
| `IVariableSymbol` | `Name`, `Type`, `VariableKind` |
| `IParameterSymbol` | `Name`, `ParameterType`, `IsVar`, `Ordinal` |
| `IMethodSymbol` | `Name`, `MethodKind`, `LocalVariables`, `Parameters`, `ReturnValueSymbol`, `ContainingModule` |
| `IRecordTypeSymbol` | `BaseTable`, `Temporary`, `OriginalDefinition` (the `ITableTypeSymbol`); see `record-receiver-forms.md` |
| `ITableTypeSymbol` | `Id`, `Name`, `TableType` |
| `IApplicationObjectTypeSymbol` | `Kind`, `Id`, `Name`, `GetMembers()`, `GetProperty()` |
| `IApplicationObjectExtensionTypeSymbol` | `Target` (the extended object) |

Interfaces and control add-ins implement `IObjectTypeSymbol` but not `IApplicationObjectTypeSymbol`, so `GetContainingApplicationObjectTypeSymbol()` returns null for them; resolve the application object first, then fall back to the object type. Request pages are `IObjectTypeSymbol` with `Local` accessibility.

## Comparing AL names: `SemanticFacts`

AL is case-insensitive. Use the SDK's `SemanticFacts` (`Microsoft.Dynamics.Nav.CodeAnalysis`) so intent is explicit and matches Microsoft's CodeCops:

| Scenario | Use |
|---|---|
| Equality, non-null | `SemanticFacts.IsSameName(a, b)` |
| Equality, nullable (`SyntaxToken.ValueText`) | `a.IsSameName(b)` (`ALCops.Common.Extensions.StringExtensions`; false when either is null) |
| Set, dictionary, GroupBy comparer | `SemanticFacts.NameEqualityComparer` |
| StartsWith, EndsWith, Contains, IndexOf | `SemanticFacts.NameEqualityComparison` |
| Sorting | `SemanticFacts.NameComparer` |

Keep `OrdinalIgnoreCase` (or the documented culture compare) for non-AL text: property values (`"Always"`, `"#All"`), file paths, diagnostic ids, translation keys, user-configured affix lists, permission character strings, and FC0004's `NaturalStringComparer` (`InvariantCultureIgnoreCase`, matching AZ AL Dev Tools).
