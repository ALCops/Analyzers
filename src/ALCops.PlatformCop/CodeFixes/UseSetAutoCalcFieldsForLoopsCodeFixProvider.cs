using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(UseSetAutoCalcFieldsForLoopsCodeFixProvider))]
public sealed class UseSetAutoCalcFieldsForLoopsCodeFixProvider : CodeFixProvider
{
    private class UseSetAutoCalcFieldsForLoopsCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public UseSetAutoCalcFieldsForLoopsCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.UseSetAutoCalcFieldsForLoops.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        SyntaxNode node = syntaxRoot.FindNode(span);
        if (node is not InvocationExpressionSyntax)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(node, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static UseSetAutoCalcFieldsForLoopsCodeAction CreateCodeAction(
        SyntaxNode node, Document document, bool generateFixAll)
    {
        return new UseSetAutoCalcFieldsForLoopsCodeAction(
            PlatformCopAnalyzers.UseSetAutoCalcFieldsForLoopsCodeAction,
            ct => ApplyFix(document, node, ct),
            nameof(UseSetAutoCalcFieldsForLoopsCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (node is not InvocationExpressionSyntax invocation)
            return document;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var variableName = memberAccess.Expression.ToString();
        var arguments = invocation.ArgumentList?.Arguments ?? default;

        if (arguments.Count == 0)
            return document;

        // Build SetAutoCalcFields statement
        var setAutoCalcFieldsStatement = BuildSetAutoCalcFieldsStatement(variableName, arguments);

        // Find the insertion point: before the loop or before the FindSet/Find call
        var insertionTarget = FindInsertionTarget(invocation);
        if (insertionTarget is null)
            return document;

        // Find the statement containing the CalcFields call (to remove it)
        var calcFieldsStatement = FindContainingStatement(invocation);
        if (calcFieldsStatement is null)
            return document;

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        // Remove CalcFields statement first, then insert SetAutoCalcFields
        // We do remove first because after insert, spans shift
        var newRoot = root.RemoveNode(calcFieldsStatement, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot is null)
            return document;

        // After removing, we need to find the insertion target again in the modified tree
        // Since we removed a node INSIDE the loop body, the insertion target (before the loop)
        // should still be findable by span (it's earlier in the file, so its span didn't change)
        var updatedTarget = newRoot.FindNode(insertionTarget.Span);
        if (updatedTarget is null)
            return document;

        // Find the statement node for insertion
        var targetStatement = updatedTarget as StatementSyntax
            ?? updatedTarget.FirstAncestorOrSelf<StatementSyntax>();
        if (targetStatement is null)
            return document;

        newRoot = newRoot.InsertNodesBefore(targetStatement, new[] { setAutoCalcFieldsStatement });

        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionStatementSyntax BuildSetAutoCalcFieldsStatement(
        string variableName, SeparatedSyntaxList<CodeExpressionSyntax> calcFieldsArguments)
    {
        var variableIdentifier = SyntaxFactory.IdentifierName(variableName);

        var setAutoCalcFieldsAccess = SyntaxFactory.MemberAccessExpression(
            variableIdentifier,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName("SetAutoCalcFields"));

        var argumentList = SyntaxFactory.ArgumentList(calcFieldsArguments);
        var invocationExpr = SyntaxFactory.InvocationExpression(setAutoCalcFieldsAccess, argumentList);

        return SyntaxFactory.ExpressionStatement(invocationExpr,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));
    }

    /// <summary>
    /// Finds the statement before which to insert SetAutoCalcFields.
    /// For repeat-until: finds the FindSet/Find call before the repeat.
    /// For while loops: finds the while statement itself.
    /// For report triggers: uses the first statement in the trigger body.
    /// </summary>
    private static StatementSyntax? FindInsertionTarget(InvocationExpressionSyntax calcFieldsInvocation)
    {
        var current = calcFieldsInvocation.Parent;
        while (current is not null)
        {
            switch (current)
            {
                case RepeatStatementSyntax repeatStatement:
                    // For repeat-until, try to find the FindSet/Find statement before the repeat
                    return FindPrecedingFindStatement(repeatStatement) ?? repeatStatement;

                case WhileStatementSyntax whileStatement:
                    return whileStatement;

                case ForEachStatementSyntax forEachStatement:
                    return forEachStatement;

                case MethodOrTriggerDeclarationSyntax:
                    // We've reached the method body without finding a loop
                    // This is the report OnAfterGetRecord case - insert before the CalcFields itself
                    return FindContainingStatement(calcFieldsInvocation);
            }
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Looks for the FindSet/Find statement immediately before a repeat statement.
    /// </summary>
    private static StatementSyntax? FindPrecedingFindStatement(RepeatStatementSyntax repeatStatement)
    {
        if (repeatStatement.Parent is not BlockSyntax block)
            return null;

        var statements = block.Statements;
        for (int i = 0; i < statements.Count; i++)
        {
            if (statements[i] == repeatStatement && i > 0)
                return statements[i - 1];
        }
        return null;
    }

    private static ExpressionStatementSyntax? FindContainingStatement(SyntaxNode node)
    {
        return node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
    }
}
