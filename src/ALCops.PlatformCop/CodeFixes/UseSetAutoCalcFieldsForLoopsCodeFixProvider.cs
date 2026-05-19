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
        if (node is not InvocationExpressionSyntax invocation)
            return;

        // Don't offer CodeFix when there's no valid insertion target
        // (e.g., report OnAfterGetRecord where SetAutoCalcFields belongs in OnPreDataItem)
        if (FindInsertionTarget(invocation) is null)
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

        // Remove CalcFields statement first, then insert SetAutoCalcFields.
        // The insertion target is before the CalcFields in the file, so its span
        // is not affected by the removal.
        var newRoot = root.RemoveNode(calcFieldsStatement, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot is null)
            return document;

        // After removing, find the insertion target in the modified tree.
        // Use FullSpan.Start to locate the node since its start position is stable.
        var updatedTarget = FindNodeByStartPosition(newRoot, insertionTarget.FullSpan.Start);
        if (updatedTarget is null)
            return document;

        newRoot = newRoot.InsertNodesBefore(updatedTarget, new[] { setAutoCalcFieldsStatement });

        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Finds a statement node at the given start position in the syntax tree.
    /// More robust than FindNode(span) when the node's end position may have shifted.
    /// </summary>
    private static StatementSyntax? FindNodeByStartPosition(SyntaxNode root, int startPosition)
    {
        var token = root.FindToken(startPosition);
        var node = token.Parent;

        while (node is not null)
        {
            if (node is StatementSyntax statement && node.FullSpan.Start == startPosition)
                return statement;
            node = node.Parent;
        }

        return null;
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
    /// For repeat-until: walks up to find the enclosing if-FindSet or while statement.
    /// SetAutoCalcFields must be placed BEFORE the FindSet/Find call for it to take effect.
    /// For report triggers: returns null (SetAutoCalcFields belongs in OnPreDataItem).
    /// </summary>
    private static StatementSyntax? FindInsertionTarget(InvocationExpressionSyntax calcFieldsInvocation)
    {
        var current = calcFieldsInvocation.Parent;
        while (current is not null)
        {
            switch (current)
            {
                case RepeatStatementSyntax repeatStatement:
                    // Check if an ancestor if-statement contains the FindSet condition
                    var enclosingIf = FindEnclosingIfWithFind(repeatStatement);
                    if (enclosingIf is not null)
                        return enclosingIf;

                    // Otherwise look for a standalone FindSet statement before the repeat
                    var precedingFind = FindPrecedingFindStatement(repeatStatement);
                    if (precedingFind is not null)
                        return precedingFind;

                    return repeatStatement;

                case WhileStatementSyntax whileStatement:
                    return whileStatement;

                case ForEachStatementSyntax forEachStatement:
                    return forEachStatement;

                case MethodOrTriggerDeclarationSyntax:
                    // We've reached the method body without finding a loop.
                    // This is the report OnAfterGetRecord case where SetAutoCalcFields
                    // belongs in OnPreDataItem, not here. No CodeFix for this scenario.
                    return null;
            }
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Walks up from the repeat statement to find an enclosing if-statement whose condition
    /// contains a FindSet/Find call. Handles both:
    ///   if Rec.FindSet() then repeat...
    ///   if Rec.FindSet() then begin repeat... end;
    /// </summary>
    private static IfStatementSyntax? FindEnclosingIfWithFind(RepeatStatementSyntax repeatStatement)
    {
        SyntaxNode? parent = repeatStatement.Parent;

        // If repeat is inside begin...end, go one level up
        if (parent is BlockSyntax)
            parent = parent.Parent;

        if (parent is IfStatementSyntax ifStatement)
            return ifStatement;

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
