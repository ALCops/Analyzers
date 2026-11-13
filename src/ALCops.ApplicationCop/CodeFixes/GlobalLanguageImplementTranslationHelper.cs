using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(GlobalLanguageImplementTranslationHelperCodeFixProvider))]
public sealed class GlobalLanguageImplementTranslationHelperCodeFixProvider : CodeFixProvider
{
    private const int DefaultGlobalLanguageId = 1033;
    private const string TranslationHelperCodeunitName = "Translation Helper";
    private const string DefaultVariableName = "TranslationHelper";
    private const string SetGlobalLanguageByIdMethodName = "SetGlobalLanguageById";
    private const string SetGlobalLanguageToDefaultMethodName = "SetGlobalLanguageToDefault";

    private sealed class GlobalLanguageImplementTranslationHelperCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public GlobalLanguageImplementTranslationHelperCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.GlobalLanguageImplementTranslationHelper.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Document document = context.Document;
        TextSpan span = context.Span;
        CancellationToken cancellationToken = context.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        RegisterInstanceCodeFix(context, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span);
        ctx.RegisterCodeFix(CreateCodeAction(node, document, generateFixAll: false), ctx.Diagnostics[0]);
    }

    private static GlobalLanguageImplementTranslationHelperCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new GlobalLanguageImplementTranslationHelperCodeAction(
            ApplicationCopAnalyzers.GlobalLanguageImplementTranslationHelperCodeAction,
            ct => ImplementTranslationHelperCodeAction(document, node, ct),
            nameof(GlobalLanguageImplementTranslationHelperCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ImplementTranslationHelperCodeAction(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var originalInvocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>(); // GlobalLanguage(1033);
        var originalAssignment = node.FirstAncestorOrSelf<AssignmentStatementSyntax>();  // GlobalLanguage := 1033;
        if (originalInvocation is null && originalAssignment is null)
            return document;

        // For assignment we want to replace the whole statement; for invocation we replace the invocation expression.
        SyntaxNode anchorNode = (SyntaxNode?)originalAssignment ?? originalInvocation!;

        var containingMethodOrTrigger = anchorNode.FirstAncestorOrSelf<MethodOrTriggerDeclarationSyntax>();
        if (containingMethodOrTrigger is null)
            return document;

        var existingVariableName = containingMethodOrTrigger.FindCodeunitVariableName(TranslationHelperCodeunitName);
        var variableName = existingVariableName ?? DefaultVariableName;

        // Track nodes across edits so we always operate on nodes from the current tree
        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var trackedRoot = root.TrackNodes(containingMethodOrTrigger, anchorNode);

        var currentMethodOrTrigger = trackedRoot.GetCurrentNode(containingMethodOrTrigger);
        var currentAnchorNode = trackedRoot.GetCurrentNode(anchorNode);
        if (currentMethodOrTrigger is null || currentAnchorNode is null)
            return document;

        SyntaxNode newRoot;
        switch (currentAnchorNode)
        {
            // GlobalLanguage(1033);
            case InvocationExpressionSyntax currentInvocation:
                {
                    var firstArgExpr =
                        currentInvocation.ArgumentList.Arguments.Count > 0
                            ? currentInvocation.ArgumentList.Arguments[0]
                            : null;

                    var replacementInvocation =
                        CreateSetGlobalLanguageInvocation(
                                variableName,
                                currentInvocation.ArgumentList,
                                firstArgExpr)
                            .WithLeadingTrivia(currentInvocation.GetLeadingTrivia())
                            .WithTrailingTrivia(currentInvocation.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentInvocation, replacementInvocation);
                    break;
                }

            // GlobalLanguage := 1033;
            case AssignmentStatementSyntax currentAssignment:
                {
                    var args = default(SeparatedSyntaxList<CodeExpressionSyntax>).Add(currentAssignment.Source);
                    var argumentList = SyntaxFactory.ArgumentList(args);

                    var invocation =
                        CreateSetGlobalLanguageInvocation(
                            variableName,
                            argumentList,
                            currentAssignment.Source);

                    var replacementStatement =
                        SyntaxFactory.ExpressionStatement(invocation, currentAssignment.SemicolonToken)
                            .WithLeadingTrivia(currentAssignment.GetLeadingTrivia())
                            .WithTrailingTrivia(currentAssignment.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentAssignment, replacementStatement);
                    break;
                }

            default:
                return document;
        }

        // If needed add "Translation Helper" codeunit as a local variable
        if (existingVariableName is null)
        {
            var updatedMethodOrTrigger = newRoot.GetCurrentNode(containingMethodOrTrigger);
            if (updatedMethodOrTrigger is not null)
                newRoot = newRoot.ReplaceNode(updatedMethodOrTrigger, updatedMethodOrTrigger.AddLocalVariable(
                    CodeunitVariableFactory.CreateDeclaration(variableName, TranslationHelperCodeunitName)));
        }

        // Only a newly declared variable in a namespaced file needs a using: a reused variable already resolves,
        // and a file without a namespace declaration resolves every object globally
        if (existingVariableName is null && newRoot is CompilationUnitSyntax { NamespaceDeclaration: not null } compilationUnit)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var targetNamespace = semanticModel?.Compilation.GetApplicationObjectNamespaceWithReflection(EnumProvider.SymbolKind.Codeunit, TranslationHelperCodeunitName);
            if (targetNamespace is not null)
                newRoot = compilationUnit.AddUsingDirectiveIfNeeded(targetNamespace);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    #region Invocation Helpers

    private static InvocationExpressionSyntax CreateSetGlobalLanguageInvocation(string variableName, ArgumentListSyntax args, CodeExpressionSyntax? singleValueExpressionFor1033Check = null)
    {
        if (singleValueExpressionFor1033Check is not null &&
            IsLiteralIntValue(singleValueExpressionFor1033Check, DefaultGlobalLanguageId))
        {
            return CreateSetGlobalLanguageToDefaultInvocation(variableName);
        }

        return CreateSetGlobalLanguageByIdInvocation(variableName, args);
    }

    private static bool IsLiteralIntValue(CodeExpressionSyntax codeExpression, int expected)
    {
        if (codeExpression is not LiteralExpressionSyntax literalExpression)
            return false;

        if (literalExpression.Literal is not Int32SignedLiteralValueSyntax syntax)
            return false;

        return int.TryParse(syntax.Number.ValueText, out var value) &&
               value == expected;
    }

    private static InvocationExpressionSyntax CreateSetGlobalLanguageToDefaultInvocation(string variableName)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(SetGlobalLanguageToDefaultMethodName));

        return SyntaxFactory.InvocationExpression(memberAccess, SyntaxFactory.ArgumentList());
    }

    private static InvocationExpressionSyntax CreateSetGlobalLanguageByIdInvocation(string variableName, ArgumentListSyntax originalArguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(SetGlobalLanguageByIdMethodName));

        return SyntaxFactory.InvocationExpression(memberAccess, originalArguments);
    }

    #endregion
}