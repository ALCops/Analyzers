using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CommitRequiresComment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.CommitRequiresCommen);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            this.AnalyzeCommitHasComment,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeCommitHasComment(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            operation.TargetMethod.Name != "Commit")
            return;

        var parentSyntax = operation.Syntax.Parent;
        if (HasLineComment(parentSyntax.GetLeadingTrivia()) || HasLineComment(parentSyntax.GetTrailingTrivia()))
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CommitRequiresCommen,
                ctx.Operation.Syntax.GetLocation()));
    }
    private static bool HasLineComment(SyntaxTriviaList triviaList)
    {
        return triviaList.Any(trivia => trivia.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia));
    }
}
