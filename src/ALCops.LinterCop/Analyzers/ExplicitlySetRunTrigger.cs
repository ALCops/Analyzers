using System.Collections.Immutable;
using ALCops.Common;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ExplicitlySetRunTrigger : DiagnosticAnalyzer
{
    private const string RunTriggerParameterName = "RunTrigger";
    private static readonly ImmutableHashSet<string> BuiltInMethodNames =
        RecordMethodClassification.RunTriggerParameterMethods;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ExplicitlySetRunTrigger);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeRunTriggerParameters,
            EnumProvider.OperationKind.InvocationExpression
        );

    private void AnalyzeRunTriggerParameters(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;
        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod || !BuiltInMethodNames.Contains(targetMethod.Name))
            return;

        var navTypeKind = invocation.Instance?.GetSymbol()?.GetTypeSymbol().GetNavTypeKindSafe();
        if (navTypeKind is null || navTypeKind != EnumProvider.NavTypeKind.Record)
            return;

        foreach (var arg in invocation.Arguments)
        {
            if (SemanticFacts.IsSameName(arg.Parameter.Name, RunTriggerParameterName))
                return;
        }

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ExplicitlySetRunTrigger,
            GetInvocationNameLocation(invocation),
            targetMethod.Name));
    }

    private static Location GetInvocationNameLocation(IInvocationExpression invocation)
    {
        var syntax = invocation.Syntax;

        if (syntax is InvocationExpressionSyntax invocationSyntax)
            syntax = invocationSyntax.Expression;

        if (syntax is MemberAccessExpressionSyntax memberAccess)
            return memberAccess.Name.GetLocation();

        return invocation.Syntax.GetLocation();
    }
}