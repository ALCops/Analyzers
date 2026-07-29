using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.LinterCop.CodeFixes;

[CodeFixProvider(nameof(ParameterNotReferencedCodeFixProvider))]
public sealed class ParameterNotReferencedCodeFixProvider : CodeFixProvider
{
    private enum ProcedureKind
    {
        Regular,
        EventSubscriber
    }

    private class ParameterNotReferencedCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public ParameterNotReferencedCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DiagnosticDescriptors.ParameterNotReferenced.Id,
            DiagnosticDescriptors.EventSubscriberParameterNotReferenced.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(FixAllAsync);

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
        SyntaxNode node = syntaxRoot.FindNode(span, getInnermostNodeForTie: true);
        ProcedureKind procedureKind = GetProcedureKind(ctx.Diagnostics[0].Id);

        ctx.RegisterCodeFix(
            CreateCodeAction(node, document, procedureKind, generateFixAll: true),
            ctx.Diagnostics);
    }

    private static ProcedureKind GetProcedureKind(string diagnosticId)
    {
        if (diagnosticId == DiagnosticIds.EventSubscriberParameterNotReferenced)
        {
            return ProcedureKind.EventSubscriber;
        }

        return ProcedureKind.Regular;
    }

    private static ParameterNotReferencedCodeAction CreateCodeAction(SyntaxNode node, Document document,
        ProcedureKind procedureKind, bool generateFixAll)
    {
        string title = procedureKind == ProcedureKind.EventSubscriber
            ? LinterCopAnalyzers.EventSubscriberParameterNotReferencedCodeAction
            : LinterCopAnalyzers.ParameterNotReferencedCodeAction;

        string equivalenceKey = GetEquivalenceKey(procedureKind);

        return new ParameterNotReferencedCodeAction(
            title,
            ct => RemoveUnreferencedParameter(document, node, ct, procedureKind),
            equivalenceKey,
            generateFixAll);
    }

    private static string GetEquivalenceKey(ProcedureKind procedureKind)
    {
        return procedureKind == ProcedureKind.EventSubscriber
            ? $"{nameof(ParameterNotReferencedCodeFixProvider)}.EventSubscriber"
            : $"{nameof(ParameterNotReferencedCodeFixProvider)}.RegularProcedure";
    }

    private static async Task<Document> RemoveUnreferencedParameter(Document document, SyntaxNode node,
        CancellationToken cancellationToken, ProcedureKind procedureKind)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        SemanticModel? semanticModel = await GetSemanticModelIfNeededAsync(document, cancellationToken, procedureKind)
            .ConfigureAwait(false);

        var parameter = FindParameterInScope(node, semanticModel, procedureKind);

        if (parameter is null)
        {
            return document;
        }

        var newRoot = root.RemoveNode(parameter, SyntaxRemoveOptions.KeepNoTrivia);

        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }

    private static async Task<SemanticModel?> GetSemanticModelIfNeededAsync(Document document,
        CancellationToken cancellationToken, ProcedureKind procedureKind)
    {
        _ = procedureKind;
        return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ParameterSyntax? FindParameterInScope(SyntaxNode currentNode,
        SemanticModel? semanticModel, ProcedureKind procedureKind,
        Dictionary<MethodOrTriggerDeclarationSyntax, bool>? eventSubscriberCache = null)
    {
        var parameter = currentNode?.AncestorsAndSelf()
            .OfType<ParameterSyntax>()
            .FirstOrDefault();

        if (parameter is null)
        {
            return null;
        }

        if (semanticModel is null)
        {
            return parameter;
        }

        var methodDeclaration = parameter.AncestorsAndSelf()
            .OfType<MethodOrTriggerDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration is null)
        {
            return parameter;
        }

        bool isEventSubscriber;

        if (eventSubscriberCache is not null && eventSubscriberCache.TryGetValue(methodDeclaration, out bool cachedValue))
        {
            isEventSubscriber = cachedValue;
        }
        else
        {
            isEventSubscriber = (semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol)
                ?.IsEventSubscriber() ?? false;

            eventSubscriberCache?.Add(methodDeclaration, isEventSubscriber);
        }

        if ((procedureKind == ProcedureKind.EventSubscriber && !isEventSubscriber)
            || (procedureKind == ProcedureKind.Regular && isEventSubscriber))
        {
            return null;
        }

        return parameter;
    }

    private static async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document,
        Optional<ImmutableArray<TextSpan>> fixAllSpans)
    {
        CancellationToken cancellationToken = fixAllContext.CancellationToken;

        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        // Determine the spans to fix.
        // fixAllSpans is a Fix-In-Span filter: present + non-empty means "only fix within these spans".
        // Absent OR empty (e.g. RoslynTestKit's default Document scope) means "fix all diagnostics in the document".
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

        if (spans.IsDefaultOrEmpty)
        {
            return document;
        }

        // Determine scope filter from the invoked equivalence key.
        string? equivalenceKey = fixAllContext.CodeActionEquivalenceKey;
        ProcedureKind procedureKind = equivalenceKey == $"{nameof(ParameterNotReferencedCodeFixProvider)}.EventSubscriber"
            ? ProcedureKind.EventSubscriber
            : ProcedureKind.Regular;

        SemanticModel? semanticModel = await GetSemanticModelIfNeededAsync(document, cancellationToken, procedureKind)
            .ConfigureAwait(false);

        var eventSubscriberCache = new Dictionary<MethodOrTriggerDeclarationSyntax, bool>();

        // Collect all ParameterSyntax nodes matching the scope filter.
        // HashSet guards against duplicate spans producing the same node.
        var parametersToRemove = new HashSet<ParameterSyntax>();

        foreach (var span in spans)
        {
            SyntaxNode currentNode = root.FindNode(span, getInnermostNodeForTie: true);
            var parameter = FindParameterInScope(currentNode, semanticModel, procedureKind, eventSubscriberCache);

            if (parameter is not null)
            {
                parametersToRemove.Add(parameter);
            }
        }

        if (parametersToRemove.Count == 0)
        {
            return document;
        }

        // Single-pass rewrite: RemoveNodes correctly removes multiple SeparatedSyntaxList
        // elements from the same list, including their associated separators, in one operation.
        var newRoot = root.RemoveNodes(parametersToRemove, SyntaxRemoveOptions.KeepNoTrivia);

        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }
}
