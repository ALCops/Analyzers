using System.Collections.Immutable;
using ALCops.Common.Reflection;
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

    private sealed class PragmaTransferPlan
    {
        public Dictionary<ParameterSyntax, List<SyntaxTrivia>> PragmasByRecipient { get; } = [];
        public Dictionary<ParameterSyntax, List<SyntaxTrivia>> PragmasByClosingParen { get; } = [];
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

        var newRoot = RemoveParameters(root, [parameter]);

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

    private static SyntaxNode? RemoveParameters(SyntaxNode root,
        IEnumerable<ParameterSyntax> parametersToRemove)
    {
        var parameters = parametersToRemove.ToHashSet();
        var pragmasToRemove = GetPragmasToRemove(root, parameters);
        var pragmaTransferPlan = GetPragmaTransferPlan(root, parameters, pragmasToRemove);
        var annotationsByParameter = CreateParameterAnnotations(parameters, pragmaTransferPlan);

        root = AddParameterAnnotations(root, annotationsByParameter);

        if (pragmasToRemove.Count > 0)
        {
            var pragmaSpans = pragmasToRemove.Select(pragma => pragma.Span).ToHashSet();
            var triviaToRemove = root.GetDirectives()
                .OfType<PragmaWarningDirectiveTriviaSyntax>()
                .Where(directive => pragmaSpans.Contains(directive.ParentTrivia.Span))
                .SelectMany(directive => directive.ParentTrivia.Token.LeadingTrivia
                    .SkipWhile(trivia => trivia != directive.ParentTrivia)
                    .TakeWhile(trivia => trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                        || trivia.ToString().All(char.IsWhiteSpace)
                        || trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
                .ToList();

            root = root.ReplaceTrivia(triviaToRemove, (_, _) => default);
        }

        root = TransferPragmas(root, pragmaTransferPlan.PragmasByRecipient, annotationsByParameter);
        root = TransferPragmasToClosingParen(root, pragmaTransferPlan.PragmasByClosingParen, annotationsByParameter);

        var rewrittenParameters = parameters
            .Select(parameter => FindAnnotatedParameter(root, annotationsByParameter[parameter]))
            .Where(parameter => parameter is not null)
            .Cast<ParameterSyntax>();

        return root.RemoveNodes(rewrittenParameters, SyntaxRemoveOptions.KeepNoTrivia);
    }

    private static Dictionary<ParameterSyntax, SyntaxAnnotation> CreateParameterAnnotations(
        HashSet<ParameterSyntax> parametersToRemove, PragmaTransferPlan pragmaTransferPlan)
    {
        var annotationsByParameter = new Dictionary<ParameterSyntax, SyntaxAnnotation>();

        foreach (var parameter in parametersToRemove
            .Concat(pragmaTransferPlan.PragmasByRecipient.Keys)
            .Concat(pragmaTransferPlan.PragmasByClosingParen.Keys))
        {
            annotationsByParameter.TryAdd(parameter, new SyntaxAnnotation());
        }

        return annotationsByParameter;
    }

    private static SyntaxNode AddParameterAnnotations(SyntaxNode root,
        Dictionary<ParameterSyntax, SyntaxAnnotation> annotationsByParameter)
    {
        return root.ReplaceNodes(
            annotationsByParameter.Keys,
            (parameter, _) => parameter.WithAdditionalAnnotations(annotationsByParameter[parameter]));
    }

    private static ParameterSyntax? FindAnnotatedParameter(SyntaxNode root, SyntaxAnnotation annotation) =>
        root.GetAnnotatedNodes(annotation).OfType<ParameterSyntax>().FirstOrDefault();

    private static HashSet<SyntaxTrivia> GetPragmasToRemove(SyntaxNode root,
        HashSet<ParameterSyntax> parametersToRemove)
    {
        var pragmasToRemove = new HashSet<SyntaxTrivia>();

        foreach (var pair in GetPragmaPairs(root))
        {
            var parameterList = parametersToRemove
                .Select(parameter => parameter.AncestorsAndSelf().OfType<MethodOrTriggerDeclarationSyntax>()
                    .FirstOrDefault()?.ParameterList)
                .FirstOrDefault(list => list is not null
                    && IsWithin(pair.Disable.Span, list.FullSpan)
                    && IsWithin(pair.Restore.Span, list.FullSpan));

            if (parameterList is null)
            {
                continue;
            }

            var enclosedParameters = parameterList.Parameters
                .Where(parameter => parameter.Span.Start >= pair.Disable.Span.End
                    && parameter.Span.End <= pair.Restore.Span.Start)
                .ToList();

            if (enclosedParameters.Count > 0 && enclosedParameters.All(parametersToRemove.Contains))
            {
                pragmasToRemove.Add(pair.Disable);
                pragmasToRemove.Add(pair.Restore);
            }
        }

        foreach (var parameter in parametersToRemove)
        {
            var parameterList = parameter.AncestorsAndSelf()
                .OfType<MethodOrTriggerDeclarationSyntax>()
                .FirstOrDefault()?.ParameterList;

            if (parameterList is null)
            {
                continue;
            }

            var enclosingPair = GetLocallyEnclosingPragmaPair(root, parameter);

            if (enclosingPair is null)
            {
                continue;
            }

            var enclosedParameters = parameterList.Parameters
                .Where(candidate => candidate.Span.Start >= enclosingPair.Value.Disable.Span.End
                    && candidate.Span.End <= enclosingPair.Value.Restore.Span.Start);

            if (enclosedParameters.Any() && enclosedParameters.All(parametersToRemove.Contains))
            {
                pragmasToRemove.Add(enclosingPair.Value.Disable);
                pragmasToRemove.Add(enclosingPair.Value.Restore);
            }
        }

        return pragmasToRemove;
    }

    private static (SyntaxTrivia Disable, SyntaxTrivia Restore)? GetLocallyEnclosingPragmaPair(
        SyntaxNode root, ParameterSyntax parameter)
    {
        var method = parameter.AncestorsAndSelf()
            .OfType<MethodOrTriggerDeclarationSyntax>()
            .FirstOrDefault();

        if (method is null)
        {
            return null;
        }

        var pragmas = GetPragmaDirectives(root)
            .Where(pragma => IsWithin(pragma.Span, method.FullSpan))
            .ToList();
        var disable = pragmas
            .LastOrDefault(pragma => pragma.Span.End <= parameter.Span.Start
                && IsPragma(pragma, "#pragma warning disable"));

        if (disable == default)
        {
            return null;
        }

        string errorCodes = GetPragmaErrorCodes(disable, "#pragma warning disable");
        var restore = pragmas
            .FirstOrDefault(pragma => pragma.Span.Start >= parameter.Span.End
                && IsPragma(pragma, "#pragma warning restore")
                && string.Equals(
                    GetPragmaErrorCodes(pragma, "#pragma warning restore"),
                    errorCodes,
                    StringComparison.OrdinalIgnoreCase));

        return restore == default ? null : (disable, restore);
    }

    private static PragmaTransferPlan GetPragmaTransferPlan(SyntaxNode root,
        HashSet<ParameterSyntax> parametersToRemove, HashSet<SyntaxTrivia> pragmasToRemove)
    {
        var pragmaTransferPlan = new PragmaTransferPlan();

        foreach (var pragma in GetPragmaDirectives(root))
        {
            if (pragmasToRemove.Contains(pragma))
            {
                continue;
            }

            var target = parametersToRemove.FirstOrDefault(parameter => IsWithin(pragma.Span, parameter.FullSpan));

            if (target is null)
            {
                continue;
            }

            var method = target.AncestorsAndSelf().OfType<MethodOrTriggerDeclarationSyntax>().FirstOrDefault();
            var recipient = method?.ParameterList.Parameters
                .SkipWhile(parameter => parameter != target)
                .Skip(1)
                .FirstOrDefault(parameter => !parametersToRemove.Contains(parameter));

            if (recipient is null)
            {
                foreach (var trivia in GetPragmaTransferTrivia(target, pragma))
                {
                    AddPragma(pragmaTransferPlan.PragmasByClosingParen, target, trivia);
                }
            }
            else
            {
                foreach (var trivia in GetPragmaTransferTrivia(target, pragma))
                {
                    AddPragma(pragmaTransferPlan.PragmasByRecipient, recipient, trivia);
                }
            }
        }

        return pragmaTransferPlan;
    }

    private static IEnumerable<SyntaxTrivia> GetPragmaTransferTrivia(ParameterSyntax parameter,
        SyntaxTrivia pragma)
    {
        var leadingTrivia = parameter.GetLeadingTrivia();
        int pragmaIndex = leadingTrivia.IndexOf(pragma);

        if (pragmaIndex < 0)
        {
            return [pragma];
        }

        var transferTrivia = new List<SyntaxTrivia> { pragma };

        for (int index = pragmaIndex - 1; index >= 0; index--)
        {
            var trivia = leadingTrivia[index];

            if (trivia.ToString().All(char.IsWhiteSpace))
            {
                continue;
            }

            if (!IsComment(trivia))
            {
                break;
            }

            transferTrivia.Insert(0, trivia);
        }

        return transferTrivia;
    }

    private static bool IsComment(SyntaxTrivia trivia)
    {
        return trivia.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia)
            || trivia.IsKind(EnumProvider.SyntaxKind.CommentTrivia);
    }

    private static void AddPragma(Dictionary<ParameterSyntax, List<SyntaxTrivia>> pragmasByParameter,
        ParameterSyntax parameter, SyntaxTrivia pragma)
    {
        if (!pragmasByParameter.TryGetValue(parameter, out var pragmas))
        {
            pragmas = [];
            pragmasByParameter.Add(parameter, pragmas);
        }

        pragmas.Add(pragma);
    }

    private static SyntaxNode TransferPragmas(SyntaxNode root,
        Dictionary<ParameterSyntax, List<SyntaxTrivia>> pragmasByRecipient,
        Dictionary<ParameterSyntax, SyntaxAnnotation> annotationsByParameter)
    {
        foreach (var (recipient, pragmas) in pragmasByRecipient)
        {
            var rewrittenRecipient = FindAnnotatedParameter(root, annotationsByParameter[recipient]);

            if (rewrittenRecipient is null)
            {
                continue;
            }

            string leadingTrivia = rewrittenRecipient.GetLeadingTrivia().ToFullString().TrimStart();
            string indentation = GetParameterIndentation(root, rewrittenRecipient);
            string transferredPragmas = string.Concat(pragmas.Select(pragma =>
                $"{pragma}{Environment.NewLine}{indentation}"));
            var replacement = rewrittenRecipient.WithLeadingTrivia(
                SyntaxFactory.ParseLeadingTrivia($"{indentation}{transferredPragmas}{leadingTrivia}"));

            root = root.ReplaceNode(rewrittenRecipient, replacement);
        }

        return root;
    }

    private static SyntaxNode TransferPragmasToClosingParen(SyntaxNode root,
        Dictionary<ParameterSyntax, List<SyntaxTrivia>> pragmasByTarget,
        Dictionary<ParameterSyntax, SyntaxAnnotation> annotationsByParameter)
    {
        foreach (var (target, pragmas) in pragmasByTarget)
        {
            var rewrittenTarget = FindAnnotatedParameter(root, annotationsByParameter[target]);

            if (rewrittenTarget is null)
            {
                continue;
            }

            var parameterList = rewrittenTarget.AncestorsAndSelf()
                .OfType<MethodOrTriggerDeclarationSyntax>()
                .FirstOrDefault()?.ParameterList;

            if (parameterList is null)
            {
                continue;
            }

            var closeParenToken = parameterList.GetLastToken();
            string indentation = GetParameterIndentation(root, rewrittenTarget);
            string transferredPragmas = string.Concat(pragmas.Select(pragma =>
                $"{Environment.NewLine}{indentation}{pragma}{Environment.NewLine}{indentation}"));
            var replacement = closeParenToken.WithLeadingTrivia(
                SyntaxFactory.ParseLeadingTrivia($"{transferredPragmas}{closeParenToken.LeadingTrivia}"));

            root = root.ReplaceToken(closeParenToken, replacement);
        }

        return root;
    }

    private static string GetParameterIndentation(SyntaxNode root, ParameterSyntax parameter)
    {
        string source = root.ToFullString();
        int lineStart = source.LastIndexOf('\n', parameter.Span.Start - 1) + 1;
        string indentation = source[lineStart..parameter.Span.Start];

        if (indentation.All(char.IsWhiteSpace))
        {
            return indentation;
        }

        return string.Empty;
    }

    private static bool IsWithin(TextSpan innerSpan, TextSpan outerSpan) =>
        innerSpan.Start >= outerSpan.Start && innerSpan.End <= outerSpan.End;

    private static IEnumerable<(SyntaxTrivia Disable, SyntaxTrivia Restore)> GetPragmaPairs(SyntaxNode root)
    {
        var disabledPragmas = new Dictionary<string, Stack<SyntaxTrivia>>(StringComparer.OrdinalIgnoreCase);

        foreach (var pragma in GetPragmaDirectives(root))
        {
            const string disablePrefix = "#pragma warning disable";
            const string restorePrefix = "#pragma warning restore";

            if (IsPragma(pragma, disablePrefix))
            {
                string errorCodes = GetPragmaErrorCodes(pragma, disablePrefix);

                if (!disabledPragmas.TryGetValue(errorCodes, out var disabled))
                {
                    disabled = new Stack<SyntaxTrivia>();
                    disabledPragmas.Add(errorCodes, disabled);
                }

                disabled.Push(pragma);
            }
            else if (IsPragma(pragma, restorePrefix))
            {
                string errorCodes = GetPragmaErrorCodes(pragma, restorePrefix);

                if (disabledPragmas.TryGetValue(errorCodes, out var disabled) && disabled.Count > 0)
                {
                    yield return (disabled.Pop(), pragma);
                }
            }
        }
    }

    private static bool IsPragma(SyntaxTrivia pragma, string prefix) =>
        pragma.ToString().Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static string GetPragmaErrorCodes(SyntaxTrivia pragma, string prefix) =>
        pragma.ToString().Trim()[prefix.Length..].Trim();

    private static IEnumerable<SyntaxTrivia> GetPragmaDirectives(SyntaxNode root) =>
        root.GetDirectives()
            .OfType<PragmaWarningDirectiveTriviaSyntax>()
            .Select(directive => directive.ParentTrivia)
            .Where(trivia => trivia.ToString().Trim()
                .StartsWith("#pragma warning", StringComparison.OrdinalIgnoreCase));

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
        var newRoot = RemoveParameters(root, parametersToRemove);

        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }
}
