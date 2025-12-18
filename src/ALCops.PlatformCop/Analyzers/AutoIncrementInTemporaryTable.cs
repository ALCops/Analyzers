using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public class AutoIncrementInTemporaryTable : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.AutoIncrementInTemporaryTable);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(new Action<SymbolAnalysisContext>(this.AnalyzeTemporaryTables), EnumProvider.SymbolKind.Table);

    private void AnalyzeTemporaryTables(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not ITableTypeSymbol table)
            return;

        if (table.TableType != EnumProvider.TableTypeKind.Temporary)
            return;

        foreach (var field in table.Fields)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var autoIncrementProperty = field.GetProperty(EnumProvider.PropertyKind.AutoIncrement);
            if (autoIncrementProperty is not null &&
                 autoIncrementProperty.Value is bool isAutoIncrement && isAutoIncrement)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AutoIncrementInTemporaryTable,
                    autoIncrementProperty.GetLocation()));
            }
        }
    }
}