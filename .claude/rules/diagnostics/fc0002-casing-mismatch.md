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

Registers `RegisterSymbolAction` on the application object symbol kinds, including every extension kind and page customizations (control add-ins are not registered); main types `CasingMismatchKeyword` (keyword tokens) and `CasingMismatchIdentifier` (identifiers, data types, properties, option/object access), sharing one descriptor.

## Design decisions

| Decision | Rationale |
|---|---|
| XmlPort casing is context-dependent, mirroring the SDK exactly (see SDK facts) | The AL compiler itself is inconsistent; FC0002 follows the compiler/IntelliSense rather than one invented spelling. Confirmed by-design in [#432](https://github.com/ALCops/Analyzers/issues/432) and upstream [LinterCop #729](https://github.com/StefanMaron/BusinessCentral.LinterCop/issues/729). |
| Registered on every extension kind including page customizations; control add-ins stay out until their own issue is decided | A customization body is walked exactly like a page extension body: `IPageCustomizationTypeSymbol` is an `IPageExtensionBaseTypeSymbol`, so its `modify`/`move*` targets and properties resolve through the same paths, and `PageCustomizationObject` is already in the identifier skip list for the object name. A control add-in declares events and procedures without bodies, a different member shape that needs its own fixtures and decision ([#623](https://github.com/ALCops/Analyzers/issues/623)). |
| Identifiers grouped by (text, method scope) before `GetSymbolInfo` | One semantic call per distinct spelling per scope instead of one per occurrence. |
| A group whose representative has no symbol falls back to `GetMethodInvokedByAssignmentSyntax`, so property-style setter calls (`Proc := Value`, `Obj.Proc := Value`, `this.Proc := Value`) are checked like parenthesised calls | The binder gives the setter's target name no symbol; mechanism and cost in `symbol-resolution.md` (Property-style setter calls). The walker already collects the name in all three forms, so no collection change is needed. |
| Generic type arguments (`List of [...]`, `Dictionary of [...]`) walked by pushing the `GenericNamedDataTypeSyntax` node onto the existing node stack rather than a dedicated pass | `ChildNodes()` yields only the type-argument nodes, so the outer type name is not double-reported and nested generics recurse for free ([#255](https://github.com/ALCops/Analyzers/issues/255)). |
| Object references after subtyped data types (`Record MyTable`, `Interface "IMyInterface"`) resolved via `GetSymbolInfo` on the inner `IdentifierNameSyntax`, not the member model | The SDK derives the `SymbolKind` from the enclosing `SubtypedDataTypeSyntax.TypeName`, so one call returns the referenced object's canonical `Name` for declaration nodes. |
| Object references batched in a dedicated list keyed by `(TypeName, name)`, not in the identifier list | Sharing the (text, scope) groups would let `Record Customer` and a variable named `Customer` cross-contaminate the canonical text (false positive and wrong fix); the kind is in the key because `Record Foo` and `Codeunit Foo` resolve to different symbols. |
| Namespace-qualified subtypes (`Record Ns.Path.MyTable`) resolved in a separate pass, not fed into the qualified-name resolution | The qualified-name pass assumes a field-in-object shape and early-returns; namespace-part casing is compared right-aligned against `GetContainingNamespaceQualifiedNameWithReflection()` split on `.`. |
| No text-based keyword filter before resolution: every identifier group with a symbol is compared to `symbol.Name`, including built-in class receivers (`XMLPORT.Run` -> `Xmlport`, `CODEUNIT.RUN` -> `Codeunit`) and keyword-named user symbols (`Continue`, `Action`, `Type`, `Code`) | Keyword texts used as names are plain identifier tokens, so a text filter never protected a keyword; it only hid user symbols and the receiver form of the built-in classes, which the `::` form already reports ([#135](https://github.com/ALCops/Analyzers/issues/135)). |
| The `continue;` statement is reported against the SDK's `ContinueKeyword` text when its identifier sits directly under an `ExpressionStatement` and binds to no symbol | Mirrors `Binder.MatchesAmbiguousKeyword` (runtime 15.0+). Syntax-only, no `GetOperation`; the kind is read through `EnumProvider` because the member is absent at the netstandard2.1 floor. A variable named `Continue` binds first, so the variable and the statement resolve independently per spelling and method. |
| `Rec` is collected like any identifier | Every synthesis site (table, tableextension, page, page extension/customization, request page, report, xmlport, `TableNo` `OnRun` local) names it literally `Rec`, so `rec`/`REC` are reported against `Rec`; a user parameter named `Rec`/`rec` gets its declared casing. |

## Deliberate non-reports

- `ObjectIdSyntax` subtypes (`Record 18`): IDs have no casing.
- Namespace parts when `GetContainingNamespaceQualifiedNameWithReflection()` returns null; the object name itself is still checked.

## Known issues

- Option members of platform table fields (e.g. `"Object Type"::XMLport`) resolve via the semantic model to the platform's own casing, whatever it is.

## SDK facts

- XmlPort canonical spelling differs by context: `xmlport` as object keyword (`SyntaxFacts`), `XmlPort` as data type (`NavTypeKindExtensions`) and as `ObjectType::XmlPort` member (`SymbolKind` enum), `Xmlport` as static class and `::` left side (bound to `XmlportClassTypeSymbol`, literally named `"Xmlport"`).
- `GetSemanticInfoSymbolInNonMemberContext` derives the `SymbolKind` from the enclosing `SubtypedDataTypeSyntax.TypeName`, so `GetSymbolInfo` on the inner identifier of a subtyped data type resolves the referenced object.
- `GetSymbolInfo` on a `QualifiedNameSyntax` subtype routes through `GetSymbolFromObjectReference`, which has an explicit `QualifiedName` case (`LookupObjectTypeSymbol`).
- `continue` is never a keyword token: `SyntaxFacts.GetALKeywordKind` has no `CONTINUE`, and `continue;` is an `ExpressionStatementSyntax` over an `IdentifierNameSyntax`. The binder rebinds it as `BoundContinueStatement` only when the identifier is unbound and the runtime is Spring2025 or later; `GetSymbolInfo` on it returns no symbol. `SyntaxKind.ContinueKeyword` exists from SDK 16 and is not in `IsKeyword()`.
- The receiver in `Xmlport.Run` binds to `BoundClassType` whose symbol is the `LanguageClassTypeSymbol` (`Xmlport`, `Codeunit`, `Page`, `Report`, `Query`, `Database`, `System`, ...).
- `Rec`/`xRec` are synthesized variable symbols with a null `DeclaringSyntaxReference` and literal names.
- `QuoteIdentifierIfNeeded` quotes only object keywords outside the keyword-allowed-identifier list, so `Continue`, `continue`, `Rec`, `Xmlport` and `Codeunit` come back unquoted and the shared fix needs no special case.

## Test notes

- The `PageCustomization` fixtures (keyword and identifier suites) declare the customized page in the same file, which the compiler only accepts from 13.0; both suites skip them below that version.
- The `ContinueStatement` and `ContinueVariableAndStatementInDifferentScopes` cases are gated at 15.0 because a bare `continue;` only compiles from runtime Spring2025.

## CodeFix: CasingMismatchCodeFix

| Decision | Rationale |
|---|---|
| One provider (`CodeFixes/CasingMismatchKeyword.cs`) registered for the whole FC0002 ID, fixing every diagnostic that carries `CanonicalText` in its properties | Keyword and identifier diagnostics share the same fix shape, so one provider covers both analyzers. |
| Replacement text is `CanonicalText.QuoteIdentifierIfNeededWithReflection()` | Re-quotes names that need quotes (`"MY TABLE"` becomes `"My Table"`) and drops unnecessary quotes (`"IMYINTERFACE"` becomes `IMyInterface`) instead of copying the original quoting. |
