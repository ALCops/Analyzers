---
paths:
  - "src/ALCops.*/Analyzers/**"
  - "src/ALCops.Common/Extensions/OperationExtensions.cs"
  - "src/ALCops.Common/Permissions/**"
---

# Receiver forms

Any rule that looks at a record field (`Rec."No."`), a record method (`Modify`, `Get`, `SetRange`) or a user procedure on an object must handle every way AL reaches the receiver. The forms bind differently, and handling only some of them is the most common source of false negatives in this repo. Everything here was read in the decompiled SDK (`../nav-sdk-source`, `Microsoft.Dynamics.Nav.CodeAnalysis/net10.0`, identical in `net8.0`); type names are given so the facts can be re-checked when a newer SDK lands.

## Four forms, two operation shapes

| Form | Example | Syntax | Bound operation | Resolve the table via |
|---|---|---|---|---|
| Named variable | `Customer.Modify()`, `Customer."No."` | `MemberAccessExpressionSyntax` with an `IdentifierNameSyntax` receiver | `Instance` is a variable or parameter reference | variable map, or `Instance.GetSymbolSafe()` then `IVariableSymbol.Type` |
| Implicit `Rec` | `Rec.Modify()`, `Rec."No."` | same (`Rec` is an ordinary identifier) | `Instance` references the synthesized `Rec`: a global on most objects, a local in a `TableNo` codeunit (next section) | same as named variable |
| Bare self | `Modify()`, `"No."` | `InvocationExpressionSyntax` or `IdentifierNameSyntax` without a receiver | inside tables and tableextensions `Instance` is **null**; elsewhere the binder inserts the `Rec` receiver | the containing object for a null instance; otherwise `Instance.Type` |
| `this` (runtime 14.0+) | `this.Modify()`, `this."No."` | `MemberAccessExpressionSyntax` whose receiver is not an `IdentifierNameSyntax` | `Instance.Kind == OperationKind.ThisReference`; `Instance.Type` is the record type named after the table | `Instance.Type`, or `SemanticModel.GetOperation(receiver)?.Type` |

The forms apply identically to `IInvocationExpression.Instance` (built-in record methods and user procedures: `MyProc()`, `this.MyProc()`, `Rec.MyProc()`) and to `IFieldAccess.Instance`. `GetReceiverTableType` in `ALCops.Common/Extensions/OperationExtensions.cs` is the canonical resolver for both, including the null-instance bare form. Use it instead of re-deriving the table.

The compiler treats a null receiver and the synthesized self global as the same thing (`OverloadResolution.IsSelf`), and Microsoft's `Rule248AddThis` (CodeCop) recognises bare self with a null `Instance` plus `!symbol.IsSynthesized`, excluding extension objects because `this` there rebinds to the target. A complete "is this the current record" predicate needs four shapes: a null `Instance`, an `IGlobalReferenceExpression` over a synthesized `Rec`, an `ILocalReferenceExpression` over a synthesized `Rec`, and `OperationKind.ThisReference`.

## Where `Rec` comes from

`Rec` is not one thing. Each object kind synthesizes it differently (`Symbols/*ObjectMembers.cs`, `SourceMethodOrTriggerSymbol.cs`), and the difference decides what `Instance` looks like:

| Object | `Rec` symbol | Bare field or method access binds as | `xRec` | `this` binds to |
|---|---|---|---|---|
| table, tableextension | synthesized **global** of the own or target record, flagged as the object's own instance | `Instance == null`, in triggers and procedures alike | yes | the record type (tableextension: the target's) |
| page, pageextension, requestpage, report, xmlport | synthesized **global** of the `SourceTable`; for report and xmlport it is the **request page's** `SourceTable` and exists only when that request page declares one | the binder rewrites it to an explicit `Rec` receiver, so `Instance` is an `IGlobalReferenceExpression` over the synthesized global; gated on `#pragma implicitwith` and the `NoImplicitWith` compiler feature (`Binder.AreImplicitWithEnabled`) | yes | the object, not a record |
| codeunit with `TableNo`, **only inside `trigger OnRun`** | synthesized **local variable** of the trigger (`SymbolKind.LocalVariable`, `IsSynthesized`), created by `SourceMethodOrTriggerSymbol` when the codeunit has a `TableNo` | `Instance` is an `ILocalReferenceExpression` over that local (`InMethodBinder`) | **no** | the codeunit; `this."Field"` does not compile |
| report and query dataitem triggers | none; the dataitem is the record variable | `Instance` is an `IReportDataItemAccess` or query dataitem access (`BinderFactory`, `IsolatedScopeBinder`) | no | the object |
| xmlport table elements, query, enum, interface, codeunit without `TableNo`, reportextension | none | fields are reached through the element or dataitem name | no | the object |

`TableNo` is a codeunit-only property (`ObjectParser.GetCodeunitProperties`); pages and request pages use `SourceTable`, report dataitems use `DataItemTable`. A rule that classifies `Rec` by `SymbolKind.GlobalVariable` alone misses the `OnRun` local; a rule that gates on a non-null `Instance` misses tables and tableextensions.

## Symbol shapes

- A table object's declared symbol is an `ITableTypeSymbol`, which is **not** an `IRecordTypeSymbol`. `Rec` and `this` are a separate `IRecordTypeSymbol` whose `OriginalDefinition` is the table. Accept both: `is ITableTypeSymbol` for the object and bare self, `is IRecordTypeSymbol` for variable, `Rec` and `this` receivers.
- `Rec` binds to a variable named `"Rec"`; `this` binds to the record type whose `Name` is the table name. Name-keyed maps and symbol equality therefore see **different keys for the same instance**; normalize before comparing. To tell the current record from `xRec` (same type), compare the synthesized variable's name to `"Rec"`; the compiler's `IsThis` and `HasImplicitWith` flags live on an internal symbol.
- In a tableextension `this`, `Rec` and bare self all bind to the **target** table's record. Containing-symbol fallbacks must unwrap `IApplicationObjectExtensionTypeSymbol.Target`.
- `GetSymbolInfo` on a `this` receiver returns no symbol before AL 14.2 (the bound node gained its symbol override there), while `GetOperation(receiver)?.Type` works on every version. A `GetSymbolInfo` fast path must fall back to the operation tree for non-identifier receivers.

## Table shape: the implicit primary key

A table declared without a `keys` section still has a primary key: the compiler synthesizes one over the lowest-numbered field with a valid key type (`TableTypeSymbol.GetPrimaryKey`, `SynthesizedKeySymbol`). That key is exposed **only** through `ITableTypeSymbol.PrimaryKey`; `ITableTypeSymbol.Keys` holds declared keys and is empty for such a table, also when the table comes from a referenced `.app` (the symbol-reference converter serializes declared key members only). Any key-membership check must read `PrimaryKey` and then `Keys`; the synthesized key is recognisable by `IsSynthesized` and a null `Location`. Microsoft's `Rule210SuboptimalIndex` and `Rule222SIFTIndexShouldNotBeUsedForPrimaryAndUniqueKey` read `Keys` only and therefore never see such tables.

## Namespaces

`Record MyPublisher.MyExtension.MyAppDomain.MyTable` and `Record MyTable` bind to the same memoized `RecordTypeSymbol` (`Binder.BindRecordType`, `TableTypeSymbol.GetRecord`), so `OriginalDefinition` is the same `ITableTypeSymbol` and symbol-based resolution is namespace-agnostic. A misspelled or ambiguous namespace yields an `ExtendedErrorTypeSymbol` that still reports `NavTypeKind.Record` but is not an `IRecordTypeSymbol`: keep the `as IRecordTypeSymbol` / `OriginalDefinition as ITableTypeSymbol` casts null-checked, and never let a `NavTypeKind` check alone stand in for them.

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

Maps from variable name to record type are a performance fast path (`analyzer-performance.md`) that must replicate the compiler: locals, parameters, named return values and the `OnRun` `Rec` local **shadow** object-scope variables of the same name.

1. Classify variables by symbol type (`IVariableSymbol.Type`, `NavTypeKind`), never by name.
2. Look up the entire local scope (all local collections, in any order) before any object-scope collection.
3. Beware exclusion versus absence: if a scope's map deliberately omits some variables (temporary records, `RecordRef`), a shadowed omitted local falls through to the global of the same name. Track omitted names too, or fall back to bound symbols.

Bound-symbol resolution (`IOperation`, `GetSymbolInfo`) is immune to all of this.

## Fixtures

Receiver-relevant rules need the fixture set in `testing.md` (named variable, `Rec`, bare, `this`, tableextension, `TableNo` `OnRun`, page family where the rule can apply, one implicit-primary-key table, one namespaced file). `this` fixtures are gated on runtime 14.0.
