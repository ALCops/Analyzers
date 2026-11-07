using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class KeywordShouldEndLine : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.KeywordShouldEndLine);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, EnumProvider.SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeBlock, EnumProvider.SyntaxKind.Block);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not IfStatementSyntax ifStatement)
            return;

        // 'then begin' deliberately shares a line; the block is checked by its own 'begin'.
        if (ifStatement.Statement is BlockSyntax)
            return;

        ReportIfFollowedOnSameLine(ctx, ifStatement.ThenKeywordToken, "then");
    }

    private static void AnalyzeBlock(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not BlockSyntax block)
            return;

        ReportIfFollowedOnSameLine(ctx, block.BeginKeywordToken, "begin");
    }

    private static void ReportIfFollowedOnSameLine(SyntaxNodeAnalysisContext ctx, SyntaxToken keyword, string keywordText)
    {
        if (keyword.IsMissing)
            return;

        var next = keyword.GetNextToken();
        if (next.IsMissing || next.Kind == EnumProvider.SyntaxKind.None)
            return;

        // Comments are trivia, so a trailing comment never counts as a following token.
        var keywordLine = keyword.GetLocation().GetLineSpan().EndLinePosition.Line;
        var nextLine = next.GetLocation().GetLineSpan().StartLinePosition.Line;
        if (nextLine != keywordLine)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.KeywordShouldEndLine,
            keyword.GetLocation(),
            keywordText));
    }
}
