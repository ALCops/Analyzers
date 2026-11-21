using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(DuplicatePropertyBetweenActionAndRunObjectCodeFixProvider))]
public sealed class DuplicatePropertyBetweenActionAndRunObjectCodeFixProvider : CodeFixProvider
{
    private sealed class DuplicatePropertyBetweenActionAndRunObjectCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public DuplicatePropertyBetweenActionAndRunObjectCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.DuplicatePropertyBetweenActionAndRunObject.Id);

    // The batch fixer applies each fix on its own, so two fixes that together empty a modify block would
    // each keep the block, and the merged result would not compile. One pass sees every removed property.
    public sealed override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(FixAllAsync);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Document document = context.Document;
        TextSpan span = context.Span;
        CancellationToken cancellationToken = context.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        RegisterInstanceCodeFix(context, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot,
        TextSpan span, Document document)
    {
        var property = FindFixableProperty(syntaxRoot, span);
        if (property is null)
            return;

        var nodeToRemove = GetNodeToRemove(property, new HashSet<PropertySyntax> { property });
        if (nodeToRemove.ContainsDirectives)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(property, nodeToRemove, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static DuplicatePropertyBetweenActionAndRunObjectCodeAction CreateCodeAction(
        PropertySyntax property, SyntaxNode nodeToRemove, Document document, bool generateFixAll)
    {
        var title = string.Format(
            ApplicationCopAnalyzers.DuplicatePropertyBetweenActionAndRunObjectCodeAction,
            property.Name?.Identifier.ValueText);

        return new DuplicatePropertyBetweenActionAndRunObjectCodeAction(
            title,
            ct => ApplyFix(document, nodeToRemove, ct),
            nameof(DuplicatePropertyBetweenActionAndRunObjectCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(Document document, SyntaxNode nodeToRemove,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document,
        Optional<ImmutableArray<TextSpan>> fixAllSpans)
    {
        var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        // The SDK can hand over a present but empty span list for a document-scope Fix All, so an empty
        // list falls back to the document's diagnostics.
        ImmutableArray<TextSpan> spans;
        if (fixAllSpans.HasValue && !fixAllSpans.Value.IsDefaultOrEmpty)
        {
            spans = fixAllSpans.Value;
        }
        else
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            spans = diagnostics.Select(d => d.Location.SourceSpan).ToImmutableArray();
        }

        var properties = new HashSet<PropertySyntax>();
        foreach (var span in spans)
        {
            var property = FindFixableProperty(root, span);
            if (property is not null)
                properties.Add(property);
        }

        var nodesToRemove = new HashSet<SyntaxNode>();
        foreach (var property in properties)
        {
            var nodeToRemove = GetNodeToRemove(property, properties);
            if (!nodeToRemove.ContainsDirectives)
                nodesToRemove.Add(nodeToRemove);
        }

        if (nodesToRemove.Count == 0)
            return document;

        var newRoot = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }

    // Removing a node with KeepNoTrivia would delete any directive in its trivia. A directive line is
    // leading trivia of the next token, so this also covers a directive on the line directly above the
    // property, such as the #endif or #pragma restore that closes the preceding sibling.
    private static PropertySyntax? FindFixableProperty(SyntaxNode root, TextSpan span)
    {
        var property = root
            .FindNode(span, getInnermostNodeForTie: true)?
            .AncestorsAndSelf()
            .OfType<PropertySyntax>()
            .FirstOrDefault();

        return property is null || property.ContainsDirectives ? null : property;
    }

    // The compiler rejects a modify block without properties or triggers (AL0403), so when every property
    // of the block is removed the whole block goes. A property's full span is its indentation, its text and
    // its end of line, so KeepNoTrivia deletes exactly the property's line, or the block's lines.
    private static SyntaxNode GetNodeToRemove(PropertySyntax property, ISet<PropertySyntax> removedProperties)
    {
        if (property.Parent is PropertyListSyntax propertyList &&
            propertyList.Parent is ActionModifyChangeSyntax modify &&
            modify.Triggers.Count == 0 &&
            propertyList.Properties.OfType<PropertySyntax>().All(removedProperties.Contains))
        {
            return modify;
        }

        return property;
    }
}
