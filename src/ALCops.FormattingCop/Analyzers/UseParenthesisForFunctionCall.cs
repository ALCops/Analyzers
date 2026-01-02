using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UseParenthesisForFunctionCall : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UseParenthesisForFunctionCall
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocationExpression,
            EnumProvider.OperationKind.InvocationExpression
        );

    private void AnalyzeInvocationExpression(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression { Arguments.Length: 0 } invocation ||
            invocation.TargetMethod is not IMethodSymbol { MethodKind: MethodKind.BuiltInMethod } method)
            return;

        // Exclude using methodes like IsolationLevel::UpdLock and/or TextEncoding::Windows
        if (ctx.Operation.Syntax.Parent.IsKind(EnumProvider.SyntaxKind.OptionAccessExpression))
            return;

        if (!invocation.Syntax.GetLastToken().IsKind(EnumProvider.SyntaxKind.CloseParenToken))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseParenthesisForFunctionCall,
                invocation.Syntax.GetLocation(),
                method.Name));
        }
    }
}