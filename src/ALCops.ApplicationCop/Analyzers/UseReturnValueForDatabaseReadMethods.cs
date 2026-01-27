using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class UseReturnValueForDatabaseReadMethods : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UseReturnValueForDatabaseReadMethods);

    private static readonly ImmutableHashSet<string> DatabaseReadMethods =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Find",
            "FindFirst",
            "FindLast",
            "Get",
            "GetBySystemId");

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            invocation.Instance?.Type.OriginalDefinition.Kind != EnumProvider.SymbolKind.Table ||
            !DatabaseReadMethods.Contains(invocation.TargetMethod.Name))
            return;

        if (ctx.Operation.Syntax.Parent.Kind == EnumProvider.SyntaxKind.ExpressionStatement)
        {
            var methodName = invocation.TargetMethod.Name;
            if (invocation.Syntax is not InvocationExpressionSyntax invocationSyntax)
                return;

            var location = invocationSyntax.Expression.GetLocation();

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseReturnValueForDatabaseReadMethods,
                location,
                methodName));
        }
    }
}