using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ProcedureRequiresDocumentation : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.PublicProcedureRequiresDocumentation,
            DiagnosticDescriptors.InternalProcedureRequiresDocumentation,
            DiagnosticDescriptors.EventRequiresDocumentation,
            DiagnosticDescriptors.InternalEventRequiresDocumentation);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeProcedures,
            EnumProvider.SyntaxKind.MethodDeclaration
        );

    private void AnalyzeProcedures(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not MethodDeclarationSyntax method)
            return;

        var containingApplicationObject = ctx.ContainingSymbol.GetContainingApplicationObjectTypeSymbol();

        // Interfaces and control add-ins are IObjectTypeSymbol but not IApplicationObjectTypeSymbol,
        // so fall back to the object-type walker only when no application object exists in the chain.
        // The application-object walker stays primary so members nested in request pages keep
        // resolving to their report/xmlport instead of the request page (which is always Local).
        var containingObject = (ISymbol?)containingApplicationObject
            ?? ctx.ContainingSymbol.GetContainingObjectTypeSymbol();

        if (containingObject?.Kind == EnumProvider.SymbolKind.ControlAddIn)
            return;

        if (containingApplicationObject.IsTestCodeunit())
            return;

        if (HasXmlDocumentation(method))
            return;

        var accessibilityToken = method.ProcedureKeyword.GetPreviousToken();

        if (ctx.ContainingSymbol is IMethodSymbol methodSymbol && (methodSymbol is not null))
        {
            var methodDisplayText = methodSymbol.GetDiagnosticDisplayText(MethodSymbolDisplayFormat.MethodSignature);

            if (methodSymbol.IsIntegrationOrBusinessEvent())
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EventRequiresDocumentation,
                    method.Name.GetLocation(),
                    methodDisplayText));
            }

            else if (methodSymbol.IsInternalEvent())
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InternalEventRequiresDocumentation,
                    method.Name.GetLocation(),
                    methodDisplayText));
            }

            else
            {
                if (accessibilityToken.Kind == EnumProvider.SyntaxKind.LocalKeyword)
                {
                    return;
                }

                if ((accessibilityToken.Kind == EnumProvider.SyntaxKind.InternalKeyword) ||
                    (containingObject?.DeclaredAccessibility == EnumProvider.Accessibility.Internal))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InternalProcedureRequiresDocumentation,
                        method.Name.GetLocation(),
                        methodDisplayText));
                }
                else
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.PublicProcedureRequiresDocumentation,
                        method.Name.GetLocation(),
                        methodDisplayText));
                }
            }
        }
    }

    private static bool HasXmlDocumentation(MethodDeclarationSyntax method)
    {
        var trivia = method.GetLeadingTrivia();

        return trivia.Any(t =>
            t.Kind == EnumProvider.SyntaxKind.SingleLineDocumentationCommentTrivia ||
            t.Kind == EnumProvider.SyntaxKind.MultiLineDocumentationCommentTrivia);
    }
}