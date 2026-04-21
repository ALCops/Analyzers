using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class FieldGroupsRequired : DiagnosticAnalyzer
{
    private const string FieldGroupNameBrick = "Brick";
    private const string FieldGroupNameDropDown = "DropDown";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.FieldGroupsRequired);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var tablesReferencedByPages = BuildTablesReferencedByPages(compilationContext);

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                AnalyzeFieldgroups(symbolContext, tablesReferencedByPages);
            }, EnumProvider.SymbolKind.Table);
        });
    }

    private static void AnalyzeFieldgroups(SymbolAnalysisContext ctx, ISet<ITableTypeSymbol> tablesReferencedByPages)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not ITableTypeSymbol table)
            return;

        if (IsTableOfTypeSetupTable(table))
            return;

        if (IsTemporaryTable(table) && !tablesReferencedByPages.Contains(table))
            return;

        CheckFieldGroup(ctx, table, FieldGroupNameBrick, table.GetLocation());
        CheckFieldGroup(ctx, table, FieldGroupNameDropDown, table.GetLocation());
    }

    private static void CheckFieldGroup(SymbolAnalysisContext ctx, ITableTypeSymbol table, string fieldGroupName, Location location)
    {
        if (!table.FieldGroups.Any(item => item.Name == fieldGroupName && item.Fields.Length > 0))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FieldGroupsRequired,
                location,
                table.Name,
                fieldGroupName));
        }
    }

    private static bool IsTableOfTypeSetupTable(ITableTypeSymbol table)
    {
        // Expect Primary Key to contains only one field
        if (table.PrimaryKey is null || table.PrimaryKey.Fields.Length != 1)
            return false;

        // The field should be of type Code
        if (table.PrimaryKey.Fields[0].GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Code)
            return false;

        // The field should be exactly (case sensitive) called 'Primary Key'
        return string.Equals(table.PrimaryKey.Fields[0].Name, "Primary Key", StringComparison.Ordinal);
    }

    private static bool IsTemporaryTable(ITableTypeSymbol table)
        => table.TableType == EnumProvider.TableTypeKind.Temporary;

    private static HashSet<ITableTypeSymbol> BuildTablesReferencedByPages(CompilationStartAnalysisContext ctx)
    {
        var result = new HashSet<ITableTypeSymbol>();
        var declared = ctx.Compilation.GetDeclaredApplicationObjectSymbols();

        for (int i = 0; i < declared.Length; i++)
        {
            var symbol = declared[i];

            if (symbol.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
                continue;

            if (symbol is IPageTypeSymbol page && page.RelatedTable is ITableTypeSymbol table)
                result.Add(table);
        }

        return result;
    }
}