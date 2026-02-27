using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PublicProcedureRequiresDocumentation : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.PublicProcedureRequiresDocumentation);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzePublicProcedures,
            EnumProvider.SyntaxKind.MethodDeclaration
        );

    private void AnalyzePublicProcedures(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not MethodDeclarationSyntax method)
            return;

        // Rule applies only when the containing object itself is public
        if (ctx.ContainingSymbol.GetContainingObjectTypeSymbol().DeclaredAccessibility != EnumProvider.Accessibility.Public)
            return;

        var accessibilityToken = method.ProcedureKeyword.GetPreviousToken();
        if (accessibilityToken.Kind == EnumProvider.SyntaxKind.LocalKeyword ||
            accessibilityToken.Kind == EnumProvider.SyntaxKind.InternalKeyword)
            return;

        if (HasXmlDocumentation(method))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PublicProcedureRequiresDocumentation,
            method.Name.GetLocation(),
            method.Name.Identifier.ToString()));
    }

    private static bool HasXmlDocumentation(MethodDeclarationSyntax method)
    {
        var trivia = method.GetLeadingTrivia();

        return trivia.Any(t =>
            t.Kind == EnumProvider.SyntaxKind.SingleLineDocumentationCommentTrivia ||
            t.Kind == EnumProvider.SyntaxKind.MultiLineDocumentationCommentTrivia);
    }
}