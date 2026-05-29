using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public class UseValidateForFieldAssignment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UseValidateForFieldAssignment
        );

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeAssignmentOperation,
            EnumProvider.OperationKind.AssignmentStatement
        );

        if (EnumProvider.OperationKind.CompoundAssignmentStatement != default)
        {
            context.RegisterOperationAction(
                AnalyzeAssignmentOperation,
                EnumProvider.OperationKind.CompoundAssignmentStatement
            );
        }
    }

    private static void AnalyzeAssignmentOperation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IAssignmentStatement assignment)
            return;

        if (assignment.Target is not IFieldAccess fieldAccess)
            return;

        if (fieldAccess.Instance?.Type is not IRecordTypeSymbol recordType)
            return;

        if (recordType.Temporary)
            return;

        var location = fieldAccess.Syntax?.GetIdentifierNameSyntax()?.GetLocation()
                       ?? fieldAccess.Syntax?.GetLocation();

        if (location is null)
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.UseValidateForFieldAssignment,
                location,
                fieldAccess.FieldSymbol.Name));
    }
}
