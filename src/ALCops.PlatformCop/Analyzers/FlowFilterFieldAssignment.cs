using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public class FlowFilterFieldAssignment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.FlowFilterFieldAssignment
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeAssignmentOperation,
            EnumProvider.OperationKind.AssignmentStatement
        );

    private static void AnalyzeAssignmentOperation(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IAssignmentStatement assignment)
            return;

        if (assignment.Target is not IFieldAccess fieldAccess)
            return;

        var fieldSymbol = fieldAccess.FieldSymbol;
        if (fieldSymbol.FieldClass != EnumProvider.FieldClassKind.FlowFilter)
            return;

        // Use  the identifier of the member access (otherwise the target syntax)
        var location = fieldAccess.Syntax?.GetIdentifierNameSyntax()?.GetLocation()
                       ?? fieldAccess.Syntax?.GetLocation();

        if (location is null)
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.FlowFilterFieldAssignment,
                location,
                fieldSymbol.Name));
    }
}