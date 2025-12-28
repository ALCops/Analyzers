using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(IntegrationEventInInternalCodeunitRemoveAccessInternalFixProvider))]
public sealed class IntegrationEventInInternalCodeunitRemoveAccessInternalFixProvider : CodeFixProvider
{
    private const string AccessPropertyName = "Access";
    private const string EnumPropertyIdentifierName = "Internal";

    private class IntegrationEventInInternalCodeunitAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public IntegrationEventInInternalCodeunitAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.IntegrationEventInInternalCodeunit.Id);

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
        ctx.RegisterCodeFix(CreateCodeAction(node, document, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static IntegrationEventInInternalCodeunitAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new IntegrationEventInInternalCodeunitAction(
            ApplicationCopAnalyzers.IntegrationEventInInternalCodeunitRemoveAccessPropertyCodeAction,
            ct => RemoveAccessProperyFromCodeunit(document, node, ct),
            nameof(IntegrationEventInInternalCodeunitRemoveAccessInternalFixProvider),
            generateFixAll);
    }

    private static async Task<Document> RemoveAccessProperyFromCodeunit(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        CodeunitSyntax? codeunit = node.FirstAncestorOrSelf<CodeunitSyntax>();
        if (codeunit is null)
            return document;

        PropertyListSyntax? propertyList = codeunit.PropertyList;
        if (propertyList is null)
            return document;

        PropertySyntax? accessProperty = FindAccessInternalProperty(propertyList);
        if (accessProperty is null)
            return document;

        var newProperties = propertyList.Properties.Remove(accessProperty);
        var newPropertyList = propertyList.WithProperties(newProperties);

        var newCodeunit = codeunit.WithPropertyList(newPropertyList);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(codeunit, newCodeunit);
        return document.WithSyntaxRoot(newRoot);
    }

    private static PropertySyntax? FindAccessInternalProperty(PropertyListSyntax propertyList)
    {
        foreach (SyntaxNode p in propertyList.Properties)
        {
            if (p is not PropertySyntax prop)
                continue;

            if (!IsPropertyName(prop, AccessPropertyName))
                continue;

            if (!IsEnumPropertyValueIdentifier(prop, EnumPropertyIdentifierName))
                continue;

            return prop;
        }

        return null;
    }

    private static bool IsPropertyName(PropertySyntax prop, string expectedName)
    {
        if (prop.Name is not PropertyNameSyntax propName)
            return false;

        return string.Equals(
            propName.Identifier.ValueText,
            expectedName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEnumPropertyValueIdentifier(PropertySyntax prop, string expectedIdentifier)
    {
        if (prop.Value is not EnumPropertyValueSyntax propValue)
            return false;

        return string.Equals(
             propValue.Value.Identifier.ValueText,
             expectedIdentifier,
             StringComparison.OrdinalIgnoreCase);
    }
}