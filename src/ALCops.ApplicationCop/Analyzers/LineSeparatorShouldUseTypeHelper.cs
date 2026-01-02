using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class LineSeparatorShouldUseTypeHelper : DiagnosticAnalyzer
{
    private const int LfAscii = 10;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.LineSeparatorShouldUseTypeHelper
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeAssignmentStatement,
            EnumProvider.OperationKind.AssignmentStatement);

    private static void AnalyzeAssignmentStatement(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IAssignmentStatement assignment)
            return;

        if (!TryGetIntLiteralValue(assignment.Value, out int rhsValue) || rhsValue != LfAscii)
            return;

        if (!IsValidLfSeparatorTarget(assignment.Target))
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.LineSeparatorShouldUseTypeHelper,
                ctx.Operation.Syntax.GetLocation()));
    }

    private static bool IsValidLfSeparatorTarget(IOperation targetOperation)
    {
        // Code[1], Code[2], Text[1], Text[2]
        if (targetOperation.Kind == EnumProvider.OperationKind.FieldAccess &&
            IsValidTextOrCodeArrayAccess(targetOperation.Syntax))
        {
            return true;
        }

        // Char variable
        if (targetOperation.Kind == EnumProvider.OperationKind.LocalReferenceExpression ||
            targetOperation.Kind == EnumProvider.OperationKind.GlobalReferenceExpression)
        {
            return IsCharVariable(targetOperation);
        }

        return false;
    }

    private static bool IsValidTextOrCodeArrayAccess(SyntaxNode? targetSyntax)
    {
        if (targetSyntax is not ElementAccessExpressionSyntax elementAccess)
            return false;

        var argumentList = elementAccess.ArgumentList;
        if (argumentList is null)
            return false;

        var args = argumentList.Arguments;

        if (args.Count != 1)
            return false;

        if (args[0] is not LiteralExpressionSyntax indexLiteral)
            return false;

        if (indexLiteral.Literal is not Int32SignedLiteralValueSyntax indexInt)
            return false;

        var indexValue = indexInt.GetIdentifierOrLiteralValue();
        return indexValue == "1" || indexValue == "2";
    }

    private static bool IsCharVariable(IOperation targetOperation)
    {
        if (targetOperation.GetSymbol() is not IVariableSymbol variableSymbol)
            return false;

        return variableSymbol
            .GetTypeSymbol()
            .GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Char;
    }

    private static bool TryGetIntLiteralValue(IOperation operation, out int value)
    {
        value = default;

        return operation.Syntax is LiteralExpressionSyntax literalExpr &&
               TryGetIntFromLiteralExpression(literalExpr, out value);
    }

    private static bool TryGetIntFromLiteralExpression(LiteralExpressionSyntax literalExpr, out int value)
    {
        value = default;

        if (literalExpr.Literal is not Int32SignedLiteralValueSyntax intLiteral)
            return false;

        return int.TryParse(intLiteral.GetIdentifierOrLiteralValue(), out value);
    }
}