using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class SetRangeWithFilterOperators : DiagnosticAnalyzer
{
    private readonly Lazy<Regex> replacementFieldPatternLazy = new Lazy<Regex>((Func<Regex>)(() => new Regex(@"%\d+", RegexOptions.Compiled)));

    private Regex ReplacementFieldPatternLazy => this.replacementFieldPatternLazy.Value;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.SetRangeWithFilterOperators);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            this.AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            operation.TargetMethod.Name != "SetRange" ||
            operation.TargetMethod.ContainingSymbol?.Name != "Table" ||
            operation.Arguments.Length < 2)
            return;

        CheckParameter(operation.Arguments[1].Value, ref operation, ref ctx);

        if (operation.Arguments.Length == 3)
            CheckParameter(operation.Arguments[2].Value, ref operation, ref ctx);
    }

    private void CheckParameter(IOperation operand, ref IInvocationExpression operation, ref OperationAnalysisContext context)
    {
        if (operand.Type.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.String && operand.Type.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Joker)
            return;

        if (operand.Syntax.Kind != EnumProvider.SyntaxKind.LiteralExpression)
            return;

        string parameterString = operand.Syntax.ToString();

        if (ContainsFilterOperators(parameterString))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SetRangeWithFilterOperators,
                operation.Syntax.GetLocation()));

            return;
        }

        Match match = this.ReplacementFieldPatternLazy.Match(parameterString);
        if (match.Success)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SetRangeWithFilterOperators,
                operation.Syntax.GetLocation()));
        }
    }

    private static bool ContainsFilterOperators(string parameterString) =>
        parameterString.IndexOfAny(['<', '>', '*', '&', '|']) >= 0 || parameterString.Contains("..");
}