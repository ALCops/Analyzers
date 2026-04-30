using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

/// <summary>
/// Reports when the number of substitution arguments does not match the number of
/// unique placeholders in the format string of StrSubstNo, Error, Message, or Confirm.
/// This rule extends CodeCop AA0131 to cover its gaps:
/// - Zero substitution arguments when placeholders exist (AA0131 exits early)
/// - The Confirm method (AA0131 does not handle it)
/// </summary>
[DiagnosticAnalyzer]
public sealed class PlaceholderArgumentCountMismatch : DiagnosticAnalyzer
{
    private static readonly Regex PlaceholderPattern = new("[#%](\\d+)", RegexOptions.Compiled);

    private static readonly HashSet<string> MethodsCoveredByAA0131 = new(StringComparer.OrdinalIgnoreCase)
    {
        "StrSubstNo",
        "Message",
        "Error"
    };

    private static readonly HashSet<string> AllTargetMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "StrSubstNo",
        "Message",
        "Error",
        "Confirm"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PlaceholderArgumentCountMismatch);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private static void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        IMethodSymbol targetMethod = invocation.TargetMethod;
        if (targetMethod is null ||
            targetMethod.ContainingSymbol?.Kind != EnumProvider.SymbolKind.Class ||
            invocation.Arguments.IsEmpty)
            return;

        if (!AllTargetMethods.Contains(targetMethod.Name))
            return;

        string? formatString = GetFormatStringValue(invocation.Arguments[0].Value);
        if (string.IsNullOrEmpty(formatString))
            return;

        int placeholderCount = CountUniquePlaceholders(formatString);
        int argumentCount = GetSubstitutionArgumentCount(targetMethod.Name, invocation.Arguments.Length);

        if (placeholderCount == argumentCount)
            return;

        // For methods covered by AA0131, only report the gap it misses (zero args with placeholders).
        // AA0131 already handles the case where argumentCount >= 1.
        if (MethodsCoveredByAA0131.Contains(targetMethod.Name) && argumentCount >= 1)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PlaceholderArgumentCountMismatch,
            invocation.Arguments[0].Syntax.GetLocation(),
            placeholderCount,
            argumentCount));
    }

    /// <summary>
    /// Gets the number of substitution arguments (excluding the format string and,
    /// for Confirm, the optional default button parameter).
    /// Confirm signature: Confirm(String [, Default: Boolean] [, Value1] ...)
    /// Others: Method(String [, Value1] [, Value2] ...)
    /// </summary>
    private static int GetSubstitutionArgumentCount(string methodName, int totalArguments)
    {
        if (string.Equals(methodName, "Confirm", StringComparison.OrdinalIgnoreCase))
        {
            // Confirm: args[0]=format, args[1]=default button (Boolean, required if subs present)
            // If only 1 arg (format only) or 2 args (format + default): 0 subs
            return totalArguments <= 2 ? 0 : totalArguments - 2;
        }

        // StrSubstNo, Error, Message: args[0]=format, args[1..N]=substitution values
        return totalArguments - 1;
    }

    private static string? GetFormatStringValue(IOperation operation)
    {
        // Unwrap conversion expressions, bail out if we hit a Text variable
        while (operation.Kind == EnumProvider.OperationKind.ConversionExpression)
        {
            if (operation is not IConversionExpression conversion)
                break;

            operation = conversion.Operand;
            if (operation.Type?.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Text)
                return null;
        }

        if (operation.Type is null || !operation.Type.IsTextType())
            return null;

        switch (operation.Type.GetNavTypeKindSafe())
        {
            case var kind when kind == EnumProvider.NavTypeKind.Label:
                if (operation.Type is ILabelTypeSymbol labelTypeSymbol)
                    return labelTypeSymbol.Text;
                break;

            case var kind when kind == EnumProvider.NavTypeKind.String:
                if (operation.ConstantValue.HasValue)
                    return operation.ConstantValue.Value?.ToString();
                break;
        }

        return null;
    }

    private static int CountUniquePlaceholders(string formatString)
    {
        MatchCollection matches = PlaceholderPattern.Matches(formatString);
        if (matches.Count == 0)
            return 0;

        HashSet<string> unique = new();
        foreach (Match match in matches)
            unique.Add(match.Value);

        return unique.Count;
    }
}
