using System.Collections.Immutable;
using ALCops.Common;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TemporaryRecordTriggerInvocation : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> MethodsThatMayRunTriggers =
        RecordMethodClassification.TriggerMethods;
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TemporaryRecordTriggerInvocation);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeTemporaryRecords,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeTemporaryRecords(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.Instance?.Type is not IRecordTypeSymbol recordType)
            return;

        if (!recordType.Temporary)
            return;

        if (recordType.BaseTable is not null &&
            recordType.BaseTable.TableType == EnumProvider.TableTypeKind.Temporary)
            return;

        var method = invocation.TargetMethod;
        if (method.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            !MethodsThatMayRunTriggers.Contains(method.Name))
            return;

        bool isExecutingTriggersOrValidation = invocation.TargetMethod.Name switch
        {
            "Validate" => true,
            "ModifyAll" => invocation.Arguments.Length == 3 && IsRunTriggerEnabled(invocation.Arguments[2]),
            _ => invocation.Arguments.Length == 1 && IsRunTriggerEnabled(invocation.Arguments[0])
        };

        if (isExecutingTriggersOrValidation)
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TemporaryRecordTriggerInvocation,
                ctx.Operation.Syntax.GetLocation()));
    }

    private static bool IsRunTriggerEnabled(IArgument argument) =>
        argument.Value.ConstantValue.HasValue &&
        argument.Value.ConstantValue.Value is bool isEnabled &&
        isEnabled;
}