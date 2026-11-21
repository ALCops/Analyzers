using System.Diagnostics.CodeAnalysis;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Editing;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces.Formatting;

namespace ALCops.Common.Extensions;

/// <summary>
/// CodeFix edits through the SDK's <see cref="SyntaxEditor"/>. The only members of Common that need the
/// Workspaces assembly, kept apart so analyzer code paths never load it.
/// </summary>
public static class SyntaxEditorExtensions
{
    /// <summary>
    /// Inserts <paramref name="statements"/> before <paramref name="target"/> (see
    /// <see cref="StatementSyntaxExtensions.FindStatementInsertionTarget"/>). A target in a statement list
    /// gets them as preceding siblings; an embedded target is wrapped in
    /// <c>begin &lt;statements&gt; &lt;target&gt; end</c>. Edits on nested targets compose: the editor tracks
    /// every edited node and hands each replacement the already rewritten target.
    /// The inserted statements should carry elastic leading trivia so the CodeAction formatter indents them;
    /// <paramref name="lineBreak"/> is the document's own (<see cref="SyntaxNodeExtensions.GetLineBreak"/>).
    /// </summary>
    public static void InsertStatementsBefore(this SyntaxEditor editor, StatementSyntax target,
        IReadOnlyList<StatementSyntax> statements, SyntaxTrivia lineBreak)
    {
        if (target.IsInStatementList())
        {
            editor.InsertBefore(target, statements);
            return;
        }

        editor.ReplaceNode(target, current => WrapInBlock((StatementSyntax)current, target, statements, lineBreak));

        // The formatter only adds line breaks, so a following else joins the new end keyword (end else)
        // only when the wrap drops the line break between them. The else keyword's leading whitespace
        // becomes elastic, so the formatter also lays out the else branch as it would in new code.
        if (JoinsElse(target, out IfStatementSyntax? ifStatement))
        {
            editor.ReplaceNode(ifStatement, current =>
            {
                var currentIf = (IfStatementSyntax)current;
                return currentIf.WithElseKeywordToken(currentIf.ElseKeywordToken.WithLeadingTrivia(SyntaxFactory.ElasticMarker));
            });
        }
    }

    /// <summary>
    /// Wraps <paramref name="current"/> (the rewritten form of <paramref name="original"/>) in begin..end
    /// after <paramref name="statements"/>. The block takes over the statement's leading and trailing trivia
    /// and, when the statement ended with one, its semicolon.
    /// </summary>
    private static BlockSyntax WrapInBlock(StatementSyntax current, StatementSyntax original,
        IReadOnlyList<StatementSyntax> statements, SyntaxTrivia lineBreak)
    {
        // The trailing trivia moves behind the block, so a trailing line comment ends up after the end
        // keyword instead of commenting it out. The end keyword gets a real line break: the factory's
        // elastic one makes the formatter put a blank line before it.
        SyntaxTriviaList trailingTrivia = current.GetTrailingTrivia();
        StatementSyntax inner = current.WithLeadingTrivia(SyntaxFactory.ElasticMarker).WithoutTrailingTrivia();

        BlockSyntax block = SyntaxFactory.Block(SyntaxFactory.List(statements.Append(inner)));
        block = block.WithEndKeywordToken(block.EndKeywordToken.WithLeadingTrivia(lineBreak));

        if (current.GetLastToken().Kind == EnumProvider.SyntaxKind.SemicolonToken)
            block = block.WithSemicolonToken(SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));

        if (JoinsElse(original, out _) && trailingTrivia.Count > 0
            && trailingTrivia[trailingTrivia.Count - 1].Kind == EnumProvider.SyntaxKind.EndOfLineTrivia)
        {
            trailingTrivia = trailingTrivia.RemoveAt(trailingTrivia.Count - 1);
        }

        return block
            .WithLeadingTrivia(current.GetLeadingTrivia())
            .WithTrailingTrivia(trailingTrivia)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    /// <summary>
    /// Whether <paramref name="statement"/> is a then-branch whose else moves onto the line of the new end
    /// keyword: only when nothing but whitespace separates them, so no comment or directive moves.
    /// </summary>
    private static bool JoinsElse(StatementSyntax statement, [NotNullWhen(true)] out IfStatementSyntax? ifStatement)
    {
        ifStatement = statement.Parent as IfStatementSyntax;
        return ifStatement is not null
            && ifStatement.Statement == statement
            && ifStatement.ElseStatement is not null
            && IsWhitespace(statement.GetTrailingTrivia())
            && IsWhitespace(ifStatement.ElseKeywordToken.LeadingTrivia);
    }

    private static bool IsWhitespace(SyntaxTriviaList triviaList) =>
        triviaList.All(trivia => trivia.ToString().All(char.IsWhiteSpace));
}
