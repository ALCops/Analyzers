using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class AllowInCustomizationsForOmittedFields : DiagnosticAnalyzer
{
    private const int MinUserFieldId = 1;
    private const int MaxUserFieldIdExclusive = 2000000000;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.AllowInCustomizationsForOmittedFields);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext compilationCtx)
    {
        var (tablesWithPages, tableToReferencedFields) = BuildPageLookups(compilationCtx.Compilation);

        compilationCtx.RegisterSymbolAction(
            symbolCtx => AnalyzeSymbol(symbolCtx, tablesWithPages, tableToReferencedFields),
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension);
    }

    private static void AnalyzeSymbol(
        SymbolAnalysisContext ctx,
        HashSet<ITableTypeSymbol> tablesWithPages,
        Dictionary<ITableTypeSymbol, HashSet<IFieldSymbol>> tableToReferencedFields)
    {
        if (!VersionChecker.IsSupported(ctx.Symbol, EnumProvider.Feature.AddPageControlInPageCustomization))
            return;

        if (ctx.IsObsolete())
            return;

        if (ctx.Symbol.GetProperty(EnumProvider.PropertyKind.AllowInCustomizations) is not null)
            return;

        if (!TryGetTableOrTargetTable(ctx.Symbol, out var table, out var isTableExtension))
            return;

        var candidateFields = GetCandidateFields(ctx.Symbol);
        if (candidateFields.Count == 0)
            return;

        if (!tablesWithPages.Contains(table))
        {
            if (!isTableExtension)
                return;

            if (!BaseTableHasLookupOrDrillDown(table))
                return;
        }

        tableToReferencedFields.TryGetValue(table, out var referencedOnPages);

        foreach (var field in candidateFields)
        {
            if (field.OriginalDefinition is not IFieldSymbol fieldKey)
                continue;

            if (referencedOnPages is not null && referencedOnPages.Contains(fieldKey))
                continue;

            var location =
                field.Location
                ?? field.ContainingSymbol?.Location
                ?? ctx.Symbol.Location;

            if (location is null)
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AllowInCustomizationsForOmittedFields,
                location,
                field.Name));
        }
    }

    private static (HashSet<ITableTypeSymbol> tablesWithPages, Dictionary<ITableTypeSymbol, HashSet<IFieldSymbol>> tableToReferencedFields)
        BuildPageLookups(Compilation compilation)
    {
        var tablesWithPages = new HashSet<ITableTypeSymbol>();
        var tableToReferencedFields = new Dictionary<ITableTypeSymbol, HashSet<IFieldSymbol>>();

        var declared = compilation.GetDeclaredApplicationObjectSymbols();

        for (int i = 0; i < declared.Length; i++)
        {
            var symbol = declared[i];
            var navKind = symbol.GetNavTypeKindSafe();

            if (navKind == EnumProvider.NavTypeKind.Page)
            {
                ProcessPage(symbol, tablesWithPages, tableToReferencedFields);
            }
            else if (navKind == EnumProvider.NavTypeKind.PageExtension)
            {
                ProcessPageExtension(symbol, tablesWithPages, tableToReferencedFields);
            }
        }

        return (tablesWithPages, tableToReferencedFields);
    }

    private static void ProcessPage(
        IApplicationObjectTypeSymbol symbol,
        HashSet<ITableTypeSymbol> tablesWithPages,
        Dictionary<ITableTypeSymbol, HashSet<IFieldSymbol>> tableToReferencedFields)
    {
        if (symbol is not IPageTypeSymbol page)
            return;

        if (page.PageType == PageTypeKind.API)
            return;

        if (page.RelatedTable is not ITableTypeSymbol table)
            return;

        tablesWithPages.Add(table);

        if (!tableToReferencedFields.TryGetValue(table, out var fieldSet))
        {
            fieldSet = new HashSet<IFieldSymbol>();
            tableToReferencedFields[table] = fieldSet;
        }

        AddFieldControls(fieldSet, page.FlattenedControls);
    }

    private static void ProcessPageExtension(
        IApplicationObjectTypeSymbol symbol,
        HashSet<ITableTypeSymbol> tablesWithPages,
        Dictionary<ITableTypeSymbol, HashSet<IFieldSymbol>> tableToReferencedFields)
    {
        if (symbol is not IApplicationObjectExtensionTypeSymbol ext || ext.Target is null)
            return;

        if (ext.Target.GetTypeSymbol() is not IPageTypeSymbol targetPage)
            return;

        if (targetPage.RelatedTable is not ITableTypeSymbol table)
            return;

        tablesWithPages.Add(table);

        if (!tableToReferencedFields.TryGetValue(table, out var fieldSet))
        {
            fieldSet = new HashSet<IFieldSymbol>();
            tableToReferencedFields[table] = fieldSet;
        }

        if (symbol is IPageExtensionTypeSymbol pageExt)
            AddFieldControls(fieldSet, pageExt.AddedControlsFlattened);
    }

    private static bool TryGetTableOrTargetTable(ISymbol symbol, out ITableTypeSymbol table, out bool isTableExtension)
    {
        table = null!;
        isTableExtension = false;

        if (symbol is ITableTypeSymbol t)
        {
            table = t;
            return true;
        }

        if (symbol is IApplicationObjectExtensionTypeSymbol ext)
        {
            isTableExtension = true;

            if (ext.Target is ITableTypeSymbol targetTable)
            {
                table = targetTable;
                return true;
            }

            return false;
        }

        return false;
    }

    private static List<IFieldSymbol> GetCandidateFields(ISymbol symbol)
    {
        ICollection<IFieldSymbol> fields;

        if (symbol is ITableTypeSymbol table)
            fields = table.Fields;
        else if (symbol is ITableExtensionTypeSymbol tableExt)
            fields = tableExt.AddedFields;
        else
            return new List<IFieldSymbol>(0);

        if (fields.Count == 0)
            return new List<IFieldSymbol>(0);

        var result = new List<IFieldSymbol>(fields.Count);

        foreach (var field in fields)
        {
            if (field.Id < MinUserFieldId || field.Id >= MaxUserFieldIdExclusive)
                continue;

            if (field.DeclaredAccessibility == EnumProvider.Accessibility.Local ||
             field.DeclaredAccessibility == EnumProvider.Accessibility.Protected)
                continue;

            if (field.FieldClass == EnumProvider.FieldClassKind.FlowFilter)
                continue;

            if (field.GetBooleanPropertyValue(EnumProvider.PropertyKind.Enabled) == false)
                continue;

            if (field.GetProperty(EnumProvider.PropertyKind.AllowInCustomizations) is not null)
                continue;

            if (field.IsObsolete())
                continue;

            var navTypeKind = field.OriginalDefinition.GetTypeSymbol().GetNavTypeKindSafe();
            if (!IsSupportedType(navTypeKind))
                continue;

            result.Add(field);
        }

        return result;
    }

    private static void AddFieldControls(HashSet<IFieldSymbol> set, ImmutableArray<IControlSymbol> controls)
    {
        foreach (var c in controls)
        {
            if (c.ControlKind != EnumProvider.ControlKind.Field)
                continue;

            if (c.RelatedFieldSymbol is not IFieldSymbol field)
                continue;

            set.Add((IFieldSymbol)field.OriginalDefinition);
        }
    }

    private static bool IsSupportedType(NavTypeKind navTypeKind) =>
        navTypeKind switch
        {
            var k when k == EnumProvider.NavTypeKind.Blob => false,
            var k when k == EnumProvider.NavTypeKind.Media => false,
            var k when k == EnumProvider.NavTypeKind.MediaSet => false,
            var k when k == EnumProvider.NavTypeKind.RecordId => false,
            var k when k == EnumProvider.NavTypeKind.TableFilter => false,
            _ => true
        };

    private static bool BaseTableHasLookupOrDrillDown(ITableTypeSymbol table) =>
        table.Properties.Any(p =>
            p.PropertyKind == EnumProvider.PropertyKind.DrillDownPageId ||
            p.PropertyKind == EnumProvider.PropertyKind.LookupPageId);
}
