---
paths:
  - "src/ALCops.*/CodeFixes/**"
---

# CodeFix Development Guide

How to implement `CodeFixProvider` classes in ALCops Analyzers. A CodeFix offers an automatic quick-fix for a diagnostic reported by an analyzer.

## Finding reference implementations

Find reference implementations with `grep -rl CodeFixProvider src/*/CodeFixes/`.

## Base class and registration

Every CodeFix provider:

1. Inherits from `Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes.CodeFixProvider`
2. Is decorated with `[CodeFixProvider(name)]` from `Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef`
3. Is discovered automatically via MEF (`System.Composition.AttributedModel`). No explicit registration needed.

Required overrides:

- `FixableDiagnosticIds` (property, `ImmutableArray<string>`) - the diagnostic IDs this provider handles
- `GetFixAllProvider()` (method) - normally returns `WellKnownFixAllProviders.BatchFixer`. Use a custom `FixAllProvider.Create(FixAllAsync)` when multiple diagnostics in the same document may resolve to edits on a **shared ancestor node** (e.g. removing multiple entries from the same `ParameterListSyntax` / `PropertyListSyntax` / any `SeparatedSyntaxList`). See "Custom FixAllProvider" below.
- `RegisterCodeFixesAsync(CodeFixContext ctx)` (method) - entry point that registers fix actions

## Diagnostic ID linkage

A CodeFix is paired with its analyzer through the diagnostic ID string:

```
DiagnosticIds.cs          →  public static readonly string MyRule = "XX0001";
DiagnosticDescriptors.cs  →  public static readonly DiagnosticDescriptor MyRule = new(id: DiagnosticIds.MyRule, ...);
Analyzers/MyRule.cs       →  ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MyRule, location));
CodeFixes/MyRule.cs       →  FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.MyRule.Id);
```

The analyzer must already exist and report the diagnostic before a CodeFix can act on it.

## Naming conventions

- **Class name**: `{RuleName}CodeFix` or `{RuleName}CodeFixProvider` (both exist; the `CodeFixProvider` suffix is more common in newer code)
- **File name**: `{RuleName}.cs`, in `CodeFixes/`
- **Namespace**: `ALCops.{CopName}.CodeFixes`
- **Inner CodeAction class**: `{RuleName}CodeAction` (private, nested)
- **CodeFixProvider attribute**: `[CodeFixProvider(nameof(ClassName))]` or `[CodeFixProvider("ClassName")]`
- **Resx key for the fix title**: `{RuleName}CodeAction` (e.g. `EditableFlowFieldCodeAction`)

## Standard CodeFix structure

Canonical provider template (class layout, inner `CodeAction`, `RegisterCodeFixesAsync`, `ApplyFix`, required `using`s): `.claude/skills/new-codefix/references/codefix-template.md` (used by `/new-codefix`). When editing an existing provider, the provider itself is the template.

## Custom FixAllProvider

Default to `WellKnownFixAllProviders.BatchFixer`. It runs the single-diagnostic fix path once per diagnostic and merges the resulting document edits.

**Exception — use a custom `FixAllProvider` when multiple diagnostics may produce edits on a shared ancestor node.** BatchFixer merges the produced `Document`s using a diff strategy that assumes independent edits. When two or more diagnostics translate into `ReplaceNode(sameAncestor, …)` calls (e.g. removing multiple `ParameterSyntax` entries from the same `ParameterListSyntax`, or two properties from the same `PropertyListSyntax`), only one edit survives the merge and the rest are silently dropped. Symptom: FixAll actions that fix "1 of N" occurrences.

Trigger conditions:

- The fix removes or replaces a node inside a `SeparatedSyntaxList` (parameters, properties, arguments, variables in a `var` section, permission entries).
- Multiple diagnostics from the same rule can point at siblings in the same list within one document.

Pattern — one syntax rewrite per document, executed in `FixAllAsync`:

```csharp
public sealed override FixAllProvider GetFixAllProvider() =>
    FixAllProvider.Create(FixAllAsync);

private static async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document,
    Optional<ImmutableArray<TextSpan>> fixAllSpans)
{
    var cancellationToken = fixAllContext.CancellationToken;

    SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
        return document;

    // Resolve the spans to fix. See "Optional<ImmutableArray<TextSpan>> quirk" below.
    ImmutableArray<TextSpan> spans;
    if (fixAllSpans.HasValue && !fixAllSpans.Value.IsDefaultOrEmpty)
    {
        spans = fixAllSpans.Value;
    }
    else
    {
        var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
        spans = diagnostics.Select(d => d.Location.SourceSpan).ToImmutableArray();
    }

    if (spans.IsDefaultOrEmpty)
        return document;

    // Collect all target nodes in one pass, then rewrite the tree once.
    var nodesToRemove = new HashSet<SyntaxNode>();
    foreach (var span in spans)
    {
        var node = root.FindNode(span, getInnermostNodeForTie: true);
        // Optional: apply scope filter (e.g. based on fixAllContext.CodeActionEquivalenceKey).
        if (node is not null)
            nodesToRemove.Add(node);
    }

    if (nodesToRemove.Count == 0)
        return document;

    var newRoot = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
    return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
}
```

Key details:

- **Delegate signature.** `FixAllProvider.Create(...)` requires `Task<Document?>` (nullable). Returning `Task<Document>` compiles but binds to a different overload path and can misbehave.
- **`Optional<ImmutableArray<TextSpan>>` quirk.** In the AL SDK, `fixAllSpans.HasValue` can be `true` while `fixAllSpans.Value.IsDefaultOrEmpty` is also `true` (observed with RoslynTestKit's default document-scope FixAll). Always guard with `!IsDefaultOrEmpty` and fall back to `fixAllContext.GetDocumentDiagnosticsAsync(document)` so FixAll works both in VS Code and in tests.
- **One-pass rewrite for `SeparatedSyntaxList`.** Use `root.RemoveNodes(collection, SyntaxRemoveOptions.KeepNoTrivia)` (plural). It handles separator removal correctly across siblings in the same list, whereas per-diagnostic `ReplaceNode` calls conflict.
- **Trivia handling.** For node removals, prefer `SyntaxRemoveOptions.KeepNoTrivia` so multi-line signatures do not leave dangling comments or blank continuation lines. This also removes directives attached to the node; if a directive can be paired outside the removed node, explicitly preserve, transfer, or remove its matching directive before the node rewrite. See `ParameterNotReferencedCodeFixProvider` for a parameter-list implementation.
- **Scope filter via `CodeActionEquivalenceKey`.** Register multiple `CodeAction`s with distinct `EquivalenceKey`s if a rule needs "Fix all of kind X" variants. Read `fixAllContext.CodeActionEquivalenceKey` inside `FixAllAsync` to know which action the user invoked.
- **Single-fix path stays consistent.** Use `root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia)` (singular) for the individual quick-fix so single and Fix-All behave identically.

Reference implementation: `ParameterNotReferencedCodeFixProvider` in `ALCops.LinterCop`. See `.claude/rules/diagnostics/lc0095-lc0099-parameter-not-referenced.md` for its scope-split CodeActions.

## Common fix patterns

### Pattern 1: Property modification (add/update/remove)

**Add a property** (from `EditableFlowField.cs`):
```csharp
// Add Editable = false to a field that has no Editable property
newFieldNode = originalFieldNode.AddPropertyListProperties(
    SyntaxFactory.Property(EnumProvider.PropertyKind.Editable, GetBooleanFalsePropertyValue()));
```

**Update a property** (from `EditableFlowField.cs`):
```csharp
// Change existing Editable property value to false
var updatedProperty = editableProperty.WithValue(GetBooleanFalsePropertyValue());
var newProperties = propertyList.Properties.Select(prop =>
    prop == editableProperty ? updatedProperty : prop).ToList();
var newPropertyList = propertyList.WithProperties(SyntaxFactory.List(newProperties));
```

**Remove a property** (from `AllowInCustomizationsRedundancy.cs`):
```csharp
// Remove the AllowInCustomizations property entirely
var newProperties = originalPropertyList.Properties.Remove(allowInCustomizationsProperty);
var newPropertyList = originalPropertyList.WithProperties(newProperties);
```

**Insert at specific position** (from `InstallAndUpgradeCodeunitsShouldBeInternal.cs`):
```csharp
// Insert Access = Internal at the beginning of property list
properties.Insert(0, accessProperty);
```

### Pattern 2: Expression/invocation replacement

**Replace method call** (from `RecordInstanceIsolationLevel.cs`):
```csharp
// Replace Record.LockTable() with Record.ReadIsolation(IsolationLevel::UpdLock)
var memberAccess = SyntaxFactory.MemberAccessExpression(expression, "ReadIsolation");
var argument = SyntaxFactory.OptionAccessExpression(
    SyntaxFactory.IdentifierName("IsolationLevel"),
    SyntaxFactory.IdentifierName("UpdLock"));
var newInvocation = SyntaxFactory.InvocationExpression(memberAccess, argumentList)
    .WithTriviaFrom(invocation);
```

**Wrap in invocation** (from `UseParenthesisForFunctionCall.cs`):
```csharp
// Add parentheses: MyFunction → MyFunction()
var newInvocation = SyntaxFactory.InvocationExpression(identifierExpression)
    .WithTriviaFrom(node);
```

**Replace comparison** (from `GuidEmptyStringComparison.cs`):
```csharp
// Replace guid == '' with System.IsNullGuid(guid)
var invocation = SyntaxFactory.InvocationExpression(
    SyntaxFactory.MemberAccessExpression(
        SyntaxFactory.IdentifierName("System"),
        "IsNullGuid"),
    argumentList);
```

### Pattern 3: Text replacement

For simple textual changes (rare; only `CasingMismatchKeyword.cs`):
```csharp
var sourceText = await document.GetTextAsync(cancellationToken);
var newSourceText = sourceText.WithChanges(new TextChange(span, properties.CanonicalText));
return document.WithText(newSourceText);
```

### Pattern 4: Label property manipulation

Adding `Locked = true` to label values (from `EmptyCaptionLocked.cs`, `LabelWithTokSuffixMustBeLocked.cs`):
```csharp
// Find the LabelPropertyValueSyntax
// Check if it has an existing properties list
// Add or update the Locked = true entry
```

## Passing data from analyzer to CodeFix via diagnostic properties

When the CodeFix needs information computed by the analyzer (e.g. a replacement name), the analyzer passes it through `ImmutableDictionary<string, string>` properties on the diagnostic. **Always use the `CodeFixProperties` record pattern** below. Do not use raw dictionary lookups, `out` parameters, or magic strings.

### CodeFixProperties pattern (required)

Every CodeFix that receives diagnostic properties defines a **private `CodeFixProperties` type** with a static `TryParse` method. Use `nameof()` on the record/class properties as dictionary keys for compile-time safety between the `TryParse` reader and the analyzer writer.

The type must be dual-defined with `#if` guards because C# 9 records require `System.Runtime.CompilerServices.IsExternalInit`, which does not exist in `netstandard2.1`:

```csharp
#if NETSTANDARD2_1
    // C# 9 records require IsExternalInit which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    private sealed class CodeFixProperties
    {
        public string TableName { get; }
        public string PermissionChar { get; }

        private CodeFixProperties(string tableName, string permissionChar)
        {
            TableName = tableName;
            PermissionChar = permissionChar;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TableName), out var tableName) || string.IsNullOrEmpty(tableName))
                return null;

            if (!properties.TryGetValue(nameof(PermissionChar), out var permissionChar) || string.IsNullOrEmpty(permissionChar))
                return null;

            return new CodeFixProperties(tableName, permissionChar);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(string TableName, string PermissionChar)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TableName), out var tableName) || string.IsNullOrEmpty(tableName))
                return null;

            if (!properties.TryGetValue(nameof(PermissionChar), out var permissionChar) || string.IsNullOrEmpty(permissionChar))
                return null;

            return new CodeFixProperties(tableName, permissionChar);
        }
    }
#endif
```

### Rules for this pattern

1. **Both `#if` blocks must have identical `TryParse` logic.** The only difference is `sealed class` (netstandard2.1) vs `sealed record` (net8.0+).
2. **Use `nameof()` for all dictionary keys** in `TryParse`. This links the key string to the property name at compile time.
3. **The analyzer side uses the same key strings** (e.g. `"TableName"`, `"PermissionChar"`). Since the record is `private` to the CodeFix, the analyzer cannot use `nameof()` across the boundary. Keeping property names identical to the dictionary keys makes mismatches easy to spot in review.
4. **Return `null` from `TryParse` on any missing required property.** Early returns, not exceptions.
5. **Optional properties** use `TryGetValue` without a null-return guard, defaulting to `string.Empty` or a sensible fallback.
6. **Place the `CodeFixProperties` type at the top** of the CodeFix class, before the inner `CodeAction` class.

### Consuming the properties in RegisterInstanceCodeFix

```csharp
private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot,
    TextSpan span, Document document)
{
    var diagnostic = ctx.Diagnostics[0];
    var props = CodeFixProperties.TryParse(diagnostic.Properties);
    if (props is null)
        return;

    // Use props.TableName, props.PermissionChar, etc.
}
```

### Setting properties in the analyzer

```csharp
var properties = ImmutableDictionary<string, string>.Empty
    .Add("TableName", tableName)
    .Add("PermissionChar", permissionChar.ToString());

ctx.ReportDiagnostic(Diagnostic.Create(
    DiagnosticDescriptors.MyRule,
    location,
    properties,
    messageArg1, messageArg2));
```

Only use this pattern when the CodeFix needs computed data from the analyzer. Most CodeFixes reconstruct what they need directly from the syntax tree.

## SyntaxFactory reference

Common `SyntaxFactory` methods used in CodeFixes:

```csharp
// Properties
SyntaxFactory.Property(EnumProvider.PropertyKind.Editable, propertyValue)
SyntaxFactory.BooleanPropertyValue(SyntaxFactory.BooleanLiteralValue(token))
SyntaxFactory.EnumPropertyValue(value)

// Tokens
SyntaxFactory.Token(EnumProvider.SyntaxKind.FalseKeyword)
SyntaxFactory.Token(EnumProvider.SyntaxKind.TrueKeyword)

// Identifiers and names
SyntaxFactory.IdentifierName(name)
SyntaxFactory.Identifier(name)
SyntaxFactory.QualifiedName(left, right)

// Expressions
SyntaxFactory.InvocationExpression(expression)
SyntaxFactory.InvocationExpression(expression, argumentList)
SyntaxFactory.MemberAccessExpression(expression, memberName)
SyntaxFactory.OptionAccessExpression(enumName, memberName)

// Parameters
SyntaxFactory.Parameter(varKeyword, name, colonToken, type)

// Lists
SyntaxFactory.List(items)
```

Syntax tree navigation:

```csharp
node.Parent                                    // direct parent
node.FirstAncestorOrSelf<FieldSyntax>()        // walk up to specific type
syntaxRoot.FindNode(span)                      // find node at diagnostic span
syntaxRoot.FindNode(span, getInnermostNodeForTie: true)  // prefer innermost

// Modifying nodes
root.ReplaceNode(oldNode, newNode)             // swap one node
node.WithPropertyList(newPropertyList)         // replace property list
node.AddPropertyListProperties(property)       // add to property list
newNode.WithTriviaFrom(oldNode)                // preserve whitespace/comments
```

## Adding a new CodeFix

Use `/new-codefix`.

## Test infrastructure

See `.claude/rules/testing.md`.

## AL name comparisons in CodeFixes

When comparing AL identifiers (method names, property names, object names, variable names, namespaces) in CodeFix code, use the `SemanticFacts` API family. See `.claude/rules/analyzer-development.md` for the full reference table. Quick summary:

```csharp
// Direct equality
if (SemanticFacts.IsSameName(expression.GetNameStringValue(), "RunModal"))

// Collection of AL names
private static readonly HashSet<string> Methods = new(SemanticFacts.NameEqualityComparer) { "Get", "Set" };
```

Do NOT use `SemanticFacts` for property value text, file paths, or non-AL strings.
