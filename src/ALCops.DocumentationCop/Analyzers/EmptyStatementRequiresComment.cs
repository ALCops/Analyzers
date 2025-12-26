using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class EmptyStatementRequiresComment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.EmptyStatementRequiresComment);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            this.AnalyzeEmptyStatement,
            EnumProvider.OperationKind.EmptyStatement);

    private void AnalyzeEmptyStatement(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var node = ctx.Operation.Syntax;
        if (node is null)
            return;

        if (HasJustifyingComment(node.Parent))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.EmptyStatementRequiresComment,
            ctx.Operation.Syntax.GetLocation()));
    }

    private static bool HasJustifyingComment(SyntaxNode node)
    {
        return HasCommentTrivia(node.GetLeadingTrivia()) ||
               HasCommentTrivia(node.GetTrailingTrivia());
    }

    private static bool HasCommentTrivia(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (trivia.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia))
                return true;
        }

        return false;
    }
}