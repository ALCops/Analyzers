using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class MixedExitAndNamedReturnAssignment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MixedExitAndNamedReturnAssignment);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(AnalyzeCodeBlock);

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext ctx)
    {
        if (ctx.IsObsolete() ||
            ctx.CodeBlock is not MethodOrTriggerDeclarationSyntax declarationSyntax ||
            declarationSyntax.Body is null)
        {
            return;
        }

        if (declarationSyntax.ReturnValue is null)
        {
            return;
        }

        if (declarationSyntax is MethodDeclarationSyntax methodSyntax && methodSyntax.IsTryFunction())
        {
            return;
        }

        if (ctx.OwningSymbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.ReturnValueSymbol is not { IsNamed: true } returnValue)
        {
            return;
        }

        var operation = ctx.SemanticModel.GetOperation(declarationSyntax.Body, ctx.CancellationToken);

        if (operation is null)
        {
            return;
        }

        var walker = new ReturnUsageWalker(returnValue.Name);
        walker.Visit(operation);

        if (!walker.HasNamedReturnAssignment || walker.ExitLocations.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var location in walker.ExitLocations)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MixedExitAndNamedReturnAssignment,
                location,
                GetDeclarationKind(declarationSyntax),
                methodSymbol.GetDiagnosticDisplayText(MethodSymbolDisplayFormat.MethodSignature)));
        }
    }

    private static string GetDeclarationKind(MethodOrTriggerDeclarationSyntax declarationSyntax) =>
        declarationSyntax is TriggerDeclarationSyntax ? "Trigger" : "Procedure";

    private sealed class ReturnUsageWalker(string returnVariableName) : OperationWalker
    {
        private readonly string _returnVariableName = returnVariableName;
        private readonly List<Location> _exitLocations = [];

        public bool HasNamedReturnAssignment { get; private set; }

        public ImmutableArray<Location> ExitLocations => _exitLocations.ToImmutableArray();

        public override void VisitExitStatement(IExitStatement operation)
        {
            _exitLocations.Add(operation.Syntax.GetLocation());
            base.VisitExitStatement(operation);
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            if (operation.Target.IsNamedReturnTarget(_returnVariableName))
            {
                HasNamedReturnAssignment = true;
            }

            base.VisitAssignmentStatement(operation);
        }
    }
}
