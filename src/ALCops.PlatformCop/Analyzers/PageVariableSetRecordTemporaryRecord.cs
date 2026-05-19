using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PageVariableSetRecordTemporaryRecord : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PageVariableSetRecordTemporaryRecord);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;
        if (targetMethod is null || targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (!SemanticFacts.IsSameName(targetMethod.Name, "SetRecord"))
            return;

        if (targetMethod.ContainingType?.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
            return;

        if (invocation.Arguments.Length != 1)
            return;

        var recordArgument = invocation.Arguments[0];
        if (recordArgument.Value is not IConversionExpression conversion)
            return;

        var operand = conversion.Operand;
        if (operand?.Type is not IRecordTypeSymbol recordType)
            return;

        if (!recordType.Temporary)
            return;

        var pageVariableName = ResolvePageVariableName(invocation);

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PageVariableSetRecordTemporaryRecord,
            ctx.Operation.Syntax.GetLocation(),
            pageVariableName));
    }

    private static string ResolvePageVariableName(IInvocationExpression invocation)
    {
        foreach (var op in invocation.DescendantsAndSelf())
        {
            if (op.Type is null || op.Type.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
                continue;

            var symbol = op.GetSymbol();
            if (symbol is null)
                continue;

            return symbol.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}
