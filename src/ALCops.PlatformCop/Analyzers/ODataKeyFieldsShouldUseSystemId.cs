using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ODataKeyFieldsShouldUseSystemId : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ODataKeyFieldsShouldUseSystemId);

    public override void Initialize(AnalysisContext context)
        => context.RegisterSymbolAction(
            AnalyzeODataKeyFieldsPropertyOnApiPage,
            EnumProvider.SymbolKind.Page);

    private void AnalyzeODataKeyFieldsPropertyOnApiPage(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IPageTypeSymbol pageTypeSymbol)
            return;

        if (pageTypeSymbol.PageType != EnumProvider.PageTypeKind.API)
            return;

        if (pageTypeSymbol.GetBooleanPropertyValue(EnumProvider.PropertyKind.SourceTableTemporary).GetValueOrDefault())
            return;

        if (pageTypeSymbol.GetProperty(EnumProvider.PropertyKind.ODataKeyFields) is not IPropertySymbol property)
            return;

        // "2000000000" == SystemId field id
        if (string.Equals(property.ValueText, "2000000000", StringComparison.Ordinal))
            return;

        if (property.Value is null)
            return;

        if (property.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken) is not PropertySyntax propertySyntax)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ODataKeyFieldsShouldUseSystemId,
            propertySyntax.Value.GetLocation(),
            propertySyntax.Value.ToString()!
            ));
    }
}