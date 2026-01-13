using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PossibleOverflowAssigning : DiagnosticAnalyzer
{
    private const string GetMethodName = "Get";
    private const string SetFilterMethodName = "SetFilter";
    private const string ValidateMethodName = "Validate";
    private readonly Lazy<Regex> strSubstNoPatternLazy =
        new Lazy<Regex>(() => new Regex("[#%](\\d+)", RegexOptions.Compiled));
    private Regex StrSubstNoPattern => this.strSubstNoPatternLazy.Value;

    // Build-in methods like Database.CompanyName() and Database.UserId() have indirectly a return length
    private static readonly Dictionary<string, int> BuiltInMethodNameWithReturnLength = new()
        {
            { "CompanyName", 30 },
            { "UserId", 50 }
        };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.PossibleOverflowAssigning);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeTypeKindLabel,
            EnumProvider.OperationKind.AssignmentStatement,
            EnumProvider.OperationKind.ExitStatement);

        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);
    }

    #region AnalyzeTypeKindLabel
    // This rule is an extension of the CodeCop AA0139 to only check for Label variables without the MaxLength or Locked property explicitly set.
    // https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/analyzers/codecop-aa0139
    private void AnalyzeTypeKindLabel(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var sourceOperand = TryGetSourceOperand(ctx.Operation);
        if (sourceOperand?.Type.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Label)
            return;

        if (sourceOperand.GetSymbol() is not IVariableSymbol variableSymbol)
            return;

        if (variableSymbol.Type.GetTypeSymbol() is not ILabelTypeSymbol labelTypeSymbol)
            return;

        // Let CodeCop AA0139 handle it if MaxLength or Locked is set
        if (labelTypeSymbol.Locked is true || labelTypeSymbol.MaxLength < int.MaxValue)
            return;

        var targetTypeSymbol = TryGetTargetTypeSymbol(ctx);
        if (targetTypeSymbol is null)
            return;

        bool isError = false;
        int targetTypeLength = targetTypeSymbol.GetTypeLength(ref isError);
        if (isError || targetTypeLength == int.MaxValue)
            return;

        var labelText = labelTypeSymbol.Text;
        if (string.IsNullOrEmpty(labelText))
            return;

        // CodeCop AA0139 raises a diagnostic if the label value exceeds the target length.
        // In contrast here we're going to raises a diagnostic only when the label value is shorter than the target
        if (labelText.Length < targetTypeLength)
        {
            var properties = ImmutableDictionary<string, string>.Empty
                .Add("TargetLength", targetTypeLength.ToString())
                .Add("HasMaxLengthProperty", "true");

            ctx.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.PossibleOverflowAssigning,
                    sourceOperand.Syntax.GetLocation(),
                    properties,
#if NETSTANDARD2_1
                    variableSymbol.GetTypeSymbol().ToDisplayStringWithReflection(),
                    targetTypeSymbol.ToDisplayStringWithReflection()));
#else
                    variableSymbol.GetTypeSymbol().ToDisplayString(),
                    targetTypeSymbol.ToDisplayString()));
#endif
        }
    }

    private static IOperation? TryGetSourceOperand(IOperation operation)
    {
        IOperation? value = operation switch
        {
            IAssignmentStatement a => a.Value,
            IExitStatement e => e.ReturnedValue,
            _ => null
        };

        return value is null ? null : UnwrapConversion(value);
    }

    private static IOperation UnwrapConversion(IOperation operation)
    {
        while (operation is IConversionExpression conv && conv.Operand is not null)
            operation = conv.Operand;

        return operation;
    }

    private static ITypeSymbol? TryGetTargetTypeSymbol(OperationAnalysisContext ctx)
    {
        return ctx.Operation switch
        {
            IAssignmentStatement a => a.Target?.Type,
            IExitStatement => TryGetContainingMethodReturnType(ctx),
            _ => null
        };
    }

    private static ITypeSymbol? TryGetContainingMethodReturnType(OperationAnalysisContext ctx)
    {
        if (ctx.ContainingSymbol is not IMethodSymbol method)
            return null;

        return method.ReturnValueSymbol.GetTypeSymbol();
    }

    #endregion

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;
        if (targetMethod is null || targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        switch (targetMethod.Name)
        {
            case SetFilterMethodName:
                AnalyzeSetFilterInvocation(ctx, invocation);
                break;

            case ValidateMethodName:
                AnalyzeValidateInvocation(ctx, invocation);
                break;

            case GetMethodName:
                AnalyzeGetInvocation(ctx, invocation);
                break;
        }
    }

    private void AnalyzeSetFilterInvocation(OperationAnalysisContext ctx, IInvocationExpression invocation)
    {
        // Expect: SetFilter(Field, Filter, ...)
        if (invocation.Arguments.Length < 3)
            return;

        if (!TryUnwrapConversion(invocation.Arguments[0].Value, out var fieldOp))
            return;

        if (fieldOp.Type is not ITypeSymbol fieldType)
            return;

        if (fieldType.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Text)
            return;

        bool isError = false;
        int typeLength = fieldType.GetTypeLength(ref isError);
        if (isError || typeLength == int.MaxValue)
            return;

        foreach (int placeholderNumber in GetArgumentIndexes(invocation.Arguments[1].Value))
        {
            if (placeholderNumber < 1)
                continue;

            int argumentIndex = placeholderNumber + 1; // The placeholders are defines as %1, %2, %3, where in case of %1 we need the second (zero based) index of the arguments of the SetFilter method
            if (argumentIndex >= invocation.Arguments.Length)
                continue;

            var arg = invocation.Arguments[argumentIndex];

            if (!TryUnwrapConversion(arg.Value, out var argOperand))
                continue;

            int expressionLength = this.CalculateMaxExpressionLength(argOperand, ref isError);
            if (isError)
                continue;

            if (expressionLength > typeLength)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PossibleOverflowAssigning,
                    arg.Syntax.GetLocation(),
                    GetDisplayString(arg, invocation),
                    GetDisplayString(invocation.Arguments[0], invocation)));
            }
        }
    }

    private void AnalyzeValidateInvocation(OperationAnalysisContext ctx, IInvocationExpression invocation)
    {
        var targetMethod = invocation.TargetMethod;
        if (targetMethod is null)
            return;

        if (invocation.Arguments.Length < 2 ||
            invocation.Arguments[0].Value.Kind != EnumProvider.OperationKind.ConversionExpression)
            return;

        if (!TryUnwrapConversion(invocation.Arguments[0].Value, out var fieldOp))
            return;

        if (fieldOp.Type is not ITypeSymbol fieldType)
            return;

        bool isError = false;
        int typeLength = fieldType.GetTypeLength(ref isError);
        if (isError || typeLength == int.MaxValue)
            return;

        if (!TryUnwrapConversion(invocation.Arguments[1].Value, out var argOp))
            return;

        int expressionLength = this.CalculateMaxExpressionLength(argOp, ref isError);
        if (isError)
            return;

        if (expressionLength > typeLength)
        {
            var isLabel = argOp.Type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Label;
            var properties = ImmutableDictionary<string, string>.Empty
                .Add("TargetLength", typeLength.ToString())
                .Add("HasMaxLengthProperty", isLabel ? "true" : "false");

            ctx.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.PossibleOverflowAssigning,
                    argOp.Syntax.GetLocation(),
                    properties,
                    GetDisplayString(invocation.Arguments[1], invocation),
                    GetDisplayString(invocation.Arguments[0], invocation)));
        }
    }

    private void AnalyzeGetInvocation(OperationAnalysisContext ctx, IInvocationExpression invocation)
    {
        var targetMethod = invocation.TargetMethod;
        if (targetMethod is null)
            return;

        if (invocation.Arguments.Length < 1)
            return;

        if (invocation.Instance?.Type.GetTypeSymbol()?.OriginalDefinition is not ITableTypeSymbol table)
            return;

        if (invocation.Arguments.Length < table.PrimaryKey.Fields.Length)
            return;

        for (int index = 0; index < table.PrimaryKey.Fields.Length; index++)
        {
#if NETSTANDARD2_1
            var fieldType = table.PrimaryKey.Fields[index].OriginalDefinition.GetTypeSymbol();
#else
            var fieldType = table.PrimaryKey.Fields[index].Type;
#endif
            var argumentType = invocation.Arguments[index].GetTypeSymbol();

            if (fieldType is null || argumentType is null || argumentType.HasLength)
                continue;

            bool isError = false;
            int fieldLength = fieldType.GetTypeLength(ref isError);
            if (isError || fieldLength == 0)
                continue;

            if (invocation.Arguments[index].Value is not IConversionExpression argValue)
                continue;

            int expressionLength = this.CalculateMaxExpressionLength(argValue.Operand, ref isError);
            if (isError)
                continue;

            if (expressionLength > fieldLength)
            {
                string lengthSuffix = expressionLength < int.MaxValue
                    ? $"[{expressionLength}]"
                    : string.Empty;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PossibleOverflowAssigning,
                    invocation.Arguments[index].Syntax.GetLocation(),
#if NETSTANDARD2_1
                    $"{argumentType.ToDisplayStringWithReflection()}{lengthSuffix}",
                    fieldType.ToDisplayStringWithReflection()));
#else
                    $"{argumentType.ToDisplayString()}{lengthSuffix}",
                    fieldType.ToDisplayString()));
#endif
            }
        }
    }

    private int CalculateMaxExpressionLength(IOperation expression, ref bool isError)
    {
        if (expression.Syntax.Parent.IsKind(EnumProvider.SyntaxKind.CaseLine))
        {
            isError = true;
            return 0;
        }

        var kind = expression.Kind;
        switch (kind)
        {
            case var _ when kind == EnumProvider.OperationKind.LiteralExpression:
                if (expression.Type.IsTextType())
                    return expression.ConstantValue.Value.ToString()!.Length;
                ITypeSymbol type = expression.Type;
                if ((type is not null ? (type.NavTypeKind == EnumProvider.NavTypeKind.Char ? 1 : 0) : 0) != 0)
                    return 1;
                break;
            case var _ when kind == EnumProvider.OperationKind.ConversionExpression:
                return this.CalculateMaxExpressionLength(((IConversionExpression)expression).Operand, ref isError);
            case var _ when kind == EnumProvider.OperationKind.InvocationExpression:
                IInvocationExpression invocation = (IInvocationExpression)expression;
                IMethodSymbol targetMethod = invocation.TargetMethod;
                if (targetMethod is not null && targetMethod.ContainingSymbol?.Kind == EnumProvider.SymbolKind.Class)
                {
                    if (IsBuiltInMethodWithReturnLength(targetMethod, out int length))
                        return length;

                    switch (targetMethod.Name.ToLowerInvariant())
                    {
                        case "convertstr":
                        case "delchr":
                        case "delstr":
                        case "incstr":
                        case "lowercase":
                        case "uppercase":
                            if (invocation.Arguments.Length > 0)
                                return CalculateBuiltInMethodResultLength(invocation, 0, ref isError);
                            break;
                        case "copystr":
                            if (invocation.Arguments.Length == 3)
                                return CalculateBuiltInMethodResultLength(invocation, 2, ref isError);
                            break;
                        case "format":
                            return 0;
                        case "padstr":
                        case "substring":
                            if (invocation.Arguments.Length >= 2)
                                return CalculateBuiltInMethodResultLength(invocation, 1, ref isError);
                            break;
                        case "strsubstno":
                            if (invocation.Arguments.Length > 0)
                                return this.CalculateStrSubstNoMethodResultLength(invocation, ref isError);
                            break;
                        case "tolower":
                        case "toupper":
                            if (invocation.Instance is not null && invocation.Instance.IsBoundExpression())
                                return invocation.Instance.Type.GetTypeLength(ref isError);
                            break;
                    }
                }
                return expression.Type.GetTypeLength(ref isError);
            case var _ when kind == EnumProvider.OperationKind.LocalReferenceExpression:
            case var _ when kind == EnumProvider.OperationKind.GlobalReferenceExpression:
            case var _ when kind == EnumProvider.OperationKind.ReturnValueReferenceExpression:
            case var _ when kind == EnumProvider.OperationKind.ParameterReferenceExpression:
            case var _ when kind == EnumProvider.OperationKind.FieldAccess:
                return expression.Type.GetTypeLength(ref isError);
            case var _ when kind == EnumProvider.OperationKind.BinaryOperatorExpression:
                IBinaryOperatorExpression operatorExpression = (IBinaryOperatorExpression)expression;
                return Math.Min(int.MaxValue, this.CalculateMaxExpressionLength(operatorExpression.LeftOperand, ref isError) + this.CalculateMaxExpressionLength(operatorExpression.RightOperand, ref isError));
        }
        isError = true;
        return 0;
    }

    private static int? TryGetLength(IInvocationExpression invocation, int lengthArgPos)
    {
        if (!(SemanticFacts.GetBoundExpressionArgument(invocation, lengthArgPos) is IConversionExpression expressionArgument))
            return new int?();
        ITypeSymbol type = expressionArgument.Operand.Type;
        return type.HasLength ? new int?(type.Length) : new int?();
    }

    private static int CalculateBuiltInMethodResultLength(
      IInvocationExpression invocation,
      int lengthArgPos,
      ref bool isError)
    {
        IOperation operation = invocation.Arguments[lengthArgPos].Value;
        var kind = operation.Kind;
        switch (kind)
        {
            case var _ when kind == EnumProvider.OperationKind.LiteralExpression:
                Optional<object> constantValue = operation.ConstantValue;
                if (constantValue.HasValue)
                {
                    if (operation.Type.IsIntegralType())
                    {
                        constantValue = operation.ConstantValue;
                        return (int)constantValue.Value;
                    }
                    if (operation.Type.IsTextType())
                    {
                        constantValue = operation.ConstantValue;
                        return constantValue.Value.ToString()?.Length ?? 0;
                    }
                    break;
                }
                break;
            case var _ when kind == EnumProvider.OperationKind.InvocationExpression:
                invocation = (IInvocationExpression)operation;
                IMethodSymbol targetMethod = invocation.TargetMethod;
                if (targetMethod is not null && SemanticFacts.IsSameName(targetMethod.Name, "maxstrlen") && targetMethod.ContainingSymbol?.Kind == EnumProvider.SymbolKind.Class)
                {
                    ImmutableArray<IArgument> arguments = invocation.Arguments;
                    if (arguments.Length == 1)
                    {
                        arguments = invocation.Arguments;
                        IOperation operand = arguments[0].Value;
                        if (operand.Kind == EnumProvider.OperationKind.ConversionExpression)
                            operand = ((IConversionExpression)operand).Operand;
                        return operand.Type.GetTypeLength(ref isError);
                    }
                    break;
                }
                break;
        }
        return TryGetLength(invocation, lengthArgPos) ?? invocation.Type.GetTypeLength(ref isError);
    }

    private int CalculateStrSubstNoMethodResultLength(
      IInvocationExpression invocation,
      ref bool isError)
    {
        IOperation operation = invocation.Arguments[0].Value;
        if (!operation.Type.IsTextType())
        {
            isError = true;
            return -1;
        }
        Optional<object> constantValue = operation.ConstantValue;
        if (!constantValue.HasValue)
        {
            isError = true;
            return -1;
        }
        constantValue = operation.ConstantValue;
        string? input = constantValue.Value.ToString();
        if (input is null)
        {
            isError = true;
            return -1;
        }
        Match match = this.StrSubstNoPattern.Match(input);
        int num;
        for (num = input.Length; !isError && match.Success && num < int.MaxValue; match = match.NextMatch())
        {
            string s = match.Groups[1].Value;
            int result = 0;
            if (int.TryParse(s, out result) && 0 < result && result < invocation.Arguments.Length)
            {
                int expressionLength = this.CalculateMaxExpressionLength(invocation.Arguments[result].Value, ref isError);
                num = expressionLength == int.MaxValue ? expressionLength : num + expressionLength - s.Length - 1;
            }
        }
        return !isError ? num : -1;
    }

    private static bool TryUnwrapConversion(IOperation op, out IOperation operand)
    {
        operand = op;
        while (operand is IConversionExpression ce && ce.Operand is not null)
            operand = ce.Operand;

        return operand is not null;
    }

    private static string GetDisplayString(IArgument argument, IInvocationExpression operation)
    {
#if NETSTANDARD2_1
        return ((IConversionExpression)argument.Value).Operand.Type.ToDisplayStringWithReflection();
#else
        return ((IConversionExpression)argument.Value).Operand.Type.ToDisplayString();
#endif
    }

    private List<int> GetArgumentIndexes(IOperation operand)
    {
        List<int> results = new List<int>();

        if (operand.Syntax.Kind != EnumProvider.SyntaxKind.LiteralExpression)
            return results;

        foreach (Match match in this.StrSubstNoPattern.Matches(operand.Syntax.ToFullString()))
        {
            if (int.TryParse(match.Groups[1].Value, out int number))
                if (!results.Contains(number))
                    results.Add(number);
        }

        return results;
    }

    private static bool IsBuiltInMethodWithReturnLength(IMethodSymbol targetMethod, out int length)
    {
        length = 0;

        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return false;

        return BuiltInMethodNameWithReturnLength.TryGetValue(targetMethod.Name, out length);
    }
}