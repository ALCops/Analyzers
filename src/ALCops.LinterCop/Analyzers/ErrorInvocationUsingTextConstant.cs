using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ErrorInvocationUsingTextConstant : DiagnosticAnalyzer
{
    private const string ErrorMethodName = "Error";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ErrorInvocationUsingTextConstant
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeErrorMethod,
            EnumProvider.OperationKind.InvocationExpression
        );

    private void AnalyzeErrorMethod(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (!IsBuiltInErrorInvocation(invocation))
            return;

        if (invocation.Arguments[0].Value is not IOperation firstArgValue)
            return;

        // Allowed: Error(ErrorInfo)
        if (IsErrorInfo(firstArgValue))
            return;

        // Allowed: Error(Label)
        if (IsLabel(firstArgValue))
            return;

        // Allowed: Error('')
        if (IsEmptyStringLiteral(invocation.Arguments[0].Syntax))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ErrorInvocationUsingTextConstant,
            ctx.Operation.Syntax.GetLocation()));
    }

    private static bool IsBuiltInErrorInvocation(IInvocationExpression invocation)
    {
        if (invocation.TargetMethod is not IMethodSymbol targetMethod)
            return false;

        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return false;

        if (!string.Equals(targetMethod.Name, ErrorMethodName, StringComparison.Ordinal))
            return false;

        return invocation.Arguments.Length > 0;
    }

    private static bool IsErrorInfo(IOperation value)
    {
        if (value.Type is not ITypeSymbol type)
            return false;

        return type is not null && type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.ErrorInfo;
    }

    private static bool IsLabel(IOperation value)
    {
        if (IsLabelType(value.Type))
            return true;

        if (value.Kind != EnumProvider.OperationKind.ConversionExpression)
            return false;

        if (value is not IConversionExpression conversion)
            return false;

        var operand = conversion.Operand;

        var symbol = operand.GetSymbol();
        if (symbol is not null)
        {
            var symbolType = symbol.OriginalDefinition.GetTypeSymbol();
            return IsLabelType(symbolType);
        }

        return IsLabelType(operand.Type);
    }

    private static bool IsLabelType(ITypeSymbol? type) =>
        type is not null && type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Label;

    private static bool IsEmptyStringLiteral(SyntaxNode syntax)
    {
        if (syntax.Kind != EnumProvider.SyntaxKind.LiteralExpression)
            return false;

        string? value = syntax.GetIdentifierOrLiteralValue();
        return value?.Length == 0;
    }
}