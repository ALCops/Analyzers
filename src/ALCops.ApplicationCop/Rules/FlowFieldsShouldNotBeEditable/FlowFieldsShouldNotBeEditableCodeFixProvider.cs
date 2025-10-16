using System.Collections.Immutable;
using System.Reflection;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.ApplicationCop.CodeFixer;

[CodeFixProvider(nameof(FlowFieldsShouldNotBeEditableCodeFixProvider))]
public sealed class FlowFieldsShouldNotBeEditableCodeFixProvider : CodeFixProvider
{
    private class FlowFieldsShouldNotBeEditableCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;

        public FlowFieldsShouldNotBeEditableCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            // Use reflection to safely set properties that may not exist in all versions of the API
            SetPropertyIfExists("SupportsFixAll", generateFixAll);
            SetPropertyIfExists("FixAllSingleInstanceTitle", string.Empty);
            SetPropertyIfExists("FixAllTitle", Title);
        }

        private void SetPropertyIfExists(string propertyName, object value)
        {
            try
            {
                var propertyInfo = GetType().BaseType?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(this, value);
                }
            }
            catch (Exception)
            {
                // Silently ignore if property doesn't exist or can't be set
                // This maintains compatibility across different API versions
            }
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.FlowFieldsShouldNotBeEditable.Id);

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

    private static FlowFieldsShouldNotBeEditableCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new FlowFieldsShouldNotBeEditableCodeAction(
            "LinterCopAnalyzers.Fix0001FlowFieldsShouldNotBeEditableCodeAction",
            ct => SetEditablePropertyForField(document, node, ct),
            nameof(FlowFieldsShouldNotBeEditableCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> SetEditablePropertyForField(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node.Parent is not FieldSyntax originalFieldNode)
            return document;

        FieldSyntax newFieldNode;
        var editableProperty = originalFieldNode.GetProperty(EnumProvider.PropertyKind.Editable.ToString());
        if (editableProperty is null)
        {
            newFieldNode = originalFieldNode.AddPropertyListProperties(GetEditableFalseProperty());
        }
        else
        {
            var newPropertyList = UpdateEditablePropertyList(originalFieldNode, editableProperty);
            newFieldNode = originalFieldNode.WithPropertyList(newPropertyList);
        }

        var newRoot = (await document.GetSyntaxRootAsync(cancellationToken)).ReplaceNode(originalFieldNode, newFieldNode);
        return document.WithSyntaxRoot(newRoot);
    }

    private static PropertySyntax GetEditableFalseProperty() =>
        SyntaxFactory.Property(EnumProvider.PropertyKind.Editable, GetBooleanFalsePropertyValue());

    private static PropertyListSyntax UpdateEditablePropertyList(FieldSyntax fieldNode, PropertySyntax editableProperty)
    {
        var updatedEditableProperty = editableProperty.WithValue(GetBooleanFalsePropertyValue());

        var propertyList = fieldNode.PropertyList;
        var newProperties = propertyList.Properties.Select(prop =>
            prop == editableProperty ? updatedEditableProperty : prop).ToList();

        return propertyList.WithProperties(SyntaxFactory.List(newProperties));
    }

    private static PropertyValueSyntax GetBooleanFalsePropertyValue() =>
        SyntaxFactory.BooleanPropertyValue(SyntaxFactory.BooleanLiteralValue(SyntaxFactory.Token(EnumProvider.SyntaxKind.FalseKeyword)));
}