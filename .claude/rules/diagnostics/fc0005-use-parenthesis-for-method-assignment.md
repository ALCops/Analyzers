---
paths:
  - "src/ALCops.FormattingCop/**/UseParenthesisForMethodAssignment*"
---

# FC0005: UseParenthesisForMethodAssignment

## Purpose

Detects a method that takes a single parameter being invoked using assignment
syntax (`target.Method := value;`) and recommends the explicit parenthesised
call (`target.Method(value);`). The assignment form hides that a method is being
called and is ambiguous; it would become more so if AL ever gains real object
properties. Provides a CodeFix that rewrites the assignment into a parenthesised
invocation.

Applies to both built-in methods (e.g. `Rec.ReadIsolation := ...`,
`currXMLport.TextEncoding := ...`) and user-defined procedures
(e.g. `MyCodeunit.SetValue := 5`).

## Design decisions

| Decision | Rationale |
|---|---|
| New rule (FC0005), not an extension of FC0003 | FC0003 covers the no-paren no-arg *call* form (`Rec.LockTable;`); the syntax shape and CodeFix differ. |
| Cover built-in methods and user procedures | Both compile via the same single-parameter assignment binding; the discussion (#235) requests both. |
| `MethodKind.Property` exclusion is **defensive** | Genuine properties (`SynthesizedPropertySymbol`) are getters with **0 parameters**, so they never match the single-parameter assignment binding and cannot reach this analyzer today. The guard prevents a false positive (and an invalid `prop(value)` fix, which would raise `ERR_PropertyUsedAsMethod`) if the SDK ever exposes a settable property. |
| `RegisterOperationAction(InvocationExpression)` | Consistent with FC0003. The operation is already built; the syntax-kind filter is a cheap early reject. |
| Report on the whole assignment statement | That is the `BoundCall`'s syntax; matches the natural fixable span. |

## Architecture

```
src/ALCops.FormattingCop/
├── Analyzers/
│   └── UseParenthesisForMethodAssignment.cs           # Analyzer (OperationAction)
└── CodeFixes/
    └── UseParenthesisForMethodAssignment.cs           # CodeFix (assignment -> invocation)
```

### Analysis flow

1. `RegisterOperationAction` on `OperationKind.InvocationExpression` (same pattern
   as the sibling rule FC0003 `UseParenthesisForFunctionCall`).
2. `ctx.IsObsolete()` guard.
3. Keep only invocations whose `Syntax.IsKind(AssignmentStatement)` — this is the
   assignment-as-method-call form. Normal invocations (`x.M(v)`) have
   `InvocationExpression` syntax and are rejected cheaply.
4. Exclude `MethodKind.Property` (see design decision above).
5. Report at `invocation.Syntax.GetLocation()` with `TargetMethod.Name`.

### CodeFix flow

`FindNode(span)` returns the `AssignmentStatementSyntax`. It is rewritten into an
`ExpressionStatementSyntax` containing
`InvocationExpression(Target, ArgumentList().AddArguments(Source))`. Target and
Source are taken `WithoutTrivia()`; the new statement re-applies the original
statement's leading trivia (indentation) and reuses the original `SemicolonToken`
(which carries the trailing newline).

## How it works (SDK basis)

`Binder.BindAssignmentStatement` (verified in `nav-sdk-source`): when an
assignment's target binds to a `BoundCall` whose method has `ParameterCount == 1`,
AL rebinds `target := source` as `target(source)` via
`BindInvocationExpression(..., asProperty: true, ...)`. The result is a
`BoundExpressionStatement` wrapping a `BoundCall`, which surfaces in the operation
tree as an **`IInvocationExpression` whose `Syntax` is an `AssignmentStatementSyntax`**.
The `BoundCall` carries the assignment statement as its syntax, so the diagnostic
location is the whole statement (including the trailing semicolon).

## Known issues / limitations

- Compound assignments (`+=`, `-=`, ...) do not trigger the single-parameter
  method rewrite in the binder and are out of scope.
- `TextEncoding` (xmlport `currXMLport`) and `ReadIsolation` (record) are built-in
  methods, not properties — FC0005 correctly flags their assignment form. (FC0003's
  NoDiagnostic fixtures use the assignment form of `TextEncoding` only because
  FC0003 targets a different shape.)
