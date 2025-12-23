using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;

namespace ALCops.LinterCop.CodeFixes;

[CodeFixProvider("DataClassificationRedundancyCodeFixProvider")]
public sealed class DataClassificationRedundancyCodeFixProvider : CodeFixProvider
{
    private static readonly string DataClassificationName =
        EnumProvider.PropertyKind.DataClassification.ToString();

    private class DataClassificationRedundancyCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public DataClassificationRedundancyCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.DataClassificationRedundancy.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span, getInnermostNodeForTie: true);
        ctx.RegisterCodeFix((CodeAction)CreateCodeAction(node, document, true), ctx.Diagnostics[0]);
    }

    private static DataClassificationRedundancyCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new DataClassificationRedundancyCodeAction(
            LinterCopAnalyzers.DataClassificationRedundancyCodeAction,
            ct => RemoveDataClassificationForField(document, node, ct),
            nameof(DataClassificationRedundancyCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> RemoveDataClassificationForField(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node.Parent is not PropertyListSyntax originalPropertyList)
            return document;

        var dataClassificationProperty = originalPropertyList.GetProperty(DataClassificationName);
        if (dataClassificationProperty is null)
            return document;

        var newProperties = originalPropertyList.Properties.Remove(dataClassificationProperty);
        var newPropertyList = originalPropertyList.WithProperties(newProperties);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(originalPropertyList, newPropertyList);
        return document.WithSyntaxRoot(newRoot);
    }
}