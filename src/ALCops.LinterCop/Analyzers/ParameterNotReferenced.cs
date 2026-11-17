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

        HashSet<IParameterSymbol> candidates = MarkReferencedParameters(methodSyntax.Body, unusedParameters);

        // An identifier with the parameter's name may be a field, method or member name
        // (MyTable.Name), so a name match is only a candidate; confirm it on the bound tree.
        // A body with binding errors loses references under invalid nodes, so keep the name result.
        if (candidates.Count > 0
            && context.SemanticModel.GetOperation(methodSyntax.Body, context.CancellationToken) is IOperation { IsInvalid: false } body)
            new ParameterReferenceWalker(candidates).Visit(body);
        else
            candidates.Clear();

        foreach (IParameterSymbol parameter in unusedParameters.Values.Concat(candidates))
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

    private static HashSet<IParameterSymbol> MarkReferencedParameters(SyntaxNode body, Dictionary<string, IParameterSymbol> unusedParameters)
    {
        var candidates = new HashSet<IParameterSymbol>();
        foreach (SyntaxNode node in body.DescendantNodes())
        {
            if (unusedParameters.Count == 0)
                break;

            if (node is IdentifierNameSyntax identifier
                && unusedParameters.Remove(identifier.Unquoted(), out IParameterSymbol? parameter))
                candidates.Add(parameter);
        }
        return candidates;
    }

    private sealed class ParameterReferenceWalker(HashSet<IParameterSymbol> unconfirmed) : OperationWalker
    {
        public override void Visit(IOperation operation)
        {
            if (unconfirmed.Count > 0)
                base.Visit(operation);
        }

        public override void VisitParameterReferenceExpression(IParameterReferenceExpression operation) =>
            unconfirmed.Remove(operation.Parameter);

        // The SDK walker does not descend into some kinds (is/as, conditional expressions, test page,
        // query, report and xmlport member access) and these interfaces vary per SDK version,
        // so inside them a candidate is confirmed by name, as before binding.
        public override void DefaultVisit(IOperation operation)
        {
            foreach (SyntaxNode node in operation.Syntax?.DescendantNodesAndSelf() ?? [])
            {
                if (node is IdentifierNameSyntax identifier)
                    unconfirmed.RemoveWhere(parameter => SemanticFacts.NameEqualityComparer.Equals(parameter.Name, identifier.Unquoted()));
            }
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
