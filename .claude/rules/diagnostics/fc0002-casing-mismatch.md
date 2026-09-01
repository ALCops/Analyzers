---
paths:
  - "src/ALCops.FormattingCop/**/CasingMismatch*"
---

# FC0002: CasingMismatch

## Purpose

Reports when the casing of a keyword or identifier reference differs from its canonical form: the declaration for user symbols, the SDK's canonical name for built-in keywords, types, and members. AL is case-insensitive; FC0002 enforces the compiler's own spelling.

## Design decisions

| Decision | Rationale |
|---|---|
| XmlPort casing is context-dependent, mirroring the SDK exactly | The AL compiler itself is inconsistent; FC0002 follows the compiler/IntelliSense, not a single invented spelling. See matrix below. Issue #432 and upstream LinterCop #729 confirmed by-design. |
| `XmlPort → Xmlport` remap in `_symbolKindDictionary` | The `::` left side and static class bind to the SDK's `XmlportClassTypeSymbol`, literally named `"Xmlport"` (`XmlportClassTypeSymbol.cs`). |
| `.Run`/`.Import`/`.Export` receiver (`Xmlport.Run`) is NOT analyzed | The `KeywordTexts` filter in `ResolveIdentifiers` skips identifiers named after keywords to avoid false positives on user symbols. Known false negative; kept intentionally. |
| Identifiers grouped by (text, scope) before `GetSymbolInfo` | Performance: one semantic call per distinct spelling per method scope. |
| Generic type arguments (`List of [...]`, `Dictionary of [...]`) walked by pushing the `GenericNamedDataTypeSyntax` node onto the existing stack | `ChildNodes()` yields only the type-argument `DataTypeSyntax` nodes (TypeName/`of`/brackets are tokens, never revisited), so the outer type name is not double-reported and nested generics recurse for free. Issue #255. |
| Object references after subtyped data types (`Record MyTable`, `Interface "IMyInterface"`) resolved via `GetSymbolInfo` on the inner `IdentifierNameSyntax` | The SDK's `GetSemanticInfoSymbolInNonMemberContext` derives the `SymbolKind` from the enclosing `SubtypedDataTypeSyntax.TypeName`, so one call returns the referenced object's canonical `Name` — no member model needed for declaration nodes. |
| Object references batched in a dedicated `objectReferences` list keyed by the `(TypeName, name)` tuple, NOT the `identifiers` list | The `identifiers` list groups by (text, method scope); `Record Customer` and a variable named `Customer` would share a group and cross-contaminate the canonical text (false positive + wrong fix). Kind must be in the key because `Record Foo` and `Codeunit Foo` resolve to different symbols. The type name is captured at the collection site in `WalkNode` (not re-derived via parent traversal), and the default case-sensitive tuple comparer matches `ResolveIdentifiers` — differently-cased duplicates just cost one extra `GetSymbolInfo`. |
| `ObjectIdSyntax` (`Record 18`) subtypes are not collected | IDs have no casing. |
| Namespace-qualified subtypes (`Record Ns.Path.MyTable`) resolved in a separate `ResolveQualifiedObjectReferences` pass | `GetSymbolInfo` on the `QualifiedNameSyntax` routes through the SDK's `GetSymbolFromObjectReference`, which has an explicit `QualifiedName` case (`LookupObjectTypeSymbol`). Namespace-part casing is compared right-aligned against `GetContainingNamespaceQualifiedNameWithReflection()` split on `.` (the reflection helper works on all TFMs; a null result skips namespace parts but still checks the object name). Not fed into `ResolveQualifiedNames` — its `Left.Kind == IdentifierName` branch assumes a field-in-object shape and early-returns. |

## Architecture

- Two analyzers share the descriptor `DiagnosticDescriptors.CasingMismatch`: `CasingMismatchKeyword` (keyword tokens) and `CasingMismatchIdentifier` (identifiers, data types, properties, option/object access).
- `CasingMismatchKeyword`: `RegisterSymbolAction` per object kind; walks descendant tokens, compares keyword tokens against `SyntaxFactory.Token(kind).ValueText`. Skips tokens whose parent is a `*DataType` node or `IdentifierName`.
- `CasingMismatchIdentifier`: single iterative tree walk per object symbol. Dictionary-resolvable nodes are handled inline (fast); identifiers, qualified names, triggers, and subtyped object references (simple and namespace-qualified) are batched for semantic-model resolution (`ResolveIdentifiers`/`ResolveQualifiedNames`/`ResolveTriggers`/`ResolveObjectReferences`/`ResolveQualifiedObjectReferences`), grouped so `GetSymbolInfo` runs once per group. `GenericDataType` is in the stack-push allow-list alongside `EnumDataType`/`LabelDataType` so type arguments are walked.

Key dictionaries (all `OrdinalIgnoreCase` keyed, value = canonical text):

| Dictionary | Source | Used for |
|---|---|---|
| `_navTypeKindDictionary` | `NavTypeKind` enum names + `Database` | Data type names (`SubtypedDataTypeSyntax`, `DataTypeSyntax`) |
| `_symbolKindDictionary` | `SymbolKind` enum names, **`XmlPort` remapped to `Xmlport`**, + `Database`, `ObjectType` | Left side of `::` object access and member after `Database::` etc. |
| `_objectTypeMemberDictionary` | `SymbolKind` enum names verbatim (`XmlPort`) | Members of `ObjectType::` |
| `_enumPropertyValuesByKind/Name` | Reflection over SDK `PropertyInfoLookup` | Enum property values |
| `KeywordTexts` | All `*Keyword` token texts | Skip identifiers named after keywords in semantic resolution |

### XmlPort casing matrix (SDK ground truth, `../nav-sdk-source`)

| Context | Canonical | SDK evidence |
|---|---|---|
| Object declaration keyword | `xmlport` | `SyntaxFacts` keyword text |
| Variable/parameter type | `XmlPort` | `NavTypeKindExtensions`: `NavTypeKind.XmlPort => "XmlPort"` |
| Static class (`Run`/`Import`/`Export`) | `Xmlport` | `XmlportClassTypeSymbol` name |
| `::` object access left side | `Xmlport` | Binder binds to `XmlportClassTypeSymbol` |
| `ObjectType::XmlPort` member | `XmlPort` | `SymbolKind` enum name |

## Known issues

- `Xmlport.Run` receiver with wrong casing (`XMLPORT.Run`) is not flagged (keyword-named identifier filter, see design decisions).
- Option members of platform table fields (e.g. `"Object Type"::XMLport`) resolve via semantic model to the platform's own casing.

## CodeFix: CasingMismatchKeyword

`CodeFixes/CasingMismatchKeyword.cs` (class `CasingMismatchCodeFix`) is registered for the whole FC0002 ID and fixes every diagnostic carrying `CanonicalText` in properties — keyword and identifier diagnostics alike. It replaces the diagnostic span with `CanonicalText.QuoteIdentifierIfNeededWithReflection()`, which re-quotes names that need quotes (`"MY TABLE"` → `"My Table"`) and drops unnecessary quotes (`"IMYINTERFACE"` → `IMyInterface`).
