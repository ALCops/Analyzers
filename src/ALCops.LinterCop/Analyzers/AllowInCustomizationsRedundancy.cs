using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class AllowInCustomizationsRedundancy : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.AllowInCustomizationsRedundancy);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeAllowInCustomizationsForRedundancy,
            EnumProvider.SymbolKind.Field);

    private void AnalyzeAllowInCustomizationsForRedundancy(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IFieldSymbol field)
            return;

        IPropertySymbol? fieldAllowInCustomizations = field.GetProperty(EnumProvider.PropertyKind.AllowInCustomizations);
        if (fieldAllowInCustomizations is null)
            return;

        IPropertySymbol? parentAllowInCustomizations = GetContainingObjectAllowInCustomizations(field);
        if (parentAllowInCustomizations is null)
            return;

        if (!string.Equals(fieldAllowInCustomizations.ValueText, parentAllowInCustomizations.ValueText, StringComparison.OrdinalIgnoreCase))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AllowInCustomizationsRedundancy,
            fieldAllowInCustomizations.GetLocation()));
    }

    private static IPropertySymbol? GetContainingObjectAllowInCustomizations(IFieldSymbol field)
    {
        if (field.ContainingSymbol is ITableTypeSymbol table)
            return table.IsObsolete() ? null : table.GetProperty(EnumProvider.PropertyKind.AllowInCustomizations);

        if (field.ContainingSymbol is ITableExtensionTypeSymbol tableExt)
            return tableExt.IsObsolete() ? null : tableExt.GetProperty(EnumProvider.PropertyKind.AllowInCustomizations);

        return null;
    }
}
