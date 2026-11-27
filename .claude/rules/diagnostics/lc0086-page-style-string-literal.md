---
paths:
  - "src/ALCops.LinterCop/**/PageStyleStringLiteral*"
  - "src/ALCops.LinterCop.Test/Rules/PageStyleStringLiteral/**"
---

# LC0086: PageStyleStringLiteral

## Purpose

Detects string literals that match `PageStyle` enum value names (e.g., `'Unfavorable'`, `'Standard'`, `'Attention'`) and suggests using the `PageStyle` datatype instead. String literals used for page styling lack IntelliSense, are prone to typos, and won't produce compile-time errors if misspelled.

Registers `SyntaxNodeAction` on `SyntaxKind.StringLiteralValue`; main type `PageStyleStringLiteral`.

**References:**
- [GitHub Issue #183](https://github.com/ALCops/Analyzers/issues/183) (false positive report)
- [GitHub Issue #522](https://github.com/ALCops/Analyzers/issues/522) (roadmap: flow analysis, CodeFix, cross-object tracing; deny contexts for arguments, comparisons, case labels and properties)
- [MS Docs: PageStyle Option](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/pagestyle/pagestyle-option)
- [BC.LinterCop LC0086 Wiki](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0086)
- [BC.LinterCop Discussion #805](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/805)

## Design decisions

| Decision | Rationale |
|---|---|
| Same ID as BC.LinterCop; Category Design, Severity Warning | Migration compatibility; an actionable architectural smell (string vs typed enum), not a correctness issue. |
| Deny-list approach: scan every string literal and suppress known-safe contexts, instead of flow analysis from `StyleExpr` | Tracing `StyleExpr = myVar` back to `myVar := 'Standard'` across triggers, methods and objects has PC0030-level complexity; BC.LinterCop does not do it either. Flow analysis and cross-object tracing were considered and declined. |
| One policy for every `PageStyle` value; no separate tier for ambiguous words (`None`, `Standard`, `Strong`), no variable-name or method-name heuristics | A developer must be able to predict from the syntax alone whether a literal is reported. A name heuristic or a per-value tier makes the same line flip with a rename. |
| Deny every method argument, comparison operand, case label and property value, not only the Record/RecordRef/FieldRef subset | In the Base App the matching literals in these positions are test expectations (`Assert.AreEqual('Strong', ...)`), HTTP header values (`'x-ms-file-attributes', 'None'`), telemetry text, filter values and field descriptions (`Description = 'Standard'`). A literal passed into or compared against something is data the caller does not style itself; a style value reaches `StyleExpr` through an assignment or an `exit`. |
| Case-sensitive (`StringComparer.Ordinal`) matching: only PascalCase spellings are flagged | `'STANDARD'` and `'standard'` are data constants, not style values ([#183](https://github.com/ALCops/Analyzers/issues/183)). |
| Local `Lazy<ImmutableDictionary>` built from `Enum.GetNames(typeof(StyleKind))` instead of `EnumProvider.StyleKind.CanonicalNames` | Every `CanonicalNames` dictionary in `EnumProvider` is `OrdinalIgnoreCase` by design and must not be used here. |
| Syntax-only deny checks run before the one semantic check (`IsWritingToTableField`) | The style-name lookup filters almost every literal; the ancestor and parent-kind checks are cheaper than `GetSymbolInfo`. |
| No CodeFix | Replacing `'Unfavorable'` with `Format(PageStyle::Unfavorable)` keeps the string variable; the real fix is retyping the variable to `PageStyle`, which touches its declaration and every reader. Declined. |
| Version gate `Fall2024OrGreater` | The `PageStyle` datatype did not exist before BC25. |

## Deliberate non-reports

- Every property value (`Caption`, `OptionCaption`, `Description`, `ToolTip`, a direct `StyleExpr = 'Standard'`, ...): user-facing text or data; for `StyleExpr` the fix is the property value type, not the literal.
- Arguments of any method call, built-in or user-defined (`SetRange(Field, 'None')`, `StrSubstNo('%1', 'Standard')`, `Assert.AreEqual('Strong', ...)`), and attribute arguments (`[EventSubscriber(..., 'Strong', ...)]`).
- Case labels, including ranges (`'None':`, `'A'..'Z':`). A literal in the case line's statement is still reported.
- Operands of `=`, `<>`, `<`, `<=`, `>`, `>=` and elements of an `in [...]` list, including parenthesized operands, ranges and the branches of a ternary (`MyText = (IsOverdue ? 'Unfavorable' : 'Standard')`).
- Literals inside Enum and EnumValue symbols: identifiers and captions, not style expressions.
- Unlocked labels and unlocked report labels: translatable text (the text of locked ones is flagged).
- Other literals of any label or report label, such as `Comment = 'Standard'`: notes for translators, not the label's value.
- Multilanguage text without a `Locked` option: `TextConst ENU = '...'` and the legacy `label(Name; ENU = '...')` report label.
- Assignments to table fields (`MyRecord.MyField := 'Standard'`, including bare and `this` receivers): data writes, not styling.
- Non-PascalCase spellings (`'STANDARD'`, `'standard'`) and obsolete symbols.

Still reported: assignment to a non-field variable (including array elements and the branches of a ternary on the right-hand side), `exit('...')`, and the text of locked labels and locked report labels.

## Known issues

- A locked label like `Label 'Standard', Locked = true` that genuinely stores data is still flagged. Per BC.LinterCop discussion #805 these are rare enough that `#pragma warning disable LC0086` is the accepted resolution.
- A style passed through a helper parameter (`SetStyle('Unfavorable')`) is no longer reported, because every argument is denied. Accepted: telling a style parameter from a data parameter needs the name heuristics or cross-method tracing this rule declines.

## SDK facts

- No `PropertyValueSyntax` subclass contains statements or blocks, so a `Property` ancestor never swallows code in a trigger body; label variable declarations sit under `VariableDeclaration`, not `Property`. `Caption = 'x', Locked = true` is a `LabelPropertyValueSyntax` under the `PropertySyntax`. Verified against SDK 30.0 in `../nav-sdk-source` (`Microsoft.Dynamics.Nav.CodeAnalysis.Syntax/*PropertyValueSyntax.cs`).
- `ReportLabelSyntax.Label` is a `LabelSyntax` of kind `Label` (`ObjectParser.ParseReportLabel`), so the label check covers report labels without a separate kind. `LabelSyntax.LabelText` (present at `ns2.0 12.0`) is the label's own text; `Comment`, `MaxLength` and `Locked` sit in `LabelSyntax.Properties`.
- `TextConstDataTypeSyntax.Multilanguage`, the legacy `ReportLabelMultilanguage` report label and multilanguage property values all parse through `ParseCommaSeparatedIdentifierEqualsStringList` (language code = text pairs, no `Locked` option), so one `CommaSeparatedIdentifierEqualsStringList` ancestor check covers TextConst and the legacy label. Both forms still compile in the test harness. Attribute arguments sit in an `AttributeArgumentList`, not an `ArgumentList`.
- Call arguments sit in an `ArgumentList`; `exit(...)` is an `ExitStatementSyntax` with its own parenthesis tokens and no `ArgumentList`, and `in [...]` uses a `BracketedArgumentList`.
- A code literal is wrapped in a `LiteralExpression`, and any parentheses around it in a `ParenthesizedExpression`; a case range is a `RangeExpression` (a `BinaryExpressionSyntax`). `CaseLineSyntax.Expressions` are its direct children, while its `Statement` wraps any literal in a statement node, so a `CaseLine` parent identifies a case value.
- `SyntaxKind.ConditionalExpression` (the ternary) first appears in the 14.0 SDK (`v14.0.16.22338-prerelease`; absent at the `ns2.0 12.0` floor), so `EnumProvider` resolves it to `SyntaxKind.None` on older SDKs and the check guards with `!= default`. `ConditionalExpressionSyntax` cannot be referenced on netstandard2.1; the condition is told apart from the branches by `SpanStart`, because the condition is the first child and starts where the ternary starts.

## Test notes

- Fixtures are gated with `RequireMinimumVersion("14.0")` (no `PageStyle` datatype below that).
- No compiling fixture puts a string literal directly in a ternary's condition: the condition must be Boolean, so a text literal or a style-producing ternary there is a compile error. The condition side is pinned indirectly: a literal compared inside the condition is denied as a comparison operand while the branches are still reported.
