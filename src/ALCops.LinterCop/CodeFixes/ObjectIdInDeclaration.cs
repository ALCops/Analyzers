using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.LinterCop.CodeFixes;

[CodeFixProvider(nameof(ObjectIdInDeclarationCodeFixProvider))]
public sealed class ObjectIdInDeclarationCodeFixProvider : CodeFixProvider
{
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    private sealed class CodeFixProperties
    {
        public string SymbolKind { get; }
        public string NamespaceName { get; }
        public string IdentifierName { get; }

        private CodeFixProperties(string symbolKind, string namespaceName, string identifierName)
        {
            SymbolKind = symbolKind;
            NamespaceName = namespaceName;
            IdentifierName = identifierName;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(IdentifierName), out var identifierName) || string.IsNullOrEmpty(identifierName))
                return null;

            properties.TryGetValue(nameof(SymbolKind), out var symbolKind);
            properties.TryGetValue(nameof(NamespaceName), out var namespaceName);

            return new CodeFixProperties(symbolKind ?? string.Empty, namespaceName ?? string.Empty, identifierName);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(string SymbolKind, string NamespaceName, string IdentifierName)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(IdentifierName), out var identifierName) || string.IsNullOrEmpty(identifierName))
                return null;

            properties.TryGetValue(nameof(SymbolKind), out var symbolKind);
            properties.TryGetValue(nameof(NamespaceName), out var namespaceName);

            return new CodeFixProperties(symbolKind ?? string.Empty, namespaceName ?? string.Empty, identifierName);
        }
    }
#endif

    private class ObjectIdInDeclarationCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public ObjectIdInDeclarationCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.ObjectIdInDeclaration.Id);

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
            .FirstOrDefault(d => d.Id == DiagnosticDescriptors.ObjectIdInDeclaration.Id);

        var properties = CodeFixProperties.TryParse(diagnostic?.Properties);
        if (properties is null)
            return;

        ctx.RegisterCodeFix(CreateCodeAction(node, document, properties, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static ObjectIdInDeclarationCodeAction CreateCodeAction(SyntaxNode node, Document document, CodeFixProperties properties, bool generateFixAll)
    {
        return new ObjectIdInDeclarationCodeAction(
            LinterCopAnalyzers.ObjectIdInDeclarationActionTitle,
            ct => ReplaceObjectIdWithObjectName(document, node, properties, ct),
            nameof(ObjectIdInDeclarationCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ReplaceObjectIdWithObjectName(Document document, SyntaxNode node, CodeFixProperties properties, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        SyntaxNode? newNode;
        switch (node)
        {
            case LiteralAttributeArgumentSyntax:
                {
                    newNode = SyntaxFactory.OptionAccessAttributeArgument(
                        CreateOptionAccessExpression(properties));
                    break;
                }

            case LiteralExpressionSyntax:
                {
                    newNode = CreateOptionAccessExpression(properties);
                    break;
                }

            case ObjectNameOrIdSyntax:
                {
                    newNode = SyntaxFactory.ObjectNameOrId(
                        CreateIdentifierName(properties));
                    break;
                }

            case ObjectReferencePropertyValueSyntax:
                {
                    newNode = SyntaxFactory.ObjectReferencePropertyValue(
                        SyntaxFactory.ObjectNameOrId(
                            CreateIdentifierName(properties)));
                    break;
                }

            default:
                return document;
        }

        if (newNode is null)
            return document;

        var root = await syntaxRootTask.ConfigureAwait(false);

        return document.WithSyntaxRoot(root.ReplaceNode(node, newNode));
    }

    private static OptionAccessExpressionSyntax CreateOptionAccessExpression(CodeFixProperties properties)
    {
        return SyntaxFactory.OptionAccessExpression(
            SyntaxFactory.IdentifierName(properties.SymbolKind),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.ColonColonToken),
            CreateIdentifierName(properties));
    }

    private static NameSyntax CreateIdentifierName(CodeFixProperties properties)
    {
        if (string.IsNullOrEmpty(properties.NamespaceName))
            return SyntaxFactory.IdentifierName(properties.IdentifierName);

        return SyntaxFactory.QualifiedName(
            SyntaxFactory.IdentifierName(properties.NamespaceName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(properties.IdentifierName));
    }
}