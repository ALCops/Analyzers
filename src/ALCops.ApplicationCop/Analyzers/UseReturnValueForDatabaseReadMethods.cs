using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class UseReturnValueForDatabaseReadMethods : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UseReturnValueForDatabaseReadMethods);

    private static readonly HashSet<string> DatabaseReadMethods = ["Find", "FindFirst", "FindLast", "Get", "GetBySystemId"];

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeAssignmentStatement,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeAssignmentStatement(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            operation.TargetMethod.ContainingSymbol?.Name != "Table" ||
            !DatabaseReadMethods.Contains(operation.TargetMethod.Name))
            return;

        if (ctx.Operation.Syntax.Parent.Kind == SyntaxKind.ExpressionStatement)
        {
            var methodName = operation.TargetMethod.Name.ToString();
            var node = operation.Syntax.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .FirstOrDefault(node => string.Equals(node.Identifier.ValueText, methodName, StringComparison.OrdinalIgnoreCase));
            if (node is null)
                return;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseReturnValueForDatabaseReadMethods,
                node.GetLocation(),
                methodName));
        }
    }
}