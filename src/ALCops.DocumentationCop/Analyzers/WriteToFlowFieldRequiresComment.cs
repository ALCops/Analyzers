using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public class WriteToFlowFieldRequiresComment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.WriteToFlowFieldRequiresComment);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(new Action<OperationAnalysisContext>(this.AnalyzeAssignmentStatement),
            EnumProvider.OperationKind.AssignmentStatement);

        context.RegisterOperationAction(new Action<OperationAnalysisContext>(this.AnalyzeInvocationExpression),
            EnumProvider.OperationKind.InvocationExpression);
    }

    private void AnalyzeAssignmentStatement(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IAssignmentStatement operation)
            return;

        if (operation.Target.Kind != EnumProvider.OperationKind.FieldAccess)
            return;

        var fieldSymbol = ExtractFieldSymbolFromAssignment(operation);
        if (fieldSymbol?.FieldClass != EnumProvider.FieldClassKind.FlowField || HasExplainingComment(operation))
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.WriteToFlowFieldRequiresComment,
            operation.Target.Syntax.GetLocation()));
    }

    private static IFieldSymbol? ExtractFieldSymbolFromAssignment(IAssignmentStatement operation)
    {
        if (operation.Target.Syntax.Kind == SyntaxKind.ArrayIndexExpression &&
            operation.Target is ITextIndexAccess textIndexAccess &&
            textIndexAccess.TextExpression is IFieldAccess fieldAccess)
        {
            return fieldAccess.FieldSymbol;
        }

        return operation.Target is IFieldAccess directFieldAccess ? directFieldAccess.FieldSymbol : null;
    }

    private void AnalyzeInvocationExpression(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IInvocationExpression operation)
            return;

        var targetMethod = operation.TargetMethod;
        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (!string.Equals(targetMethod.Name, "Validate", StringComparison.Ordinal))
            return;

        var instance = operation.Instance;
        if (instance?.Type.NavTypeKind != EnumProvider.NavTypeKind.Record)
            return;

        if (operation.Arguments.Length == 0)
            return;

        if (operation.Arguments[0].Value is not IConversionExpression { Operand: IFieldAccess fieldAccess })
            return;

        if (fieldAccess.FieldSymbol.FieldClass != EnumProvider.FieldClassKind.FlowField)
            return;

        if (HasExplainingComment(operation))
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.WriteToFlowFieldRequiresComment,
            operation.Arguments[0].Value.Syntax.GetLocation()));
    }

    private static bool HasExplainingComment(IOperation operation)
    {
        // Check for comment before the statement (leading trivia)
        if (operation.Syntax.GetLeadingTrivia()
            .Any(t => t.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia)))
            return true;

        // Check for comment at end of statement (trailing trivia)
        var statement = operation.Syntax
            .AncestorsAndSelf()
            .OfType<StatementSyntax>()
            .FirstOrDefault();

        if (statement is null)
            return false;

        return statement
            .GetLastToken()
            .TrailingTrivia
            .Any(t => t.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia));
    }
}