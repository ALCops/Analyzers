using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public class UseSecretTextForSensitiveText : DiagnosticAnalyzer
{
    private const string AuthorizationHeaderName = "Authorization";

    private static readonly HashSet<string> HttpHeadersMethodNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Add",
            "GetValues",
            "TryAddWithoutValidation"
        };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UseSecretTextForSensitiveText
        );

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Fall2023OrGreater;

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocationExpression,
            EnumProvider.OperationKind.InvocationExpression
        );
    }


    private static void AnalyzeInvocationExpression(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        IMethodSymbol? targetMethod = invocation.TargetMethod;
        if (targetMethod is null)
            return;

        if (AnalyzeIsolatedStorage(ctx, invocation, targetMethod))
            return;

        var kind = targetMethod.MethodKind;
        switch (kind)
        {
            case var _ when kind == EnumProvider.MethodKind.BuiltInMethod:
                AnalyzeHttpAuthorizationHeader(ctx, invocation, targetMethod);
                break;

            case var _ when kind == EnumProvider.MethodKind.Method:
                AnalyzeRestClientAuthorizationHeader(ctx, invocation, targetMethod);
                break;
        }
    }

    private static bool AnalyzeIsolatedStorage(
        OperationAnalysisContext ctx,
        IInvocationExpression invocation,
        IMethodSymbol targetMethod)
    {
        if (!VersionChecker.IsSupported(ctx.ContainingSymbol, VersionProvider.VersionCompatibility.Spring2024OrGreater))
            return false;

        if (targetMethod.ContainingSymbol?.Kind != EnumProvider.SymbolKind.Class)
            return false;

        if (!SemanticFacts.IsSameName(targetMethod.ContainingSymbol.Name, "IsolatedStorage"))
            return false;

        // Only some methods have a value argument that should be SecretText.
        // - Get: var parameter
        // - Set/SetEncrypted: second argument (index 1)
        IArgument? valueArgument = targetMethod.Name switch
        {
            var n when SemanticFacts.IsSameName(n, "Get")
                => invocation.Arguments.FirstOrDefault(a => a.Parameter.IsVar),

            var n when (SemanticFacts.IsSameName(n, "Set") || SemanticFacts.IsSameName(n, "SetEncrypted")) && invocation.Arguments.Length > 1
                => invocation.Arguments[1],

            _ => null
        };

        if (valueArgument is null)
            return false;

        if (IsSecretTextValue(valueArgument))
            return false;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseSecretTextForSensitiveText,
            valueArgument.Syntax.GetLocation()));

        return true;
    }

    private static void AnalyzeHttpAuthorizationHeader(
        OperationAnalysisContext ctx,
        IInvocationExpression invocation,
        IMethodSymbol targetMethod)
    {
        if (!HttpHeadersMethodNames.Contains(targetMethod.Name))
            return;

        if (invocation.Arguments.Length < 2)
            return;

        NavTypeKind instanceKind = invocation.Instance?.GetSymbol()?.GetTypeSymbol().GetNavTypeKindSafe() ?? EnumProvider.NavTypeKind.None;
        if (instanceKind != EnumProvider.NavTypeKind.HttpHeaders && instanceKind != EnumProvider.NavTypeKind.HttpClient)
            return;

        if (!IsAuthorizationNameArgument(invocation.Arguments[0]))
            return;

        IArgument credentialsArg = invocation.Arguments[1];
        if (IsSecretTextValue(credentialsArg))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseSecretTextForSensitiveText,
            credentialsArg.Syntax.GetLocation()));
    }

    private static void AnalyzeRestClientAuthorizationHeader(
        OperationAnalysisContext ctx,
        IInvocationExpression invocation,
        IMethodSymbol targetMethod)
    {
        if (invocation.Arguments.Length < 2)
            return;

        if (targetMethod.ContainingType?.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Codeunit)
            return;

        var codeunitTypeSymbol = (ICodeunitTypeSymbol)targetMethod.GetContainingObjectTypeSymbol();

        // System.RestClient."Rest Client".SetDefaultRequestHeader(...)
        if (!SemanticFacts.IsSameName(((INamespaceSymbol)codeunitTypeSymbol.ContainingSymbol!).QualifiedName, "System.RestClient"))
            return;

        if (!SemanticFacts.IsSameName(codeunitTypeSymbol.Name, "Rest Client"))
            return;

        if (!SemanticFacts.IsSameName(targetMethod.Name, "SetDefaultRequestHeader"))
            return;

        if (!IsAuthorizationNameArgument(invocation.Arguments[0]))
            return; // RestClient call, but not Authorization header

        IArgument credentialsArg = invocation.Arguments[1];
        if (IsSecretTextValue(credentialsArg))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseSecretTextForSensitiveText,
            credentialsArg.Syntax.GetLocation()));
    }

    private static bool IsSecretTextValue(IArgument argument)
    {
        IOperation value = argument.Value;

        if (value is IConversionExpression conversion)
            value = conversion.Operand;

        // Prefer operation.Type if available; fall back to symbol type
        NavTypeKind navTypeKind =
            value.Type?.GetNavTypeKindSafe()
            ?? value.GetSymbol()?.OriginalDefinition.GetTypeSymbol().GetNavTypeKindSafe()
            ?? EnumProvider.NavTypeKind.None;

        return navTypeKind == EnumProvider.NavTypeKind.SecretText;
    }

    private static bool IsAuthorizationNameArgument(IArgument nameArgument)
    {
        var kind = nameArgument.Syntax.Kind;

        switch (kind)
        {
            case var _ when kind == EnumProvider.SyntaxKind.LiteralExpression:
                {
                    string? text = nameArgument.Value.ConstantValue.Value?.ToString();
                    return !string.IsNullOrEmpty(text) && SemanticFacts.IsSameName(text, AuthorizationHeaderName);
                }

            case var _ when kind == EnumProvider.SyntaxKind.IdentifierName:
                {
                    // Common pattern: Label/const passed where Text expected -> conversion
                    if (nameArgument.Value is not IConversionExpression conv)
                        return false;

                    IOperation operand = conv.Operand;

                    // label "Authorization" locked
                    if (operand.GetSymbol()?.OriginalDefinition.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Label)
                        return false;

                    var label = (ILabelTypeSymbol)operand.GetSymbol()!.OriginalDefinition.GetTypeSymbol();
                    return SemanticFacts.IsSameName(label.Text ?? string.Empty, AuthorizationHeaderName);
                }

            default:
                return false;
        }
    }
}