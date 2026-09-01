using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class BuiltInDateTimeMethod : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.BuiltInDateTimeMethod);

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Fall2024OrGreater;

    public override void Initialize(AnalysisContext context) =>
          context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            operation.Arguments.Length < 1)
            return;

        if (operation.Syntax is not InvocationExpressionSyntax invocationExpression)
            return;

        if (IsFieldReferenceValueAccess(operation.Arguments[0]))
            return;

        if (IsVariantOrJokerArgument(operation.Arguments[0]))
            return;

        InvocationExpressionSyntax? replacementMethod = GetReplacementMethod(operation.TargetMethod.Name, invocationExpression);
        if (replacementMethod is null)
            return;

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("ReplacementMethodName", replacementMethod.Expression.ToString());

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.BuiltInDateTimeMethod,
                invocationExpression.GetLocation(),
                properties,
                operation.Arguments[0].Value.Syntax.ToString(),
                replacementMethod.ToString()));
    }

    private static InvocationExpressionSyntax? GetReplacementMethod(string methodName, InvocationExpressionSyntax invocationExpression)
    {
        var arguments = invocationExpression.ArgumentList.Arguments;

        return methodName switch
        {
            "Date2DMY" when arguments.Count == 2 => GetDate2DMYReplacement(arguments[1]),
            "Date2DWY" when arguments.Count == 2 => GetDate2DWYReplacement(arguments[1]),
            "DT2Date" => GetInvocationExpressionSyntax("Date"),
            "DT2Time" => GetInvocationExpressionSyntax("Time"),
            "Format" when arguments.Count == 3 => GetFormatReplacement(arguments[2]),
            _ => null
        };
    }

    private static InvocationExpressionSyntax? GetDate2DMYReplacement(CodeExpressionSyntax argument)
    {
        return GetIntegerValueOrNull(argument) switch
        {
            1 => GetInvocationExpressionSyntax("Day"),
            2 => GetInvocationExpressionSyntax("Month"),
            3 => GetInvocationExpressionSyntax("Year"),
            _ => null
        };
    }

    private static InvocationExpressionSyntax? GetDate2DWYReplacement(CodeExpressionSyntax argument)
    {
        return GetIntegerValueOrNull(argument) switch
        {
            1 => GetInvocationExpressionSyntax("DayOfWeek"),
            2 => GetInvocationExpressionSyntax("WeekNo"),
            3 => null, // Year() isn't returning the same value. When the input date to the Date2DWY method is in a week that spans two years, the Date2DWY method computes the output year as the year that has the most days.
            _ => null
        };
    }

    private static InvocationExpressionSyntax? GetFormatReplacement(CodeExpressionSyntax argument)
    {
        return argument.GetIdentifierOrLiteralValue() switch
        {
            "<HOURS24>" => GetInvocationExpressionSyntax("Hour"),
            "<MINUTES>" => GetInvocationExpressionSyntax("Minute"),
            "<SECONDS>" => GetInvocationExpressionSyntax("Second"),
            "<THOUSANDS>" => GetInvocationExpressionSyntax("Millisecond"),
            _ => null
        };
    }

    private static int? GetIntegerValueOrNull(CodeExpressionSyntax argument)
        => int.TryParse(argument.GetIdentifierOrLiteralValue(), out var value) ? value : null;

    private static InvocationExpressionSyntax GetInvocationExpressionSyntax(string identifier)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(identifier),
            SyntaxFactory.ArgumentList());
    }

    private static bool IsVariantOrJokerArgument(IArgument argument)
    {
        IOperation value = argument.Value;

        if (value is IConversionExpression conversion)
            value = conversion.Operand;

        var navTypeKind =
            value.Type?.GetNavTypeKindSafe()
            ?? value.GetSymbol()?.OriginalDefinition.GetTypeSymbol().GetNavTypeKindSafe()
            ?? EnumProvider.NavTypeKind.None;

        return navTypeKind == EnumProvider.NavTypeKind.Variant
            || navTypeKind == EnumProvider.NavTypeKind.Joker;
    }

    private static bool IsFieldReferenceValueAccess(IArgument argument)
    {
        IOperation value = argument.Value;

        if (value is IConversionExpression conversion)
            value = conversion.Operand;

        // The AL SDK currently models `FieldRef.Value` as an IInvocationExpression
        // (getter call). Guarding IFieldAccess as well defends against future SDK
        // versions that could remodel the same construct as a field/property access.
        var (instance, memberName) = value switch
        {
            IInvocationExpression invocation => (invocation.Instance, invocation.TargetMethod?.Name),
            IFieldAccess fieldAccess => (fieldAccess.Instance, fieldAccess.FieldSymbol?.Name),
            _ => ((IOperation?)null, (string?)null)
        };

        if (!memberName.IsSameName("Value"))
            return false;

        return instance?.Type?.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.FieldRef;
    }
}