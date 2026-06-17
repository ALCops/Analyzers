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

		var containingObject = ctx.ContainingSymbol.GetContainingObjectTypeSymbol();

		if (IsTestCodeunit(containingObject))
			return;

		if (HasXmlDocumentation(method))
			return;

		var accessibilityToken = method.ProcedureKeyword.GetPreviousToken();

		if (method.IsIntegrationOrBusinessEvent())
		{
			var loc = method.Name.GetLocation();
			var id = method.Name.Identifier.ToString();

			ctx.ReportDiagnostic(Diagnostic.Create(
				DiagnosticDescriptors.EventRequiresDocumentation,
				loc,
				id));
		}

		else if (method.IsInternalEvent())
		{
			ctx.ReportDiagnostic(Diagnostic.Create(
				DiagnosticDescriptors.InternalEventRequiresDocumentation,
				method.Name.GetLocation(),
				method.Name.Identifier.ToString()));
		}

		else
		{
			if (accessibilityToken.Kind == EnumProvider.SyntaxKind.LocalKeyword)
			{
				return;
			}

			if ((accessibilityToken.Kind == EnumProvider.SyntaxKind.InternalKeyword) ||
				(containingObject.DeclaredAccessibility == EnumProvider.Accessibility.Internal))
			{
				ctx.ReportDiagnostic(Diagnostic.Create(
					DiagnosticDescriptors.InternalProcedureRequiresDocumentation,
					method.Name.GetLocation(),
					method.Name.Identifier.ToString()));
			}
			else
			{
				ctx.ReportDiagnostic(Diagnostic.Create(
					DiagnosticDescriptors.PublicProcedureRequiresDocumentation,
					method.Name.GetLocation(),
					method.Name.Identifier.ToString()));
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

	private static bool IsTestCodeunit(IObjectTypeSymbol symbol)
	{
		var subtype = symbol.GetEnumPropertyValue<CodeunitSubtypeKind>(EnumProvider.PropertyKind.Subtype);

		return subtype is not null && subtype == EnumProvider.CodeunitSubtypeKind.Test;
	}
}