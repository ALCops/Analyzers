using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ParameterNotReferenced : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ParameterNotReferenced,
            DiagnosticDescriptors.EventSubscriberParameterNotReferenced);

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

        DiagnosticDescriptor? diagnosticDescriptor = GetDiagnosticDescriptor(method);

        if (diagnosticDescriptor is null)
            return;

        if (method.Parameters.IsEmpty)
            return;

        if (methodSyntax.Body is null)
            return;

        var unusedParameters = GetNonSynthesizedParameters(method);
        if (unusedParameters.Count == 0)
            return;

        var parameters = unusedParameters.Values.ToList();
        MarkReferencedParameters(methodSyntax.Body, unusedParameters);

        // An identifier with the parameter's name may be a field, method or member name
        // (MyTable.Name), so a name match is only a candidate; confirm it on the bound tree.
        // A body with binding errors loses references under invalid nodes, so keep the name result.
        if (unusedParameters.Count < parameters.Count
            && context.SemanticModel.GetOperation(methodSyntax.Body, context.CancellationToken) is IOperation { IsInvalid: false } body)
        {
            var walker = new ParameterReferenceWalker();
            walker.Visit(body);
            unusedParameters = parameters
                .Where(parameter => !walker.ReferencedParameters.Contains(parameter))
                .ToDictionary(parameter => parameter.Name, SemanticFacts.NameEqualityComparer);
        }

        foreach (IParameterSymbol parameter in unusedParameters.Values)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    diagnosticDescriptor,
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

    private sealed class ParameterReferenceWalker : OperationWalker
    {
        public HashSet<IParameterSymbol> ReferencedParameters { get; } = [];

        public override void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        {
            ReferencedParameters.Add(operation.Parameter);
            base.VisitParameterReferenceExpression(operation);
        }
    }

    private static DiagnosticDescriptor? GetDiagnosticDescriptor(IMethodSymbol method)
    {
        // Handler functions (MessageHandler, ConfirmHandler, etc.) have platform-enforced signatures
        if (method.IsHandler())
            return null;

        // ErrorInfo/Notification AddAction callbacks have a contractually required parameter
        if (IsActionCallbackMethod(method))
            return null;

        // Event subscribers are local but explicitly excluded by AA0137,
        // so we handle them here with a dedicated diagnostic ID
        if (method.IsEventSubscriber())
            return DiagnosticDescriptors.EventSubscriberParameterNotReferenced;

        // AA0137 already handles local procedures (except event subscribers above)
        if (method.IsLocal)
            return null;

        // Triggers have platform-defined signatures
        if (method.MethodKind == EnumProvider.MethodKind.Trigger)
            return null;

        // Event declarations define the subscriber contract
        if (method.IsEvent)
            return null;

        // Obsolete methods should not be modified
        if (method.IsObsoleteRemoved || method.IsObsoletePending)
            return null;

        // Interface implementations are bound by the interface contract
        if (method.MethodImplementsInterfaceMethod())
            return null;

        return DiagnosticDescriptors.ParameterNotReferenced;
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
