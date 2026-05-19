using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ParameterNotReferenced : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.ParameterNotReferenced);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCodeBlockAction(AnalyzeCodeBlock);
    }

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
    {
        if (context.IsObsolete() || context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodSyntax)
            return;

        if (context.OwningSymbol is not IMethodSymbol method)
            return;

        if (!ShouldAnalyzeMethod(method))
            return;

        if (method.Parameters.IsEmpty)
            return;

        if (methodSyntax.Body is null)
            return;

        var unusedParameters = GetNonSynthesizedParameters(method);
        if (unusedParameters.Count == 0)
            return;

        MarkReferencedParameters(methodSyntax.Body, unusedParameters);

        foreach (IParameterSymbol parameter in unusedParameters.Values)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ParameterNotReferenced,
                    parameter.GetLocation(),
                    parameter.Name,
                    method.Name));
        }
    }

    private static Dictionary<string, IParameterSymbol> GetNonSynthesizedParameters(IMethodSymbol method)
    {
        var parameters = new Dictionary<string, IParameterSymbol>(SemanticFacts.NameEqualityComparer);
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (!parameter.IsSynthesized)
                parameters[parameter.Name] = parameter;
        }
        return parameters;
    }

    private static void MarkReferencedParameters(SyntaxNode body, Dictionary<string, IParameterSymbol> unusedParameters)
    {
        foreach (SyntaxNode node in body.DescendantNodes())
        {
            if (unusedParameters.Count == 0)
                return;

            if (node is IdentifierNameSyntax identifier)
                unusedParameters.Remove(identifier.Unquoted());
        }
    }

    private static bool ShouldAnalyzeMethod(IMethodSymbol method)
    {
        // Handler functions (MessageHandler, ConfirmHandler, etc.) have platform-enforced signatures
        if (method.IsHandler())
            return false;

        // ErrorInfo/Notification AddAction callbacks have a contractually required parameter
        if (IsActionCallbackMethod(method))
            return false;

        // Event subscribers are local but explicitly excluded by AA0137,
        // so we handle them here
        if (method.IsEventSubscriber())
            return true;

        // AA0137 already handles local procedures (except event subscribers above)
        if (method.IsLocal)
            return false;

        // Triggers have platform-defined signatures
        if (method.MethodKind == EnumProvider.MethodKind.Trigger)
            return false;

        // Event declarations define the subscriber contract
        if (method.IsEvent)
            return false;

        // Obsolete methods should not be modified
        if (method.IsObsoleteRemoved || method.IsObsoletePending)
            return false;

        // Interface implementations are bound by the interface contract
        if (method.MethodImplementsInterfaceMethod())
            return false;

        return true;
    }

    private static bool IsActionCallbackMethod(IMethodSymbol method)
    {
        if (method.Parameters.Length != 1)
            return false;

        IApplicationObjectTypeSymbol? containingObject = method.GetContainingApplicationObjectTypeSymbol();
        if (containingObject is null || containingObject.NavTypeKind != EnumProvider.NavTypeKind.Codeunit)
            return false;

        NavTypeKind paramTypeKind = method.Parameters[0].ParameterType.GetNavTypeKindSafe();
        return paramTypeKind == EnumProvider.NavTypeKind.ErrorInfo
            || paramTypeKind == EnumProvider.NavTypeKind.Notification;
    }
}
