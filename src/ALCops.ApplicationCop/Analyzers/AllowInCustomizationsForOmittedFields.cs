using System.Collections.Concurrent;
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
        var (tablesWithPages, tableToPages, tableToPageExtensions) =
            BuildTableToPageIndex(compilationCtx.Compilation);

        var fieldRefCache = new ConcurrentDictionary<ITableTypeSymbol, Lazy<HashSet<IFieldSymbol>>>();

        compilationCtx.RegisterSymbolAction(
            symbolCtx => AnalyzeSymbol(
                symbolCtx, tablesWithPages, tableToPages, tableToPageExtensions, fieldRefCache),
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension);
    }

    private static void AnalyzeSymbol(
        SymbolAnalysisContext ctx,
        HashSet<ITableTypeSymbol> tablesWithPages,
        Dictionary<ITableTypeSymbol, List<IPageTypeSymbol>> tableToPages,
        Dictionary<ITableTypeSymbol, List<IPageExtensionTypeSymbol>> tableToPageExtensions,
        ConcurrentDictionary<ITableTypeSymbol, Lazy<HashSet<IFieldSymbol>>> fieldRefCache)
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

        var referencedOnPages = fieldRefCache.GetOrAdd(
            table,
            t => new Lazy<HashSet<IFieldSymbol>>(
                () => ResolveFieldsOnPages(t, tableToPages, tableToPageExtensions))).Value;

        foreach (var field in candidateFields)
        {
            if (field.OriginalDefinition is not IFieldSymbol fieldKey)
                continue;

            if (referencedOnPages.Contains(fieldKey))
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

    /// <summary>
    /// Builds a lightweight index of which tables have pages and which pages/extensions reference each table.
    /// Does NOT access FlattenedControls or iterate controls (deferred to lazy resolution).
    /// </summary>
    private static (
        HashSet<ITableTypeSymbol> tablesWithPages,
        Dictionary<ITableTypeSymbol, List<IPageTypeSymbol>> tableToPages,
        Dictionary<ITableTypeSymbol, List<IPageExtensionTypeSymbol>> tableToPageExtensions)
        BuildTableToPageIndex(Compilation compilation)
    {
        var tablesWithPages = new HashSet<ITableTypeSymbol>();
        var tableToPages = new Dictionary<ITableTypeSymbol, List<IPageTypeSymbol>>();
        var tableToPageExtensions = new Dictionary<ITableTypeSymbol, List<IPageExtensionTypeSymbol>>();

        var declared = compilation.GetDeclaredApplicationObjectSymbols();

        for (int i = 0; i < declared.Length; i++)
        {
            var symbol = declared[i];
            var navKind = symbol.GetNavTypeKindSafe();

            if (navKind == EnumProvider.NavTypeKind.Page)
            {
                IndexPage(symbol, tablesWithPages, tableToPages);
            }
            else if (navKind == EnumProvider.NavTypeKind.PageExtension)
            {
                IndexPageExtension(symbol, tablesWithPages, tableToPageExtensions);
            }
        }

        return (tablesWithPages, tableToPages, tableToPageExtensions);
    }

    private static void IndexPage(
        IApplicationObjectTypeSymbol symbol,
        HashSet<ITableTypeSymbol> tablesWithPages,
        Dictionary<ITableTypeSymbol, List<IPageTypeSymbol>> tableToPages)
    {
        if (symbol is not IPageTypeSymbol page)
            return;

        if (page.PageType == PageTypeKind.API)
            return;

        if (page.RelatedTable is not ITableTypeSymbol table)
            return;

        tablesWithPages.Add(table);

        if (!tableToPages.TryGetValue(table, out var pages))
        {
            pages = new List<IPageTypeSymbol>();
            tableToPages[table] = pages;
        }

        pages.Add(page);
    }

    private static void IndexPageExtension(
        IApplicationObjectTypeSymbol symbol,
        HashSet<ITableTypeSymbol> tablesWithPages,
        Dictionary<ITableTypeSymbol, List<IPageExtensionTypeSymbol>> tableToPageExtensions)
    {
        if (symbol is not IApplicationObjectExtensionTypeSymbol ext || ext.Target is null)
            return;

        if (ext.Target.GetTypeSymbol() is not IPageTypeSymbol targetPage)
            return;

        if (targetPage.RelatedTable is not ITableTypeSymbol table)
            return;

        tablesWithPages.Add(table);

        if (symbol is not IPageExtensionTypeSymbol pageExt)
            return;

        if (!tableToPageExtensions.TryGetValue(table, out var extensions))
        {
            extensions = new List<IPageExtensionTypeSymbol>();
            tableToPageExtensions[table] = extensions;
        }

        extensions.Add(pageExt);
    }

    /// <summary>
    /// Resolves all field symbols referenced on pages/page extensions for a given table.
    /// Called lazily per-table, at most once (cached via ConcurrentDictionary + Lazy).
    /// </summary>
    private static HashSet<IFieldSymbol> ResolveFieldsOnPages(
        ITableTypeSymbol table,
        Dictionary<ITableTypeSymbol, List<IPageTypeSymbol>> tableToPages,
        Dictionary<ITableTypeSymbol, List<IPageExtensionTypeSymbol>> tableToPageExtensions)
    {
        var fieldSet = new HashSet<IFieldSymbol>();

        if (tableToPages.TryGetValue(table, out var pages))
        {
            foreach (var page in pages)
                AddFieldControls(fieldSet, page.FlattenedControls);
        }

        if (tableToPageExtensions.TryGetValue(table, out var extensions))
        {
            foreach (var ext in extensions)
                AddFieldControls(fieldSet, ext.AddedControlsFlattened);
        }

        return fieldSet;
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
