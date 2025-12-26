using System.Collections.Immutable;
using System.Reflection;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(ExtensiblePropertyExplicitlySetCodeFix))]
public sealed class ExtensiblePropertyExplicitlySetCodeFix : CodeFixProvider
{
    private class ExtensiblePropertyExplicitlySetCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public ExtensiblePropertyExplicitlySetCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;

        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.ExtensiblePropertyExplicitlySet.Id);

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
        ctx.RegisterCodeFix(CreateCodeAction(node, document, true), ctx.Diagnostics[0]);
    }

    private static ExtensiblePropertyExplicitlySetCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new ExtensiblePropertyExplicitlySetCodeAction(
            PlatformCopAnalyzers.ExtensiblePropertyExplicitlySetCodeAction,
            ct => SetExtensiblePropertyToPlatformDefault(document, node, ct),
            nameof(ExtensiblePropertyExplicitlySetCodeFix),
            generateFixAll);
    }

    private static async Task<Document> SetExtensiblePropertyToPlatformDefault(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var objectNode = node switch
        {
            TableSyntax or PageSyntax or ReportSyntax => node,
            _ => node.Parent
        };

        if (objectNode is not (TableSyntax or PageSyntax or ReportSyntax))
            return document;

        var propertyList = GetPropertyList(objectNode);
        if (propertyList is null)
            return document;

        var existingExtensible = FindProperty(propertyList, EnumProvider.PropertyKind.Extensible);

        var newPropertyList = existingExtensible is null
            ? propertyList.WithProperties(propertyList.Properties.Add(CreateExtensibleProperty()))
            : ReplacePropertyValue(propertyList, existingExtensible);

        var newObjectNode = WithPropertyList(objectNode, newPropertyList);
        if (newObjectNode is null)
            return document;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        return document.WithSyntaxRoot(root.ReplaceNode(objectNode, newObjectNode));
    }

    private static PropertyListSyntax? GetPropertyList(SyntaxNode objectNode) =>
        objectNode switch
        {
            TableSyntax t => t.PropertyList,
            PageSyntax p => p.PropertyList,
            ReportSyntax r => r.PropertyList,
            EnumTypeSyntax e => e.PropertyList,
            _ => null
        };

    private static SyntaxNode? WithPropertyList(SyntaxNode objectNode, PropertyListSyntax propertyList) =>
        objectNode switch
        {
            TableSyntax t => t.WithPropertyList(propertyList),
            PageSyntax p => p.WithPropertyList(propertyList),
            ReportSyntax r => r.WithPropertyList(propertyList),
            EnumTypeSyntax e => e.WithPropertyList(propertyList),
            _ => null
        };

    private static PropertySyntax? FindProperty(PropertyListSyntax propertyList, PropertyKind kind)
    {
        // Prefer enum-based match when possible; fall back to string if your API uses names.
        foreach (var prop in propertyList.Properties)
        {
            if (prop is not PropertySyntax p)
                continue;

            // If Name is tokenized differently in your syntax model, adapt this comparison.
            if (string.Equals(p.Name?.ToString(), kind.ToString(), StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return null;
    }

    private static PropertySyntax CreateExtensibleProperty() =>
        SyntaxFactory.Property(
            EnumProvider.PropertyKind.Extensible,
            CreateBooleanPropertyValueTrue());

    private static PropertyListSyntax ReplacePropertyValue(
        PropertyListSyntax propertyList,
        PropertySyntax extensibleProperty)
    {
        var updated = extensibleProperty.WithValue(CreateBooleanPropertyValueTrue());
        var newProperties = propertyList.Properties.Replace(extensibleProperty, updated);
        return propertyList.WithProperties(newProperties);
    }

    private static PropertyValueSyntax CreateBooleanPropertyValueTrue() =>
        SyntaxFactory.BooleanPropertyValue(
            SyntaxFactory.BooleanLiteralValue(
                SyntaxFactory.Token(EnumProvider.SyntaxKind.TrueKeyword)));
}