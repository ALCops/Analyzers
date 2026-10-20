using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PragmaDisableRequiresComment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PragmaDisableRequiresComment);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext ctx)
    {
        var root = ctx.Tree.GetRoot(ctx.CancellationToken);
        if (!root.ContainsDirectives)
            return;

        foreach (var directive in root.GetDirectives(d => d.IsKind(EnumProvider.SyntaxKind.PragmaWarningDirectiveTrivia)))
        {
            if (directive is not PragmaWarningDirectiveTriviaSyntax pragma ||
                !pragma.DisableOrRestoreKeyword.IsKind(EnumProvider.SyntaxKind.DisableKeyword))
                continue;

            if (HasTrailingComment(pragma) || HasCommentOnLineAbove(pragma))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PragmaDisableRequiresComment,
                pragma.GetLocation()));
        }
    }

    private static bool HasTrailingComment(PragmaWarningDirectiveTriviaSyntax pragma)
    {
        foreach (var trivia in pragma.DescendantTrivia())
        {
            if (IsNonEmptyComment(trivia))
                return true;
        }

        return false;
    }

    // The comment must stand on its own line directly above the pragma; a trailing comment on the line
    // above belongs to that line's code.
    private static bool HasCommentOnLineAbove(PragmaWarningDirectiveTriviaSyntax pragma)
    {
        var leadingTrivia = pragma.ParentTrivia.Token.LeadingTrivia;
        var index = leadingTrivia.IndexOf(pragma.ParentTrivia);

        if (index < 0)
            return false;

        var endOfLine = PreviousNonWhitespace(leadingTrivia, index);
        if (endOfLine < 0 || !leadingTrivia[endOfLine].IsKind(EnumProvider.SyntaxKind.EndOfLineTrivia))
            return false;

        var comment = PreviousNonWhitespace(leadingTrivia, endOfLine);
        if (comment < 0 || !IsNonEmptyComment(leadingTrivia[comment]))
            return false;

        var beforeComment = PreviousNonWhitespace(leadingTrivia, comment);
        return beforeComment < 0 || leadingTrivia[beforeComment].IsKind(EnumProvider.SyntaxKind.EndOfLineTrivia);
    }

    private static int PreviousNonWhitespace(SyntaxTriviaList list, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (!list[i].IsKind(EnumProvider.SyntaxKind.WhiteSpaceTrivia))
                return i;
        }

        return -1;
    }

    private static bool IsNonEmptyComment(SyntaxTrivia trivia)
    {
        string text;
        if (trivia.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia))
            text = trivia.ToString().Trim('/');
        else if (trivia.IsKind(EnumProvider.SyntaxKind.CommentTrivia))
            text = trivia.ToString().Trim('/', '*');
        else
            return false;

        return !string.IsNullOrWhiteSpace(text);
    }
}
