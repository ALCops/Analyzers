using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public class AutoCalcFieldsOnlyOnFlowFields : DiagnosticAnalyzer
{
    private const string SetAutoCalcFieldsMethodName = "SetAutoCalcFields";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.AutoCalcFieldsOnlyOnFlowFields
        );

    public override void Initialize(AnalysisContext context) =>
            context.RegisterOperationAction(
                AnalyzeInvocation,
                EnumProvider.OperationKind.InvocationExpression
            );

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationExpression invocation)
            return;

        IMethodSymbol? targetMethod = invocation.TargetMethod;
        if (targetMethod is null)
            return;

        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (!SemanticFacts.IsSameName(targetMethod.Name, SetAutoCalcFieldsMethodName))
            return;

        foreach (IArgument argument in invocation.Arguments)
        {
            IOperation? value = argument.Value;

            if (value is IConversionExpression conversion)
                value = conversion.Operand;

            if (value is not IFieldAccess fieldAccess)
                continue;

            // Allow SetAutoCalcFields on FlowFieds and BLOB fields
            if (fieldAccess.FieldSymbol.FieldClass == EnumProvider.FieldClassKind.FlowField ||
                fieldAccess.Type.NavTypeKind == EnumProvider.NavTypeKind.Blob)
                continue;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.AutoCalcFieldsOnlyOnFlowFields,
                    fieldAccess.Syntax.GetLocation(),
                    fieldAccess.FieldSymbol.Name));
        }
    }
}