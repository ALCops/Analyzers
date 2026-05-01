using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class JsonTokenJPathUsesDoubleQuotes : DiagnosticAnalyzer
{
    private static readonly HashSet<string> MethodNames = new(StringComparer.Ordinal)
    {
        "SelectToken",
        "SelectTokens"
    };

    private static readonly HashSet<string> JsonTypeNames = new(StringComparer.Ordinal)
    {
        "JsonToken",
        "JsonObject",
        "JsonArray"
    };

    private const string PathParameterName = "Path";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.JsonTokenJPathUsesDoubleQuotes
    );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeSelectToken,
            EnumProvider.OperationKind.InvocationExpression
        );

    private void AnalyzeSelectToken(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        IMethodSymbol method = invocation.TargetMethod;

        // Cheapest gates first
        if (method.MethodKind != MethodKind.BuiltInMethod)
            return;

        if (!MethodNames.Contains(method.Name))
            return;

        if (method.ContainingSymbol?.Name is not string containingName || !JsonTypeNames.Contains(containingName))
            return;

        List<StringLiteralValueSyntax>? stringLiterals = null;
        foreach (var argument in invocation.Arguments)
        {
            if (!string.Equals(argument.Parameter?.Name, PathParameterName, StringComparison.Ordinal))
                continue;

            if (argument.Syntax is not LiteralExpressionSyntax { Literal: StringLiteralValueSyntax literal })
                continue;

            var valueText = literal.Value.ValueText;
            if (valueText is null || valueText.IndexOf('"') < 0)
                continue;

            stringLiterals ??= new List<StringLiteralValueSyntax>(capacity: 1);
            stringLiterals.Add(literal);
        }

        if (stringLiterals is null)
            return;

        foreach (var stringLiteral in stringLiterals)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.JsonTokenJPathUsesDoubleQuotes,
                stringLiteral.GetLocation()));
        }
    }
}