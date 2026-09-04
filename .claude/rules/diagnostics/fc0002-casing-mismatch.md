---
paths:
  - "src/ALCops.FormattingCop/**/CasingMismatch*"
  - "src/ALCops.FormattingCop.Test/Rules/CasingMismatchBuiltInMethod/**"
  - "src/ALCops.FormattingCop.Test/Rules/CasingMismatchDeclaration/**"
  - "src/ALCops.FormattingCop.Test/Rules/CasingMismatchKeyword/**"
---

# FC0002: CasingMismatch

## Purpose

Reports when the casing of a keyword or identifier reference differs from its canonical form: the declaration for user symbols, the SDK's canonical name for built-in keywords, types, and members. AL is case-insensitive; FC0002 enforces the compiler's own spelling.

Registers `RegisterSymbolAction` on the application object symbol kinds; main types `CasingMismatchKeyword` (keyword tokens) and `CasingMismatchIdentifier` (identifiers, data types, properties, option/object access), sharing one descriptor.

## Design decisions

| Decision | Rationale |
|---|---|
| XmlPort casing is context-dependent, mirroring the SDK exactly (see SDK facts) | The AL compiler itself is inconsistent; FC0002 follows the compiler/IntelliSense rather than one invented spelling. Confirmed by-design in [#432](https://github.com/ALCops/Analyzers/issues/432) and upstream [LinterCop #729](https://github.com/StefanMaron/BusinessCentral.LinterCop/issues/729). |
| Identifiers grouped by (text, method scope) before `GetSymbolInfo` | One semantic call per distinct spelling per scope instead of one per occurrence. |
| Generic type arguments (`List of [...]`, `Dictionary of [...]`) walked by pushing the `GenericNamedDataTypeSyntax` node onto the existing node stack rather than a dedicated pass | `ChildNodes()` yields only the type-argument nodes, so the outer type name is not double-reported and nested generics recurse for free ([#255](https://github.com/ALCops/Analyzers/issues/255)). |
| Object references after subtyped data types (`Record MyTable`, `Interface "IMyInterface"`) resolved via `GetSymbolInfo` on the inner `IdentifierNameSyntax`, not the member model | The SDK derives the `SymbolKind` from the enclosing `SubtypedDataTypeSyntax.TypeName`, so one call returns the referenced object's canonical `Name` for declaration nodes. |
| Object references batched in a dedicated list keyed by `(TypeName, name)`, not in the identifier list | Sharing the (text, scope) groups would let `Record Customer` and a variable named `Customer` cross-contaminate the canonical text (false positive and wrong fix); the kind is in the key because `Record Foo` and `Codeunit Foo` resolve to different symbols. |
| Namespace-qualified subtypes (`Record Ns.Path.MyTable`) resolved in a separate pass, not fed into the qualified-name resolution | The qualified-name pass assumes a field-in-object shape and early-returns; namespace-part casing is compared right-aligned against `GetContainingNamespaceQualifiedNameWithReflection()` split on `.`. |

## Deliberate non-reports

- Receivers named after keywords, such as `XMLPORT.Run` / `.Import` / `.Export`: identifiers matching `KeywordTexts` are skipped during semantic resolution to avoid false positives on user symbols. Known false negative, kept intentionally.
- `ObjectIdSyntax` subtypes (`Record 18`): IDs have no casing.
- Namespace parts when `GetContainingNamespaceQualifiedNameWithReflection()` returns null; the object name itself is still checked.

## Known issues

- Option members of platform table fields (e.g. `"Object Type"::XMLport`) resolve via the semantic model to the platform's own casing, whatever it is.

## SDK facts

- XmlPort canonical spelling differs by context: `xmlport` as object keyword (`SyntaxFacts`), `XmlPort` as data type (`NavTypeKindExtensions`) and as `ObjectType::XmlPort` member (`SymbolKind` enum), `Xmlport` as static class and `::` left side (bound to `XmlportClassTypeSymbol`, literally named `"Xmlport"`).
- `GetSemanticInfoSymbolInNonMemberContext` derives the `SymbolKind` from the enclosing `SubtypedDataTypeSyntax.TypeName`, so `GetSymbolInfo` on the inner identifier of a subtyped data type resolves the referenced object.
- `GetSymbolInfo` on a `QualifiedNameSyntax` subtype routes through `GetSymbolFromObjectReference`, which has an explicit `QualifiedName` case (`LookupObjectTypeSymbol`).

## CodeFix: CasingMismatchCodeFix

| Decision | Rationale |
|---|---|
| One provider (`CodeFixes/CasingMismatchKeyword.cs`) registered for the whole FC0002 ID, fixing every diagnostic that carries `CanonicalText` in its properties | Keyword and identifier diagnostics share the same fix shape, so one provider covers both analyzers. |
| Replacement text is `CanonicalText.QuoteIdentifierIfNeededWithReflection()` | Re-quotes names that need quotes (`"MY TABLE"` becomes `"My Table"`) and drops unnecessary quotes (`"IMYINTERFACE"` becomes `IMyInterface`) instead of copying the original quoting. |
