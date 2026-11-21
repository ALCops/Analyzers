using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class StatementSyntaxExtensions
{
    /// <summary>
    /// Whether the statement is a member of a statement list (<c>begin..end</c>, the body of
    /// <c>repeat..until</c>, the else part of <c>case</c>), so another statement can be inserted before it.
    /// </summary>
    public static bool IsInStatementList(this StatementSyntax statement) =>
        statement.Parent is BlockSyntax or RepeatStatementSyntax or CaseElseSyntax;

    /// <summary>
    /// Whether the statement is the single statement of an if/else branch, a case line or a
    /// <c>while</c>/<c>for</c>/<c>foreach</c>/<c>with</c> body, so inserting before it needs a <c>begin..end</c> wrap.
    /// <c>asserterror</c> is deliberately not an embedding parent: a single embedded body is never wrapped
    /// (the new statement would run under <c>asserterror</c>), so the climb continues to the
    /// <c>asserterror</c> statement. A body that is already a block takes an insert inside it like any block.
    /// </summary>
    public static bool IsEmbeddedStatement(this StatementSyntax statement) =>
        statement.Parent is IfStatementSyntax or CaseLineSyntax or WhileStatementSyntax
            or ForStatementSyntax or ForEachStatementSyntax or WithStatementSyntax;

    /// <summary>
    /// The nearest statement around <paramref name="node"/> before which a statement can be inserted:
    /// one in a statement list (insert) or embedded in a branch or loop body (wrap). A node in a loop or if
    /// condition yields the loop or if statement itself. Null when the node is not inside a statement.
    /// </summary>
    public static StatementSyntax? FindStatementInsertionTarget(this SyntaxNode node)
    {
        foreach (StatementSyntax statement in node.Ancestors().OfType<StatementSyntax>())
        {
            if (statement.IsInStatementList() || statement.IsEmbeddedStatement())
                return statement;
        }

        return null;
    }

    /// <summary>
    /// Whether a compiler directive makes inserting before <paramref name="target"/> unsafe, so the fix
    /// should not be offered (preserve-or-bail directive policy). Any directive (<c>#if</c>, <c>#pragma</c>,
    /// <c>#region</c>, ...) in the leading trivia of the target's first token blocks insertion, and so does
    /// any directive inside a target that would be wrapped.
    /// </summary>
    public static bool BlocksStatementInsertion(this StatementSyntax target)
    {
        // Wrapping rebuilds the statement and moves its trivia, which could unbalance or misplace
        // a directive. Outside the statement, a wrap only replaces the line break before a following
        // else, and only when plain whitespace separates them, so a directive there stays in place.
        if (!target.IsInStatementList() && target.ContainsDirectives)
            return true;

        // An insert lands before the statement's leading trivia, so a directive there (an #if that
        // guards the statement, the #endif of a preceding block) would leave the new statement on the
        // wrong side of it.
        return target.GetFirstToken().ContainsDirectives;
    }
}
