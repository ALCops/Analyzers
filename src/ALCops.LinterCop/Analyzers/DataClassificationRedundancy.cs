using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public class DataClassificationRedundancy : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.DataClassificationRedundancy);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeDataClassificationForRedundancy,
            EnumProvider.SymbolKind.Field);

    private void AnalyzeDataClassificationForRedundancy(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IFieldSymbol field)
            return;

        if (field.ContainingSymbol is not ITableTypeSymbol table || table.IsObsolete())
            return;

        IPropertySymbol? fieldClassification = field.GetProperty(EnumProvider.PropertyKind.DataClassification);
        if (fieldClassification is null)
            return;

        IPropertySymbol? tableClassification = table.GetProperty(EnumProvider.PropertyKind.DataClassification);
        if (tableClassification is null)
            return;

        if (!string.Equals(fieldClassification.ValueText, tableClassification.ValueText, StringComparison.OrdinalIgnoreCase))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DataClassificationRedundancy,
            fieldClassification.GetLocation()));
    }
}