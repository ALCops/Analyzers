using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class OperatorAndPlaceholderInFilterExpression : DiagnosticAnalyzer
{
    private static readonly Regex SetFilterPlaceholderRegex =
        new(@"%\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly string SetFilterMethodName = "SetFilter";
    private static readonly char[] UnsupportedOperators = ['*', '?', '@'];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.OperatorAndPlaceholderInFilterExpression
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression
        );

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (!string.Equals(operation.TargetMethod.Name, SetFilterMethodName, StringComparison.Ordinal))
            return;

        if (operation.Arguments.Length < 2)
            return;

        if (operation.Arguments[1].Value is not IOperation value)
            return;

        var navTypeKind = value.Type.GetNavTypeKindSafe();
        if (navTypeKind != EnumProvider.NavTypeKind.String &&
            navTypeKind != EnumProvider.NavTypeKind.Joker)
            return;

        if (value.Syntax is not LiteralExpressionSyntax literal)
            return;

        string filterText = literal.GetText().ToString();
        if (string.IsNullOrEmpty(filterText))
            return;

        Match match = SetFilterPlaceholderRegex.Match(filterText);
        if (!match.Success)
            return;

        int operatorIndex = filterText.IndexOfAny(UnsupportedOperators);
        if (operatorIndex < 0)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.OperatorAndPlaceholderInFilterExpression,
            literal.GetLocation(),
            filterText[operatorIndex].ToString(),
            match.Value));
    }
}