using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ListObjectsAreOneBased : DiagnosticAnalyzer
{
    private const string GetMethodName = "Get";
    private const string CountMethodName = "Count";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ListObjectsAreOneBased);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            this.AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression
        );

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            invocation.TargetMethod.ContainingType?.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.List)
            return;

        switch (invocation.TargetMethod.Name)
        {
            case var name when string.Equals(name, GetMethodName, StringComparison.Ordinal):
                AnalyzeGetOperator(invocation, ctx);
                break;

            case var name when string.Equals(name, CountMethodName, StringComparison.Ordinal):
                AnalyzeCountOperator(invocation, ctx);
                break;
        }
    }

    private static void AnalyzeGetOperator(IInvocationExpression invocation, OperationAnalysisContext ctx)
    {
        if (invocation.Arguments.Length == 0)
            return;

        switch (invocation.Arguments[0].Syntax)
        {
            case LiteralExpressionSyntax literalExpressionSyntax:
                if (IsZeroLiteral(literalExpressionSyntax))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ListObjectsAreOneBased,
                        invocation.Syntax.GetLocation()));
                }
                break;
        }
    }

    private static void AnalyzeCountOperator(IInvocationExpression operation, OperationAnalysisContext ctx)
    {
        if (operation.Syntax.Parent is not ForStatementSyntax statementSyntax)
            return;

        if (statementSyntax.InitialValue is not LiteralExpressionSyntax expressionSyntax)
            return;

        if (IsZeroLiteral(expressionSyntax))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ListObjectsAreOneBased,
                expressionSyntax.Literal.GetLocation()));
        }
    }

    private static bool IsZeroLiteral(SyntaxNode node)
    {
        if (node is not LiteralExpressionSyntax literal)
            return false;

        if (literal.Literal is not Int32SignedLiteralValueSyntax intLiteral)
            return false;

        return int.TryParse(intLiteral.Number.ValueText, out var value) && value == 0;
    }
}