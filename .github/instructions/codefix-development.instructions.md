---
applyTo: 'src/ALCops.*/CodeFixes/**'
---

# CodeFix Development Guide

This document covers how to implement `CodeFixProvider` classes in the ALCops Analyzers project. A CodeFix offers an automatic quick-fix for a diagnostic reported by an analyzer.

## Which cops have CodeFixes

| Project | Prefix | CodeFixes |
|---|---|---|
| `ALCops.PlatformCop` | `PC` | 13 implementations |
| `ALCops.ApplicationCop` | `AC` | 11 implementations |
| `ALCops.LinterCop` | `LC` | 6 implementations |
| `ALCops.FormattingCop` | `FC` | 2 implementations |
| `ALCops.DocumentationCop` | `DC` | None |
| `ALCops.TestAutomationCop` | `TA` | None |

## Base class and registration

Every CodeFix provider:

1. Inherits from `Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes.CodeFixProvider`
2. Is decorated with `[CodeFixProvider(name)]` from `Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef`
3. Is discovered automatically via MEF (`System.Composition.AttributedModel`). No explicit registration needed.

Required overrides:

- `FixableDiagnosticIds` (property, `ImmutableArray<string>`) - declares which diagnostic IDs this provider handles
- `GetFixAllProvider()` (method) - always returns `WellKnownFixAllProviders.BatchFixer`
- `RegisterCodeFixesAsync(CodeFixContext ctx)` (method) - entry point that registers fix actions

## Diagnostic ID linkage

A CodeFix is paired with its analyzer through the diagnostic ID string. The chain is:

```
DiagnosticIds.cs          →  public static readonly string MyRule = "XX0001";
DiagnosticDescriptors.cs  →  public static readonly DiagnosticDescriptor MyRule = new(id: DiagnosticIds.MyRule, ...);
Analyzers/MyRule.cs       →  ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MyRule, location));
CodeFixes/MyRule.cs       →  FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.MyRule.Id);
```

The analyzer must already exist and report the diagnostic before a CodeFix can act on it.

## Naming conventions

- **Class name**: `{RuleName}CodeFix` or `{RuleName}CodeFixProvider` (both patterns exist, but `CodeFixProvider` suffix is more common in newer code)
- **File name**: `{RuleName}.cs`, placed in `CodeFixes/` directory
- **Namespace**: `ALCops.{CopName}.CodeFixes`
- **Inner CodeAction class**: `{RuleName}CodeAction` (private, nested)
- **CodeFixProvider attribute**: `[CodeFixProvider(nameof(ClassName))]` or `[CodeFixProvider("ClassName")]`
- **Resx key for the fix title**: `{RuleName}CodeAction` (e.g. `EditableFlowFieldCodeAction`)

## Required using directives

Every CodeFix file needs these imports:

```csharp
using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
```

Additional imports as needed:

```csharp
using ALCops.Common.Reflection;  // EnumProvider, PropertyAccessor, etc.
using System.Reflection;          // when using reflection-based helpers
```

## Standard CodeFix structure

Every CodeFix in this project follows the same structural pattern. Here is the canonical template derived from actual implementations:

```csharp
using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.{CopName}.CodeFixes;

[CodeFixProvider(nameof(MyRuleCodeFixProvider))]
public sealed class MyRuleCodeFixProvider : CodeFixProvider
{
    // Inner CodeAction class (always present, always private)
    private class MyRuleCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public MyRuleCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.MyRule.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot,
        TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span);
        ctx.RegisterCodeFix(
            CreateCodeAction(node, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static MyRuleCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new MyRuleCodeAction(
            {CopName}Analyzers.MyRuleCodeAction,  // title from .resx
            ct => ApplyFix(document, node, ct),
            nameof(MyRuleCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(Document document, SyntaxNode node,
        CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        // 1. Navigate to the relevant parent node
        // 2. Build the replacement node
        // 3. Swap in the syntax tree

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(originalNode, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}
```

Key points about this structure:

- The class is always `sealed`.
- `GetSyntaxRootAsync` is called early and awaited later (the `Task<SyntaxNode> syntaxRootTask` pattern), so the syntax tree loads in parallel with node analysis.
- The inner `CodeAction` class inherits `CodeAction.DocumentChangeAction`, with `Kind` always set to `CodeActionKind.QuickFix`.
- The fix title comes from the `.resx` resource file (e.g. `PlatformCopAnalyzers.EditableFlowFieldCodeAction`).
- `RegisterInstanceCodeFix` is a static helper that finds the node and calls `RegisterCodeFix`.

## Common fix patterns

### Pattern 1: Property modification (add/update/remove)

Used when a fix needs to add, change, or remove a property on an AL object or field.

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

Used when replacing one method call or expression with another.

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

Used for simple textual changes (rare, only `CasingMismatchKeyword.cs`):
```csharp
var sourceText = await document.GetTextAsync(cancellationToken);
var newSourceText = sourceText.WithChanges(new TextChange(span, properties.CanonicalText));
return document.WithText(newSourceText);
```

### Pattern 4: Label property manipulation

Used when adding `Locked = true` to label values (from `EmptyCaptionLocked.cs`, `LabelWithTokSuffixMustBeLocked.cs`):
```csharp
// Find the LabelPropertyValueSyntax
// Check if it has an existing properties list
// Add or update the Locked = true entry
```

## Passing data from analyzer to CodeFix via diagnostic properties

When the CodeFix needs information computed by the analyzer (e.g. a replacement name), the analyzer passes it through `ImmutableDictionary<string, string>` properties on the diagnostic. **Always use the `CodeFixProperties` record pattern** described below. Do not use raw dictionary lookups, `out` parameters, or magic strings.

### CodeFixProperties pattern (required)

Every CodeFix that receives diagnostic properties must define a **private `CodeFixProperties` type** with a static `TryParse` method. Use `nameof()` on the record/class properties as dictionary keys to ensure compile-time safety between the `TryParse` reader and the analyzer writer.

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
3. **The analyzer side should use the same key strings** (e.g. `"TableName"`, `"PermissionChar"`). Since the record is `private` to the CodeFix, the analyzer cannot use `nameof()` across the boundary. Keeping property names identical to the dictionary keys makes mismatches easy to spot in review.
4. **Return `null` from `TryParse` on any missing required property.** Use early returns, not exceptions.
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

## Step-by-step: adding a new CodeFix

### 1. Verify the analyzer exists

The analyzer must already exist in `Analyzers/` with its diagnostic ID in `DiagnosticIds.cs` and `DiagnosticDescriptors.cs`. The CodeFix targets an existing diagnostic.

### 2. Add the CodeAction title to the .resx file

Add a new entry to `ALCops.{CopName}Analyzers.resx`:

```xml
<data name="MyRuleCodeAction" xml:space="preserve">
  <value>Fix: description of what the fix does</value>
</data>
```

This generates a property on the strongly-typed `{CopName}Analyzers` class that you reference as the CodeAction title.

### 3. Create the CodeFix class

Create `src/ALCops.{CopName}/CodeFixes/MyRule.cs` using the standard structure shown above. Key decisions:

- Does the fix need data from the analyzer? If yes, use the diagnostic properties pattern with `CodeFixProperties`.
- What syntax node type does the diagnostic target? Use `FindNode(span)` and navigate to the parent you need.
- What transformation? Add/remove/replace property, replace expression, or text replacement.

### 4. Add test cases

In the test project `src/ALCops.{CopName}.Test/Rules/MyRule/`:

Create the `HasFix/` subdirectory with test case folders. Each test case has two files:
- `current.al` - the AL code with the diagnostic (use `[|...|]` markers around the diagnostic span)
- `expected.al` - the AL code after the fix is applied (no markers)

The `[|...|]` marker in `current.al` identifies where the diagnostic is reported.

### 5. Add the HasFix test method

In the existing test class for the rule, add:

```csharp
using ALCops.{CopName}.CodeFixes;

// ... inside the test class:

[Test]
[TestCase("TestCaseName")]
public async Task HasFix(string testCase)
{
    var currentCode = await File.ReadAllTextAsync(
        Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
        .ConfigureAwait(false);

    var expectedCode = await File.ReadAllTextAsync(
        Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
        .ConfigureAwait(false);

    var fixture = RoslynFixtureFactory.Create<MyRuleCodeFixProvider>(
        new CodeFixTestFixtureConfig
        {
            AdditionalAnalyzers = [_analyzer]
        });

    fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.MyRule);
}
```

Key details:
- `RoslynFixtureFactory.Create<T>()` takes the CodeFix type as generic parameter (not the analyzer)
- `AdditionalAnalyzers` must include the analyzer instance so diagnostics are produced
- `fixture.TestCodeFix()` verifies the transformation from current to expected code

### 6. Build and test

```bash
dotnet build ALCops.sln
dotnet test ALCops.sln --filter "FullyQualifiedName~MyRule"
```

## Test infrastructure

See `testing.instructions.md` for the full testing guide. CodeFix-specific details:

- `RoslynFixtureFactory.Create<T>()` takes the CodeFix type as generic parameter (not the analyzer)
- `AdditionalAnalyzers` must include the analyzer instance so diagnostics are produced
- `fixture.TestCodeFix()` verifies the transformation from current to expected code
- Test cases live in `HasFix/<TestCaseName>/` with `current.al` (with `[|...|]` marker) and `expected.al`

## Reference implementations by pattern

When writing a new CodeFix, find an existing one that uses the same technique. Use `ls src/*/CodeFixes/` to discover all CodeFix files.

| Pattern | Good reference | What it demonstrates |
|---|---|---|
| Add/set a property | `EditableFlowField.cs` (PlatformCop) | Insert or update a property on a syntax node |
| Remove a property | `AllowInCustomizationsRedundancy.cs` (LinterCop) | Delete a property and clean up trivia |
| Add a keyword/token | `EventSubscriberVarKeyword.cs` (PlatformCop) | Insert a keyword into a parameter list |
| Replace an expression | `GuidEmptyStringComparison.cs` (PlatformCop) | Swap one expression for another |
| Replace a method call | `SetRangeWithFilterOperators.cs` (PlatformCop) | Rewrite a method invocation with different method/args |
| Rewrite a string literal | `JsonTokenJPathUsesDoubleQuotes.cs` (PlatformCop) | Text manipulation within a string token |
| Add syntax tokens | `UseParenthesisForFunctionCall.cs` (FormattingCop) | Insert tokens (parentheses) into existing syntax |
| Reorder/rebuild a property value | `PermissionDeclarationOrderCodeFixProvider.cs` (FormattingCop) | Sort entries, rebuild multi-line `PermissionPropertyValue` |
| Add entries to a list property | `TableDataAccessRequiresPermissions.cs` (ApplicationCop) | Insert into existing list, handle alphabetical insertion |
| Remove entries from a list property | `TableDataAccessUnusedPermissionsCodeFixProvider.cs` (ApplicationCop) | Remove specific entries, handle cleanup of entire property |
| Two alternative fixes for one diagnostic | `IntegrationEventsInInternalCodeunit*.cs` (ApplicationCop) | Two CodeFix classes sharing one diagnostic ID |
| Complex multi-node rewrite | `PossibleOverflowAssigningApplyCopyStr.cs` (PlatformCop) | Wrap expressions in function calls, handle multiple overload shapes |
