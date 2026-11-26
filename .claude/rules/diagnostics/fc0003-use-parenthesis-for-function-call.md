---
paths:
  - "src/ALCops.FormattingCop/**/UseParenthesisForFunctionCall*"
  - "src/ALCops.FormattingCop.Test/Rules/UseParenthesisForFunctionCall/**"
---

# FC0003: UseParenthesisForFunctionCall

## Purpose

Detects a call to a built-in method without arguments that is written without parentheses (`Customer.Reset;`, `x := Today;`) and recommends the explicit call form (`Customer.Reset();`). Without the parentheses a method call reads like a field or variable access. A CodeFix adds the parentheses. This is the ALCops counterpart of CodeCop AA0008.

Registers `RegisterOperationAction` on `OperationKind.InvocationExpression`; main type `UseParenthesisForFunctionCall`.

## Design decisions

| Decision | Rationale |
|---|---|
| Built-in methods only (`MethodKind.BuiltInMethod`), zero arguments, last token of the invocation syntax is not `)` | The binder already resolved the call, so the operation tells a parameterless method call apart from a field or variable of the same name. The token test is what distinguishes `Reset` from `Reset()`. |
| Report on the whole invocation syntax (`Customer.Reset`, `Today`) | It is the node the fix replaces and the natural fixable span. |

## Deliberate non-reports

- An invocation whose parent is an `OptionAccessExpression` (`IsolationLevel::UpdLock`, `TextEncoding::Windows`): the option type name binds like a call but is not one.
- Calls with arguments, which always carry parentheses, and user-defined procedures.

## Known issues

- An editor "Fix All in Project" run was reported to corrupt code: `Customer.Reset()();`, plus `()` inserted one line below at the column where `Reset` ends (`Customer.SetCu()rrentKey`). Neither the Project-scope FixAll tests (workspace apply and host text-edit replay, including the re-parsed diff base) nor a manual run in VS Code reproduce it. An edit computed for one line that lands on the next line at the same column points at drift between the editor buffer and the server's workspace snapshot, not at the fix or the BatchFixer merge, which would shift offsets by multiples of the inserted `()`. The harness cannot model that drift.

## SDK facts

- At Project and Workspace scope the editor host's FixAll uses an eager diagnostic provider. It re-runs the analyzers that support the fixable ids over the whole project compilation, and the fix-all state carries only the fixer's own diagnostic ids. At Document scope it uses the diagnostics already computed for the document. (verified against SDK 30.0.44 in `../nav-sdk-source`, `CodeFixService.cs`)
- The host does not apply a FixAll result to its workspace. It diffs each changed document against the original solution snapshot with `Document.GetTextChangesAsync`, converts each change to a line/character range of the original text, and sends all edits of a file in one batch. (verified against SDK 30.0.44 in `../nav-sdk-source`, `RunCodeActionCommandHandler.cs`)
- CodeCop AA0008 (`Fix0008UseParenthesisForFunctionCallCodeFixProvider`) uses the identical mechanism: `SyntaxFactory.InvocationExpression` around the `MemberAccessExpression`, or around a new `IdentifierName` with the original trivia, merged by `WellKnownFixAllProviders.BatchFixer`. (verified against SDK 30.0.44 in `../nav-sdk-source`)

## Test notes

- The FixAll fixtures declare `table 50100 Customer` and `table 50101 Vendor` with a `"No."` field so that `SetCurrentKey("No.")` compiles; the harness has no Base App symbols.

## CodeFix: UseParenthesisForFunctionCallCodeFix

| Decision | Rationale |
|---|---|
| `IdentifierName` becomes `InvocationExpression(IdentifierName(value))` with the trivia of the original node; `MemberAccessExpression` becomes `InvocationExpression(memberAccess)`; any other node is left unchanged | Each case is one `ReplaceNode`, which keeps the single fix and the BatchFixer merge independent per diagnostic. |
| `WellKnownFixAllProviders.BatchFixer` | The edits of different diagnostics never share an ancestor node, so the default merge is enough. FixAll is pinned by the test class at Document scope (`HasFixAll`) and at Project scope across two documents (`HasFixAllInProject`, `HasFixAllInProjectOnReparsedText`). |
