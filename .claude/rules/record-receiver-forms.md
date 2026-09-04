---
paths:
  - "src/ALCops.*/Analyzers/**"
  - "src/ALCops.Common/Extensions/OperationExtensions.cs"
  - "src/ALCops.Common/Permissions/**"
---

# Record receiver forms

Any rule that looks at record method calls or field access (`Modify`, `Get`, `SetRange`, `Rec."No."`) must handle four ways AL reaches the record. Handling only some of them is the most common source of false positives and negatives in this repo.

| Form | Example | Syntax | Bound operation | Resolve the table via |
|---|---|---|---|---|
| Named variable | `Customer.Modify()` | `MemberAccessExpressionSyntax` with an `IdentifierNameSyntax` receiver | `IInvocationExpression.Instance` is a variable reference | variable map, or `Instance.GetSymbolSafe()` then `IVariableSymbol.Type` |
| Implicit `Rec` | `Rec.Modify()` | same (`Rec` is an ordinary identifier) | `Instance` is the synthesized global **variable** named `"Rec"` | same as named variable |
| Bare self | `Modify()` | `InvocationExpressionSyntax` whose `Expression` is an `IdentifierNameSyntax`; no receiver | `Instance` is **null** | the containing object: a table's declared symbol is an `ITableTypeSymbol` |
| `this` (runtime 14.0+) | `this.Modify()` | `MemberAccessExpressionSyntax` whose receiver is not an `IdentifierNameSyntax` | `Instance.Kind == OperationKind.ThisReference`; `Instance.Type` is the record **type** named after the table | `Instance.Type`, or `SemanticModel.GetOperation(receiver)?.Type` |

`GetReceiverTableType` in `ALCops.Common/Extensions/OperationExtensions.cs` is the canonical resolver for `IInvocationExpression.Instance` and `IFieldAccess.Instance`, including the null-instance bare form. Use it instead of re-deriving the table.

## Symbol shapes

- A table object's declared symbol is an `ITableTypeSymbol`, which is **not** an `IRecordTypeSymbol`. `Rec` and `this` are a separate `IRecordTypeSymbol` whose `OriginalDefinition` is the table. Accept both: `is ITableTypeSymbol` for the object and bare self, `is IRecordTypeSymbol` for variable and `this` receivers.
- `Rec` binds to a global variable named `"Rec"`; `this` binds to the record type whose `Name` is the table name. Name-keyed maps and symbol equality therefore see **different keys for the same instance**; normalize before comparing. To tell the current record from `xRec` (same type), compare the global's name to `"Rec"`; the compiler's `IsThis` and `HasImplicitWith` flags live on an internal symbol.
- On pages, reports and xmlports `this` binds to the object, not a record. The table above applies inside tables and tableextensions only.
- In a tableextension `this`, `Rec` and bare self all bind to the **target** table's record. Containing-symbol fallbacks must unwrap `IApplicationObjectExtensionTypeSymbol.Target`.
- `GetSymbolInfo` on a `this` receiver returns no symbol before AL 14.2 (the bound node gained its symbol override there), while `GetOperation(receiver)?.Type` works on every version. A `GetSymbolInfo` fast path must fall back to the operation tree for non-identifier receivers.

## Detecting `this` on every TFM

`ThisExpressionSyntax`, `SyntaxKind.ThisExpression` and `IInstanceReferenceOperation` do not exist at the netstandard2.1 compile floor (AL 12), so naming any of them forces an `#if !NETSTANDARD2_1` guard that silently drops `this` detection on the binary that serves AL 14.0 to 15.2. Never reference them. Instead:

```csharp
// syntax level: any receiver that is not a plain identifier
if (receiver is not null && receiver is not IdentifierNameSyntax)
    type = ctx.SemanticModel.GetOperation(receiver, ct)?.Type;

// operation level: the enum member resolves to default (None) on SDKs without it
var thisKind = EnumProvider.OperationKind.ThisReference;
if (thisKind != default && instance.Kind == thisKind) { /* self */ }
```

`EnumProvider` members that may be missing from older SDKs use the string form `ParseEnum<OperationKind>("ThisReference")`, because `nameof(OperationKind.ThisReference)` does not compile at the floor.

## Name-keyed variable maps must honour AL scoping

Maps from variable name to record type are a performance fast path (`analyzer-performance.md`) that must replicate the compiler: locals, parameters and named return values **shadow** object-scope variables of the same name.

1. Classify variables by symbol type (`IVariableSymbol.Type`, `NavTypeKind`), never by name.
2. Look up the entire local scope (all local collections, in any order) before any object-scope collection.
3. Beware exclusion versus absence: if a scope's map deliberately omits some variables (temporary records, `RecordRef`), a shadowed omitted local falls through to the global of the same name. Track omitted names too, or fall back to bound symbols.

Bound-symbol resolution (`IOperation`, `GetSymbolInfo`) is immune to all of this.

## Fixtures

Receiver-relevant rules need the four-form fixture set (named variable, `Rec`, bare, `this`, plus a tableextension variant where the rule can apply); `this` fixtures are gated on runtime 14.0. Details in `testing.md`.
