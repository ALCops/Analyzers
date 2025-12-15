using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.LinterCop.Analyzer;

[DiagnosticAnalyzer]
public class RecordInstanceIsolationLevel : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.RecordInstanceIsolationLevel);

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Spring2023OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(new Action<OperationAnalysisContext>(this.CheckLockTable), EnumProvider.OperationKind.InvocationExpression);

    private void CheckLockTable(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            operation.TargetMethod.Name != "LockTable")
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.RecordInstanceIsolationLevel,
            ctx.Operation.Syntax.GetLocation()));
    }
}