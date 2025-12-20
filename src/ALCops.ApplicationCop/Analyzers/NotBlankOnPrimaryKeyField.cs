using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class NotBlankOnPrimaryKeyField : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.NotBlankRequiredOnPrimaryKeyField,
            DiagnosticDescriptors.NotBlankNotAllowedOnPrimaryKeyField);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            new Action<SymbolAnalysisContext>(this.AnalyzePrimaryKeyForNotBlankProperty), EnumProvider.SymbolKind.Table);

    private void AnalyzePrimaryKeyForNotBlankProperty(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not ITableTypeSymbol table)
            return;

        if (table.PrimaryKey?.Fields == null || table.PrimaryKey.Fields.Length != 1)
            return;

        var field = table.PrimaryKey.Fields[0];
        if (!field.GetTypeSymbol().HasLength)
            return;

        if (TableContainsNoSeries(table))
        {
            if (field.GetBooleanPropertyValue(EnumProvider.PropertyKind.NotBlank).GetValueOrDefault() && !SemanticFacts.IsSameName(field.Name, "Name"))
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NotBlankNotAllowedOnPrimaryKeyField,
                    field.GetLocation()));
        }
        else
        {
            if (field.GetProperty(EnumProvider.PropertyKind.NotBlank) is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NotBlankRequiredOnPrimaryKeyField,
                    field.GetLocation()));
            }
        }
    }

    private static bool TableContainsNoSeries(ITableTypeSymbol table)
    {
        return table.Fields
            .Where(fld => fld.FieldClass == EnumProvider.FieldClassKind.Normal && fld.Id > 0 && fld.Id < 2000000000)
#if NET8_0_OR_GREATER
            .Where(fld => fld.Type?.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Code)
#endif
            .Any(field =>
        {
            var propertySymbol = field.GetProperty(EnumProvider.PropertyKind.TableRelation);
            if (propertySymbol?.ContainingSymbol is not null)
                return SemanticFacts.IsSameName(propertySymbol.ContainingSymbol.Name.UnquoteIdentifier(), "No. Series");
            return false;
        });
    }
}