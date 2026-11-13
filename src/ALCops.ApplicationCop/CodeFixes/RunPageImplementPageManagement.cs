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

[CodeFixProvider(nameof(RunPageImplementPageManagementCodeFixProvider))]
public sealed class RunPageImplementPageManagementCodeFixProvider : CodeFixProvider
{
    private const string PageManagementCodeunitName = "Page Management";
    private const string DefaultVariableName = "PageManagement";
    private const string PageRunMethodName = "PageRun";
    private const string PageRunModalMethodName = "PageRunModal";
    private const string PageRunAtFieldMethodName = "PageRunAtField";

    private sealed class RunPageImplementPageManagementCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RunPageImplementPageManagementCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.RunPageImplementPageManagement.Id);

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

    private static RunPageImplementPageManagementCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new RunPageImplementPageManagementCodeAction(
            ApplicationCopAnalyzers.RunPageImplementPageManagementCodeAction,
            ct => ImplementPageManagement(document, node, ct),
            nameof(RunPageImplementPageManagementCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ImplementPageManagement(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var originalInvocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>(); // Page.Run(...);
        if (originalInvocation is null)
            return document;

        var containingMethodOrTrigger = originalInvocation.FirstAncestorOrSelf<MethodOrTriggerDeclarationSyntax>();
        if (containingMethodOrTrigger is null)
            return document;

        var existingVariableName = containingMethodOrTrigger.FindCodeunitVariableName(PageManagementCodeunitName);
        var variableName = existingVariableName ?? DefaultVariableName;

        // Define the corresonding method name based on original invocation
        if (originalInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var runModel = originalInvocation.Expression.GetNameStringValue().IsSameName("RunModal");
        var methodName = GetMethodNameForPageManagement(originalInvocation, runModel);

        // Track nodes across edits so we always operate on nodes from the current tree
        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var trackedRoot = root.TrackNodes(containingMethodOrTrigger, originalInvocation);

        var currentMethodOrTrigger = trackedRoot.GetCurrentNode(containingMethodOrTrigger);
        var currentoriginalInvocation = trackedRoot.GetCurrentNode(originalInvocation);
        if (currentMethodOrTrigger is null || currentoriginalInvocation is null)
            return document;

        SyntaxNode newRoot;
        switch (currentoriginalInvocation)
        {
            case InvocationExpressionSyntax currentInvocation:
                {
                    var replacementInvocation =
                        CreateRunWithPageManagementCodeUnit(
                                methodName,
                                variableName,
                                runModel,
                                currentInvocation.ArgumentList)
                            .WithLeadingTrivia(currentInvocation.GetLeadingTrivia())
                            .WithTrailingTrivia(currentInvocation.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentInvocation, replacementInvocation);
                    break;
                }

            default:
                return document;
        }

        // If needed add "Page Management" codeunit as a local variable
        if (existingVariableName is null)
        {
            var updatedMethodOrTrigger = newRoot.GetCurrentNode(containingMethodOrTrigger);
            if (updatedMethodOrTrigger is not null)
                newRoot = newRoot.ReplaceNode(updatedMethodOrTrigger, updatedMethodOrTrigger.AddLocalVariable(
                    CodeunitVariableFactory.CreateDeclaration(variableName, PageManagementCodeunitName)));
        }

        // Only a newly declared variable in a namespaced file needs a using: a reused variable already resolves,
        // and a file without a namespace declaration resolves every object globally
        if (existingVariableName is null && newRoot is CompilationUnitSyntax { NamespaceDeclaration: not null } compilationUnit)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var targetNamespace = semanticModel?.Compilation.GetApplicationObjectNamespaceWithReflection(EnumProvider.SymbolKind.Codeunit, PageManagementCodeunitName);
            if (targetNamespace is not null)
                newRoot = compilationUnit.AddUsingDirectiveIfNeeded(targetNamespace);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    #region Method Helpers
    private static string GetMethodNameForPageManagement(InvocationExpressionSyntax invocationExpressionSyntax, bool runModel)
    {
        if (invocationExpressionSyntax.ArgumentList.Arguments.Count == 3 &&
            IsLiteralIntValue(invocationExpressionSyntax.ArgumentList.Arguments[2]))
        {
            return PageRunAtFieldMethodName;
        }

        if (runModel)
        {
            return PageRunModalMethodName;
        }

        return PageRunMethodName;
    }

    #endregion


    #region Invocation Helpers

    private static bool IsLiteralIntValue(CodeExpressionSyntax codeExpression)
    {
        if (codeExpression is not LiteralExpressionSyntax literalExpression)
            return false;

        if (literalExpression.Literal is not Int32SignedLiteralValueSyntax syntax)
            return false;

        return int.TryParse(syntax.Number.ValueText, out var value);
    }

    private static InvocationExpressionSyntax CreateRunWithPageManagementCodeUnit(string methodName, string variableName, bool runModel, ArgumentListSyntax originalArguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(methodName));

        var newArguments = CreateNewArguments(originalArguments, methodName, runModel);

        return SyntaxFactory.InvocationExpression(memberAccess, newArguments);
    }

    private static ArgumentListSyntax CreateNewArguments(ArgumentListSyntax originalArguments, string methodName, bool runModel)
    {
        var identifier = originalArguments.Arguments
            .OfType<IdentifierNameSyntax>()
            .First();

        if (string.Equals(methodName, PageRunAtFieldMethodName, StringComparison.Ordinal))
        {
            var fieldExpr = originalArguments.Arguments
                .OfType<LiteralExpressionSyntax>()
                .Last();

            var boolExpr =
                SyntaxFactory.LiteralExpression(
                    SyntaxFactory.BooleanLiteralValue(
                        SyntaxFactory.Token(runModel
                            ? EnumProvider.SyntaxKind.TrueKeyword
                            : EnumProvider.SyntaxKind.FalseKeyword)));

            return SyntaxFactory.ArgumentList(
                new SeparatedSyntaxList<CodeExpressionSyntax>()
                    .Add(identifier)
                    .Add(fieldExpr)
                    .Add(boolExpr));
        }

        return SyntaxFactory.ArgumentList(
            new SeparatedSyntaxList<CodeExpressionSyntax>()
                .Add(identifier));
    }

    #endregion
}