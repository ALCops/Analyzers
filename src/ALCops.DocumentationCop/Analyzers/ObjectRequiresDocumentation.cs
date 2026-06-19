using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.DocumentationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ObjectRequiresDocumentation : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(
			DiagnosticDescriptors.PublicObjectRequiresDocumentation,
			DiagnosticDescriptors.InternalObjectRequiresDocumentation);

	public override void Initialize(AnalysisContext context)
		=> context.RegisterSymbolAction(
			CheckObjectDocumentation,
			EnumProvider.SymbolKind.Codeunit,
			EnumProvider.SymbolKind.ControlAddIn,
			EnumProvider.SymbolKind.Enum,
			EnumProvider.SymbolKind.Interface,
			EnumProvider.SymbolKind.Page,
			EnumProvider.SymbolKind.PermissionSet,
			EnumProvider.SymbolKind.Profile,
			EnumProvider.SymbolKind.Query,
			EnumProvider.SymbolKind.Report,
			EnumProvider.SymbolKind.Table,
			EnumProvider.SymbolKind.XmlPort);

	private void CheckObjectDocumentation(SymbolAnalysisContext ctx)
	{
        if (ctx.IsObsolete())
		{
            return;
		}

		if (ctx.Symbol is not IApplicationObjectTypeSymbol appObjectTypeSymbol)
		{
			return;
		}

		if (appObjectTypeSymbol.IsTestCodeunit())
		{
			return;
		}

		var xmlComment = appObjectTypeSymbol.GetDocumentationCommentXml();

		if (string.IsNullOrWhiteSpace(xmlComment))
		{
			if (appObjectTypeSymbol.DeclaredAccessibility == EnumProvider.Accessibility.Public)
			{
				ctx.ReportDiagnostic(Diagnostic.Create(
					DiagnosticDescriptors.PublicObjectRequiresDocumentation,
					appObjectTypeSymbol.GetLocation(),
					appObjectTypeSymbol.Name));
			}

			else if (appObjectTypeSymbol.DeclaredAccessibility == EnumProvider.Accessibility.Internal)
			{
				ctx.ReportDiagnostic(Diagnostic.Create(
					DiagnosticDescriptors.InternalObjectRequiresDocumentation,
					appObjectTypeSymbol.GetLocation(),
					appObjectTypeSymbol.Name));
			}
		}
	}
}
