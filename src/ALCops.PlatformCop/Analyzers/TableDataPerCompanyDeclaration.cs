using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public class TableDataPerCompanyDeclaration : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableDataPerCompanyDeclaration);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            this.AnalyzeTableObject,
            EnumProvider.SymbolKind.Table);

    private void AnalyzeTableObject(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not ITableTypeSymbol table)
            return;

        if (table.TableType == EnumProvider.TableTypeKind.Temporary)
            return;

        if (table.GetProperty(EnumProvider.PropertyKind.DataPerCompany) is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TableDataPerCompanyDeclaration,
                table.GetLocation()));
        }
    }
}