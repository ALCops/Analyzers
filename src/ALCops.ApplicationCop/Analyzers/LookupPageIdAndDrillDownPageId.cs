using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class LookupPageIdAndDrillDownPageId : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.LookupPageIdAndDrillDownPageId);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            this.CheckForLookupPageIdAndDrillDownPageId,
            EnumProvider.SymbolKind.Page);

    private void CheckForLookupPageIdAndDrillDownPageId(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IPageTypeSymbol pageTypeSymbol)
            return;

        if (pageTypeSymbol.PageType != EnumProvider.PageTypeKind.List ||
            pageTypeSymbol.RelatedTable is null ||
            pageTypeSymbol.GetBooleanPropertyValue(EnumProvider.PropertyKind.SourceTableTemporary).GetValueOrDefault() ||
            pageTypeSymbol.RelatedTable.ContainingModule != ctx.Symbol.ContainingModule)
            return;

        AnalyzeRelatedTable(pageTypeSymbol.RelatedTable, ctx);
    }

    private void AnalyzeRelatedTable(ITableTypeSymbol table, SymbolAnalysisContext context)
    {
        if (table.TableType == EnumProvider.TableTypeKind.Temporary ||
            !table.GetLocation().IsInSource ||
            table.IsObsolete())
            return;

        bool hasRequiredProperties = table.Properties.Count(property =>
            property.PropertyKind == EnumProvider.PropertyKind.DrillDownPageId ||
            property.PropertyKind == EnumProvider.PropertyKind.LookupPageId) == 2;

        if (hasRequiredProperties)
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.LookupPageIdAndDrillDownPageId,
                table.GetLocation(),
                table.Name.ToString().QuoteIdentifierIfNeededWithReflection(),
                context.Symbol.Name.ToString().QuoteIdentifierIfNeededWithReflection()));
    }
}