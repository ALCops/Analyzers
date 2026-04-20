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

When the CodeFix needs information computed by the analyzer (e.g. a replacement name), the analyzer passes it through `ImmutableDictionary<string, string>` properties on the diagnostic:

**In the analyzer:**
```csharp
var properties = ImmutableDictionary<string, string>.Empty
    .Add("ReplacementMethodName", replacementMethod.Expression.ToString());

ctx.ReportDiagnostic(Diagnostic.Create(
    DiagnosticDescriptors.MyRule,
    location,
    properties,   // ← passed here
    messageArg1, messageArg2));
```

**In the CodeFix:**
```csharp
var diagnostic = ctx.Diagnostics[0];
var properties = CodeFixProperties.TryParse(diagnostic.Properties);
if (properties is null) return;
```

This requires a `CodeFixProperties` helper class with multi-target support:

```csharp
#if NETSTANDARD2_1
// C# 9 records require IsExternalInit which doesn't exist in netstandard2.1
private sealed class CodeFixProperties
{
    public string ReplacementMethodName { get; }

    private CodeFixProperties(string replacementMethodName)
    {
        ReplacementMethodName = replacementMethodName;
    }

    public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
    {
        if (properties is null)
            return null;

        if (!properties.TryGetValue(nameof(ReplacementMethodName), out var value)
            || string.IsNullOrEmpty(value))
            return null;

        return new CodeFixProperties(value);
    }
}
#endif

#if NET8_0_OR_GREATER
private sealed record CodeFixProperties(string ReplacementMethodName)
{
    public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
    {
        if (properties is null)
            return null;

        if (!properties.TryGetValue(nameof(ReplacementMethodName), out var value)
            || string.IsNullOrEmpty(value))
            return null;

        return new CodeFixProperties(value);
    }
}
#endif
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

- **Test framework**: NUnit 4.x
- **Test kit**: `ALCops.RoslynTestKit` (custom package, version 0.4.1)
- **Base class**: `NavCodeAnalysisBase` (from RoslynTestKit)
- **Fixture factory**: `RoslynFixtureFactory.Create<T>()` for both analyzers and code fixes
- **Test projects** always target `net8.0` only

Test directory structure for a rule with a CodeFix:

```
Rules/
└── MyRule/
    ├── MyRule.cs               # Test class
    ├── HasDiagnostic/          # .al files where diagnostic IS expected
    │   ├── TestCase1.al
    │   └── TestCase2.al
    ├── NoDiagnostic/           # .al files where diagnostic is NOT expected
    │   ├── ValidCase1.al
    │   └── ValidCase2.al
    └── HasFix/                 # CodeFix test cases (current → expected)
        └── TestCaseName/
            ├── current.al      # Input code (with [|...|] marker)
            └── expected.al     # Expected output after fix
```

Three test method patterns:
- `HasDiagnostic(string testCase)` - verifies diagnostic fires at markers
- `NoDiagnostic(string testCase)` - verifies no false positives
- `HasFix(string testCase)` - verifies the CodeFix produces the expected output

## Existing implementations reference

| Cop | File | Diagnostic | Fix type |
|---|---|---|---|
| PlatformCop | `EditableFlowField.cs` | PC0001 | Add/update `Editable = false` property |
| PlatformCop | `GuidEmptyStringComparison.cs` | PC0015 | Replace `guid == ''` with `System.IsNullGuid(guid)` |
| PlatformCop | `EventSubscriberVarKeyword.cs` | PC0010 | Add `var` keyword to parameter |
| PlatformCop | `EventPublisherIsHandledByVar.cs` | PC0009 | Add `var` keyword to IsHandled parameter |
| PlatformCop | `ExtensiblePropertyExplicitlySet.cs` | PC0012 | Set Extensible property |
| PlatformCop | `JsonTokenJPathUsesDoubleQuotes.cs` | PC0016 | Replace double quotes with single quotes in JPath |
| PlatformCop | `SetRangeWithFilterOperators.cs` | PC0003 | Replace SetRange with SetFilter |
| PlatformCop | `FilterStringSingleQuoteEscaping.cs` | PC0019 | Fix quote escaping in filter strings |
| PlatformCop | `OperatorAndPlaceholderInFilterExpression.cs` | PC0017 | Fix filter expression operators |
| PlatformCop | `MandatoryFieldMissingOnApiPage.cs` | PC0022 | Add mandatory field to API page |
| PlatformCop | `ApplicationAreaOnApiPage.cs` | PC0013 | Remove ApplicationArea from API page |
| PlatformCop | `PossibleOverflowAssigningAppendMaxLengthToLabel.cs` | PC0024 | Apply MaxLength/CopyStr to prevent overflow |
| PlatformCop | `PossibleOverflowAssigningApplyCopyStr.cs` | PC0024 | Apply CopyStr to prevent overflow |
| ApplicationCop | `EmptyCaptionLocked.cs` | AC0033 | Add `Locked = true` to empty caption |
| ApplicationCop | `LabelWithTokSuffixMustBeLocked.cs` | AC0017 | Add `Locked = true` to Tok-suffixed label |
| ApplicationCop | `InstallAndUpgradeCodeunitsShouldBeInternal.cs` | AC0011 | Add `Access = Internal` |
| ApplicationCop | `PublicEventPublisher.cs` | AC0040 | Change event publisher access |
| ApplicationCop | `NotBlankRequiredOnPrimaryKeyField.cs` | AC0043 | Add NotBlank to PK field |
| ApplicationCop | `NotBlankNotAllowedOnPrimaryKeyField.cs` | AC0044 | Remove NotBlank from PK field |
| ApplicationCop | `PermissionSetCaptionLength.cs` | AC0012 | Truncate PermissionSet caption |
| ApplicationCop | `GlobalLanguageImplementTranslationHelper.cs` | AC0022 | Replace GlobalLanguage with TranslationHelper |
| ApplicationCop | `RunPageImplementPageManagement.cs` | AC0024 | Replace RunPage with PageManagement |
| ApplicationCop | `IntegrationEventsInInternalCodeunit*.cs` | AC0029 | Two fixes: remove internal access or convert event type |
| FormattingCop | `UseParenthesisForFunctionCall.cs` | FC0009 | Add `()` to function calls |
| FormattingCop | `CasingMismatchKeyword.cs` | FC0004 | Fix keyword casing via text replacement |
| LinterCop | `AllowInCustomizationsRedundancy.cs` | LC0024 | Remove redundant AllowInCustomizations property |
| LinterCop | `DataClassificationRedundancy.cs` | LC0025 | Remove redundant DataClassification property |
| LinterCop | `ApplicationAreaRedundancy.cs` | LC0026 | Remove redundant ApplicationArea property |
| LinterCop | `RecordInstanceIsolationLevel.cs` | LC0005 | Replace `LockTable()` with `ReadIsolation()` |
| LinterCop | `BuiltInDateTimeMethod.cs` | LC0022 | Replace deprecated DateTime method |
| LinterCop | `ObjectIdInDeclaration.cs` | LC0030 | Replace numeric object ID with name reference |
