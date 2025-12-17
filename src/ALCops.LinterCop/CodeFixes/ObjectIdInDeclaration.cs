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
        public string NamespaceName { get; }
        public string IdentifierName { get; }

        private CodeFixProperties(string namespaceName, string identifierName)
        {
            NamespaceName = namespaceName;
            IdentifierName = identifierName;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(IdentifierName), out var identifierName) || string.IsNullOrEmpty(identifierName))
                return null;

            properties.TryGetValue(nameof(NamespaceName), out var namespaceName);

            return new CodeFixProperties(namespaceName ?? string.Empty, identifierName);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(string NamespaceName, string IdentifierName)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(IdentifierName), out var identifierName) || string.IsNullOrEmpty(identifierName))
                return null;

            properties.TryGetValue(nameof(NamespaceName), out var namespaceName);

            return new CodeFixProperties(namespaceName ?? string.Empty, identifierName);
        }
    }
#endif

    private class ObjectIdInDeclarationCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;

        public ObjectIdInDeclarationCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            this.SetPropertyIfExists("SupportsFixAll", generateFixAll);
            this.SetPropertyIfExists("FixAllSingleInstanceTitle", string.Empty);
            this.SetPropertyIfExists("FixAllTitle", Title);
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

        ctx.RegisterCodeFix(CreateCodeAction(node, document, properties, true), ctx.Diagnostics[0]);
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
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (node is not ObjectNameOrIdSyntax objectNameOrIdSyntax)
            return document;

        var newObjectNameOrIdSyntax = SyntaxFactory.ObjectNameOrId(CreateIdentifierName(properties));
        var newRoot = syntaxRoot.ReplaceNode(objectNameOrIdSyntax, newObjectNameOrIdSyntax);
        return document.WithSyntaxRoot(newRoot);
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