---
applyTo: 'src/ALCops.FormattingCop/**/CasingMismatch*'
---

# FC0002: Casing Mismatch

## Purpose

Reports when the casing of a keyword or identifier reference differs from its canonical form: the declaration for user symbols, the SDK's canonical name for built-in keywords, types, and members. AL is case-insensitive; FC0002 enforces the compiler's own spelling.

## Diagnostic properties

- **ID**: FC0002, category Style, severity Info (see `DiagnosticDescriptors.CasingMismatch`)
- **Two analyzers share the descriptor**: `CasingMismatchKeyword` (keyword tokens) and `CasingMismatchIdentifier` (identifiers, data types, properties, option/object access)
- **CodeFix**: `CodeFixes/CasingMismatchKeyword.cs` (keyword tokens only; identifier diagnostics carry `CanonicalText` in properties but have no CodeFix yet)

## Architecture

- `CasingMismatchKeyword`: `RegisterSymbolAction` per object kind; walks descendant tokens, compares keyword tokens against `SyntaxFactory.Token(kind).ValueText`. Skips tokens whose parent is a `*DataType` node or `IdentifierName`.
- `CasingMismatchIdentifier`: single iterative tree walk per object symbol. Dictionary-resolvable nodes are handled inline (fast); identifiers, qualified names, and triggers are batched for semantic-model resolution, grouped by (text, scope) so `GetSymbolInfo` runs once per group.

Key dictionaries (all `OrdinalIgnoreCase` keyed, value = canonical text):

| Dictionary | Source | Used for |
|---|---|---|
| `_navTypeKindDictionary` | `NavTypeKind` enum names + `Database` | Data type names (`SubtypedDataTypeSyntax`, `DataTypeSyntax`) |
| `_symbolKindDictionary` | `SymbolKind` enum names, **`XmlPort` remapped to `Xmlport`**, + `Database`, `ObjectType` | Left side of `::` object access and member after `Database::` etc. |
| `_objectTypeMemberDictionary` | `SymbolKind` enum names verbatim (`XmlPort`) | Members of `ObjectType::` |
| `_enumPropertyValuesByKind/Name` | Reflection over SDK `PropertyInfoLookup` | Enum property values |
| `KeywordTexts` | All `*Keyword` token texts | Skip identifiers named after keywords in semantic resolution |

## Design decisions

| Decision | Rationale |
|---|---|
| XmlPort casing is context-dependent, mirroring the SDK exactly | The AL compiler itself is inconsistent; FC0002 follows the compiler/IntelliSense, not a single invented spelling. See matrix below. Issue #432 and upstream LinterCop #729 confirmed by-design. |
| `XmlPort → Xmlport` remap in `_symbolKindDictionary` | The `::` left side and static class bind to the SDK's `XmlportClassTypeSymbol`, literally named `"Xmlport"` (`XmlportClassTypeSymbol.cs`). |
| `.Run`/`.Import`/`.Export` receiver (`Xmlport.Run`) is NOT analyzed | The `KeywordTexts` filter in `ResolveIdentifiers` skips identifiers named after keywords to avoid false positives on user symbols. Known false negative; kept intentionally. |
| Identifiers grouped by (text, scope) before `GetSymbolInfo` | Performance: one semantic call per distinct spelling per method scope. |

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

## Test coverage

Test classes: `CasingMismatchKeyword`, `CasingMismatchDeclaration`, `CasingMismatchBuiltInMethod` (the latter two both exercise `CasingMismatchIdentifier`).

**CasingMismatchKeyword HasDiagnostic (4 cases):** Codeunit, Enum, Table, XmlPort.
**CasingMismatchKeyword NoDiagnostic (5 cases):** Codeunit, Enum, Table, VariableNamedAfterKeyword, XmlPort.
**CasingMismatchKeyword HasFix (1 case):** ObjectKeyword.
**CasingMismatchDeclaration HasDiagnostic (20 cases):** DataType, EnumDataType, FieldGroup, LabelDataType, LabelProperties, LengthDataType, MemberAccessField, OptionDataType, Property, TextConstDataType, TestType, TriggerDeclaration, ObjectTypeOptionAccess, CrossScopeIdentifierGrouping, GlobalVarAndParam, GlobalVarAndParamThisPrefix, GlobalVarAndReturnValue, GlobalVarAndLocalVar, XmlPortDataType, XmlPortObjectAccess.
**CasingMismatchDeclaration NoDiagnostic (25 cases):** AccessByPermission, DataType, EnumDataType, FieldGroup, IdentifierNameSyntaxGrouping, LabelDataType, LabelProperties, LengthDataType, MemberAccessField, OptionDataType, Property, TextConstDataType, TestType, TriggerDeclaration, VariableNamedAfterKeyword, ObjectTypeOptionAccess, DeeplyNestedExpression, CrossScopeIdentifierGrouping, CrossScopeQualifiedNameGrouping, GlobalVarAndParam, GlobalVarAndParamThisPrefix, GlobalVarAndReturnValue, GlobalVarAndLocalVar, XmlPortDataType, XmlPortObjectAccess.
**CasingMismatchBuiltInMethod HasDiagnostic (7 cases):** FieldAccess, GlobalReferenceExpression, InvocationExpression, LocalReferenceExpression, ParameterReferenceExpression, ReturnValueReferenceExpression, XmlPortDataItemAccess.
**CasingMismatchBuiltInMethod NoDiagnostic (7 cases):** FieldAccess, GlobalReferenceExpression, InvocationExpression, LocalReferenceExpression, ParameterReferenceExpression, ReturnValueReferenceExpression, XmlPortDataItemAccess.
