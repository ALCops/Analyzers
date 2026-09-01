using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UseParenthesisForMethodAssignment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UseParenthesisForMethodAssignment);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocationExpression,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocationExpression(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        // A single-parameter method invoked using assignment syntax (target := source)
        // binds to an invocation whose syntax is an assignment statement.
        if (!invocation.Syntax.IsKind(EnumProvider.SyntaxKind.AssignmentStatement))
            return;

        // Genuine property members (MethodKind.Property) are getters with no parameters,
        // so they cannot bind to this single-parameter assignment form in practice. This guard
        // is defensive: it prevents a false positive (and an invalid code fix) should the SDK
        // ever expose a settable property, which must be invoked using assignment syntax.
        if (invocation.TargetMethod is not IMethodSymbol method ||
            method.MethodKind == EnumProvider.MethodKind.Property)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseParenthesisForMethodAssignment,
            invocation.Syntax.GetLocation(),
            method.Name));
    }
}
