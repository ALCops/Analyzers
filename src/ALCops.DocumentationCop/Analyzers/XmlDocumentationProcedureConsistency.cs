using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class XmlDocumentationProcedureConsistency : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.XmlDocumentationProcedureConsistency);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeDocumentationComments,
            EnumProvider.SyntaxKind.MethodDeclaration);

    private void AnalyzeDocumentationComments(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
            return;

        var docCommentTrivia = methodDeclarationSyntax.GetLeadingTrivia().FirstOrDefault(trivia => trivia.Kind == EnumProvider.SyntaxKind.SingleLineDocumentationCommentTrivia);
        if (docCommentTrivia.IsKind(EnumProvider.SyntaxKind.None))
            return; // no documentation comment exists

        Dictionary<string, XmlElementSyntax> docCommentParameters = new Dictionary<string, XmlElementSyntax>(StringComparer.OrdinalIgnoreCase);
        XmlElementSyntax? docCommentReturns = null;

        var docCommentStructure = (DocumentationCommentTriviaSyntax)docCommentTrivia.GetStructure();
        var docCommentElements = docCommentStructure.Content.Where(xmlNode => xmlNode.Kind == EnumProvider.SyntaxKind.XmlElement);

        // evaluate documentation comment syntax
        foreach (XmlElementSyntax element in docCommentElements.Cast<XmlElementSyntax>())
        {
            switch (element.StartTag.Name.LocalName.Text.ToLowerInvariant())
            {
                case "param":
                    var nameAttribute = (XmlNameAttributeSyntax)element.StartTag.Attributes.First(att => att.IsKind(EnumProvider.SyntaxKind.XmlNameAttribute));
                    var parameterName = nameAttribute.Identifier.GetText().ToString();
                    if (!docCommentParameters.ContainsKey(parameterName))
                        docCommentParameters.Add(parameterName, element);
                    else
                        // report diagnostic for duplicate parameter documentation
                        ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.XmlDocumentationProcedureConsistency, element.GetLocation()));
                    break;
                case "returns":
                    if (docCommentReturns is not null)
                        // report diagnostic for duplicate returns documentation
                        ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.XmlDocumentationProcedureConsistency, element.GetLocation()));
                    docCommentReturns = element;
                    break;
            }
        }

        // excess documentation comment return value
        if (docCommentReturns is not null && methodDeclarationSyntax.ReturnValue is null)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.XmlDocumentationProcedureConsistency, docCommentReturns.GetLocation()));

        // return value without documentation comment
        if (docCommentReturns is null && (methodDeclarationSyntax.ReturnValue is not null))
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.XmlDocumentationProcedureConsistency, methodDeclarationSyntax.ReturnValue.GetLocation()));

        // method with TryFunction decorator without return in documentation comment
        if (docCommentReturns is null)
        {
            var tryFunctionAttribute =
                methodDeclarationSyntax.Attributes
                    .FirstOrDefault(attr =>
                        string.Equals(
                            attr.Name.Identifier.ValueText?.UnquoteIdentifier(),
                            "TryFunction",
                            StringComparison.OrdinalIgnoreCase));

            if (tryFunctionAttribute is not null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.XmlDocumentationProcedureConsistency,
                        tryFunctionAttribute.Name.GetLocation()));
            }
        }

        // check documentation comment parameters against method syntax
        foreach (var docCommentParameter in docCommentParameters)
        {
            if (!methodDeclarationSyntax.ParameterList.Parameters.Any(param => (param.Name.Identifier.ValueText?.UnquoteIdentifier() ?? string.Empty).Equals(docCommentParameter.Key, StringComparison.OrdinalIgnoreCase)))
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.XmlDocumentationProcedureConsistency, docCommentParameter.Value.GetLocation()));
        }

        // check method parameters against documentation comment syntax
        foreach (var methodParameter in methodDeclarationSyntax.ParameterList.Parameters)
        {
            if (!docCommentParameters.Any(docParam => docParam.Key.Equals(methodParameter.Name.Identifier.ValueText?.UnquoteIdentifier(), StringComparison.OrdinalIgnoreCase)))
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.XmlDocumentationProcedureConsistency, methodParameter.GetLocation()));
        }
    }
}