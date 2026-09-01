# CodeFixProvider template

Used by `/new-codefix` when creating `src/ALCops.{Cop}/CodeFixes/{RuleName}CodeFixProvider.cs`. Patterns for FixAll, `CodeFixProperties`, `SyntaxFactory` and AL name comparison are in `.claude/rules/codefix-development.md`.

## Required using directives

```csharp
using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
```

As needed:

```csharp
using ALCops.Common.Reflection;  // EnumProvider, PropertyAccessor, etc.
using System.Reflection;          // when using reflection-based helpers
```

## Standard structure

Canonical template derived from actual implementations:

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

Key points:

- The class is always `sealed`.
- `GetSyntaxRootAsync` is called early and awaited later (the `Task<SyntaxNode> syntaxRootTask` pattern), so the syntax tree loads in parallel with node analysis.
- The inner `CodeAction` class inherits `CodeAction.DocumentChangeAction`, with `Kind` always `CodeActionKind.QuickFix`.
- The fix title comes from the `.resx` (e.g. `PlatformCopAnalyzers.EditableFlowFieldCodeAction`).
- `RegisterInstanceCodeFix` is a static helper that finds the node and calls `RegisterCodeFix`.
