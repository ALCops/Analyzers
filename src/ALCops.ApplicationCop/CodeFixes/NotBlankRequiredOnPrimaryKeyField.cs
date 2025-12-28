using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(NotBlankRequiredOnPrimaryKeyFieldCodeFixProvider))]
public sealed class NotBlankRequiredOnPrimaryKeyFieldCodeFixProvider : CodeFixProvider
{
    private class NotBlankRequiredOnPrimaryKeyFieldCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public NotBlankRequiredOnPrimaryKeyFieldCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.NotBlankRequiredOnPrimaryKeyField.Id);

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
        ctx.RegisterCodeFix(CreateCodeAction(node, document, generateFixAll: false), ctx.Diagnostics[0]);
    }

    private static NotBlankRequiredOnPrimaryKeyFieldCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new NotBlankRequiredOnPrimaryKeyFieldCodeAction(
            ApplicationCopAnalyzers.NotBlankRequiredOnPrimaryKeyFieldCodeAction,
            ct => SetNonBlankPropertyForField(document, node, ct),
            nameof(NotBlankRequiredOnPrimaryKeyFieldCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> SetNonBlankPropertyForField(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (node.Parent is not FieldSyntax originalFieldNode)
            return document;

        FieldSyntax newFieldNode;
        var notBlankProperty = originalFieldNode.GetProperty(EnumProvider.PropertyKind.NotBlank.ToString());
        if (notBlankProperty is null)
        {
            newFieldNode = originalFieldNode.AddPropertyListProperties(GetNotBlankFalseProperty());
        }
        else
        {
            var newPropertyList = UpdateNotBlankPropertyList(originalFieldNode, notBlankProperty);
            newFieldNode = originalFieldNode.WithPropertyList(newPropertyList);
        }

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(originalFieldNode, newFieldNode);
        return document.WithSyntaxRoot(newRoot);
    }

    private static PropertySyntax GetNotBlankFalseProperty() =>
        SyntaxFactory.Property(EnumProvider.PropertyKind.NotBlank, GetBooleanFalsePropertyValue());

    private static PropertyListSyntax UpdateNotBlankPropertyList(FieldSyntax fieldNode, PropertySyntax notBlankProperty)
    {
        var updatednotBlankProperty = notBlankProperty.WithValue(GetBooleanFalsePropertyValue());

        var propertyList = fieldNode.PropertyList;
        var newProperties = propertyList.Properties.Select(prop =>
            prop == notBlankProperty ? updatednotBlankProperty : prop).ToList();

        return propertyList.WithProperties(SyntaxFactory.List(newProperties));
    }

    private static BooleanPropertyValueSyntax GetBooleanFalsePropertyValue() =>
        SyntaxFactory.BooleanPropertyValue(SyntaxFactory.BooleanLiteralValue(SyntaxFactory.Token(EnumProvider.SyntaxKind.TrueKeyword)));
}