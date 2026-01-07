using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class FilterStringSingleQuoteEscaping : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.FilterStringSingleQuoteEscaping);

    private static readonly string InvalidNotEmptyFilterLiteralTokenText = "'<>'''";

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocationExpression,
            EnumProvider.OperationKind.InvocationExpression);

        context.RegisterSymbolAction(
            AnalyzeFieldSymbolForCalcFormula,
            EnumProvider.SymbolKind.Field);
    }

    private static void AnalyzeInvocationExpression(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;
        if (targetMethod is null)
            return;

        foreach (var arg in invocation.Arguments)
        {
            var expr = arg?.Value;
            if (expr is null)
                continue;

            if (!TryGetStringLiteralTokenText(expr.Syntax, out var tokenText))
                continue;

            if (!IsInvalidNotEmptyFilterLiteral(tokenText))
                continue;

            var location = expr.Syntax?.GetLocation() ?? invocation.Syntax?.GetLocation();
            if (location is null)
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FilterStringSingleQuoteEscaping,
                location,
                tokenText));
        }
    }

    private static void AnalyzeFieldSymbolForCalcFormula(SymbolAnalysisContext ctx)
    {
        var declaringSyntax = ctx.Symbol.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken);
        if (declaringSyntax is not FieldSyntax fieldSyntax)
            return;

        var calcFormulaValue = fieldSyntax.PropertyList?.Properties
            .OfType<PropertySyntax>()
            .Select(p => p.Value)
            .OfType<CalculationFormulaPropertyValueSyntax>()
            .FirstOrDefault();
        if (calcFormulaValue is null)
            return;

        var conditions = calcFormulaValue.WhereExpression?.Filter?.Conditions;
        if (conditions is null)
            return;

        foreach (var condition in conditions.OfType<FilterExpressionSyntax>())
        {
            var filterNode = condition.Filter;
            if (filterNode is null)
                continue;

            var filterText = filterNode.ToString();
            if (!IsInvalidNotEmptyFilterLiteral(filterText))
                continue;

            var location = filterNode.GetLocation() ?? condition.GetLocation() ?? fieldSyntax.GetLocation();
            if (location is null)
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FilterStringSingleQuoteEscaping,
                location,
                filterText));
        }
    }

    private static bool IsInvalidNotEmptyFilterLiteral(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Match exact raw literal token text, e.g. '<>'''
        // In CalcFormula paths we compare node.ToString() which typically returns the same raw representation.
        return string.Equals(text, InvalidNotEmptyFilterLiteralTokenText, StringComparison.Ordinal);
    }

    private static bool TryGetStringLiteralTokenText(SyntaxNode? node, out string tokenText)
    {
        tokenText = string.Empty;

        if (node is null)
            return false;

        // Most common: string literal expression node
        if (node is LiteralExpressionSyntax stringLiteral)
        {
            tokenText = stringLiteral.ToString();
            return true;
        }

        // Some trees wrap literals (parentheses, unary nodes, etc.)
        // Try a conservative fallback: find a descendant StringLiteralExpressionSyntax.
        var descendantLiteral = node.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .FirstOrDefault();

        if (descendantLiteral is null)
            return false;

        tokenText = descendantLiteral.ToString();
        return true;
    }
}