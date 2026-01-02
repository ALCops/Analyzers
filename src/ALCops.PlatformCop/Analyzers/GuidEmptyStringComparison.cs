using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public class GuidEmptyStringComparison : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.GuidEmptyStringComparison
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeBinaryOperatorExpression,
            EnumProvider.OperationKind.BinaryOperatorExpression);

    private void AnalyzeBinaryOperatorExpression(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IBinaryOperatorExpression operation)
            return;

        var leftIsGuid = TryGetGuidOperand(operation.LeftOperand, out var leftGuidOperand);
        var rightIsGuid = TryGetGuidOperand(operation.RightOperand, out var rightGuidOperand);

        var leftIsEmpty = IsEmptyStringLiteral(operation.LeftOperand);
        var rightIsEmpty = IsEmptyStringLiteral(operation.RightOperand);

        IOperation? guidOperand = null;

        if (leftIsGuid && rightIsEmpty)
        {
            guidOperand = leftGuidOperand;
        }
        else if (rightIsGuid && leftIsEmpty)
        {
            guidOperand = rightGuidOperand;
        }

        if (guidOperand is null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.GuidEmptyStringComparison,
            operation.Syntax.GetLocation(),
            GetVariableName(guidOperand),
            "''"));
    }

    private static bool TryGetGuidOperand(IOperation operand, out IOperation? guidOperand)
    {
        guidOperand = null;

        if (operand.Kind == EnumProvider.OperationKind.ConversionExpression &&
            operand is IConversionExpression conversion &&
            conversion.Operand is IOperation innerOperation &&
            innerOperation.Type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Guid)
        {
            guidOperand = innerOperation;
            return true;
        }

        return false;
    }

    private static bool IsEmptyStringLiteral(IOperation operand)
    {
        if (operand.Kind == EnumProvider.OperationKind.LiteralExpression &&
            operand.Type.IsTextType())
        {
            var constantValue = operand.ConstantValue.Value?.ToString();
            return string.IsNullOrEmpty(constantValue);
        }

        return false;
    }

    private static string GetVariableName(IOperation guidOperand)
    {
        return guidOperand.GetSymbol()?.Name ?? string.Empty;
    }
}