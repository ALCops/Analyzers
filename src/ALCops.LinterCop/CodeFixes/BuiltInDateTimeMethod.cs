using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.LinterCop.CodeFixes;

[CodeFixProvider(nameof(BuiltInDateTimeMethodCodeFixProvider))]
public sealed class BuiltInDateTimeMethodCodeFixProvider : CodeFixProvider
{
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    private sealed class CodeFixProperties
    {
        public string ReplacementMethodName { get; }

        private CodeFixProperties(string _replacementMethodName)
        {
            ReplacementMethodName = _replacementMethodName;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(ReplacementMethodName), out var _replacementMethodName) || string.IsNullOrEmpty(_replacementMethodName))
                return null;

            return new CodeFixProperties(_replacementMethodName);
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

            if (!properties.TryGetValue(nameof(ReplacementMethodName), out var _replacementMethodName) || string.IsNullOrEmpty(_replacementMethodName))
                return null;

            return new CodeFixProperties(_replacementMethodName);
        }
    }
#endif

    private class BuiltInDateTimeMethodCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public BuiltInDateTimeMethodCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.BuiltInDateTimeMethod.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span);

        var diagnostic = ctx.Diagnostics
            .FirstOrDefault(d => d.Id == DiagnosticDescriptors.BuiltInDateTimeMethod.Id);

        var properties = CodeFixProperties.TryParse(diagnostic?.Properties);
        if (properties is null)
            return;

        ctx.RegisterCodeFix(CreateCodeAction(node, document, properties, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static BuiltInDateTimeMethodCodeAction CreateCodeAction(SyntaxNode node, Document document, CodeFixProperties properties, bool generateFixAll)
    {
        return new BuiltInDateTimeMethodCodeAction(
            LinterCopAnalyzers.BuiltInDateTimeMethodCodeAction,
            ct => ReplaceWithNewMethodAsync(document, node, properties, ct),
            nameof(ObjectIdInDeclarationCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ReplaceWithNewMethodAsync(Document document, SyntaxNode node, CodeFixProperties properties, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (node is not InvocationExpressionSyntax invocation)
            return document;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return document;

        var firstArgument = invocation.ArgumentList.Arguments[0];

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            firstArgument,
            properties.ReplacementMethodName);

        var newInvocation = SyntaxFactory.InvocationExpression(memberAccess)
            .WithTriviaFrom(invocation);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}