using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class DuplicateODataEntityName : DiagnosticAnalyzer
{
    private static readonly HashSet<PageTypeKind> RelevantPageTypes =
    [
        EnumProvider.PageTypeKind.Card,
        EnumProvider.PageTypeKind.Document,
        EnumProvider.PageTypeKind.List,
        EnumProvider.PageTypeKind.ListPart,
        EnumProvider.PageTypeKind.ListPlus,
        EnumProvider.PageTypeKind.Worksheet
    ];

    private static ImmutableArray<IPageExtensionBaseTypeSymbol> GetCachedPageExtensions(Compilation compilation)
        => PageExtensionsCache.GetValue(compilation, static c => new PageExtensionsCacheEntry(c)).Value.Value;
    private static readonly ConditionalWeakTable<Compilation, PageExtensionsCacheEntry> PageExtensionsCache = new();
    private sealed class PageExtensionsCacheEntry(Compilation compilation)
    {
        public Lazy<ImmutableArray<IPageExtensionBaseTypeSymbol>> Value { get; } =
            new Lazy<ImmutableArray<IPageExtensionBaseTypeSymbol>>(
                () => compilation
                    .GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(EnumProvider.SymbolKind.PageExtension)
                    .OfType<IPageExtensionBaseTypeSymbol>()
                    .ToImmutableArray(),
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.DuplicateODataEntityName);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeSymbol,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.PageExtension);

    private void AnalyzeSymbol(SymbolAnalysisContext ctx)
    {
        if (!ODataNameHelper.IsAvailable)
            return;

        if (ctx.IsObsolete())
            return;

        switch (ctx.Symbol)
        {
            case IPageBaseTypeSymbol page:
                AnalyzePage(ctx, page);
                break;
            case IPageExtensionBaseTypeSymbol pageExtension:
                AnalyzePageExtension(ctx, pageExtension);
                break;
        }
    }

    private static void AnalyzePage(SymbolAnalysisContext ctx, IPageBaseTypeSymbol page)
    {
        if (!RelevantPageTypes.Contains(page.PageType))
            return;

        var controlEntries = CollectFieldControlEntries(page.FlattenedControls);
        var pkEntries = CollectPrimaryKeyEntries(page.RelatedTable);

        var allEntries = new List<ODataNameEntry>(controlEntries.Count + pkEntries.Count);
        allEntries.AddRange(controlEntries);
        allEntries.AddRange(pkEntries);

        ReportDuplicates(ctx, allEntries, reportableFilter: null);
    }

    private static void AnalyzePageExtension(SymbolAnalysisContext ctx, IPageExtensionBaseTypeSymbol pageExtension)
    {
        var targetPage = pageExtension.Target?.OriginalDefinition as IPageBaseTypeSymbol;
        if (targetPage is null || !RelevantPageTypes.Contains(targetPage.PageType))
            return;

        var extensionControls = CollectFieldControlEntries(pageExtension.AddedControlsFlattened);
        if (extensionControls.Count == 0)
            return;

        var baseControls = CollectFieldControlEntries(targetPage.FlattenedControls);
        var pkEntries = CollectPrimaryKeyEntries(targetPage.RelatedTable);

        // Collect controls from sibling page extensions targeting the same base page
        var siblingControls = CollectSiblingExtensionControls(ctx, pageExtension, targetPage);

        var allEntries = new List<ODataNameEntry>(
            extensionControls.Count + baseControls.Count + pkEntries.Count + siblingControls.Count);
        allEntries.AddRange(extensionControls);
        allEntries.AddRange(baseControls);
        allEntries.AddRange(pkEntries);
        allEntries.AddRange(siblingControls);

        // Only report diagnostics on the extension's own controls
        var extensionControlSet = new HashSet<IControlSymbol>(
            extensionControls.Where(e => e.Control is not null).Select(e => e.Control!));

        ReportDuplicates(ctx, allEntries, reportableFilter: entry => entry.Control is not null && extensionControlSet.Contains(entry.Control));
    }

    private static List<ODataNameEntry> CollectSiblingExtensionControls(
        SymbolAnalysisContext ctx,
        IPageExtensionBaseTypeSymbol currentExtension,
        IPageBaseTypeSymbol targetPage)
    {
        var allPageExtensions = GetCachedPageExtensions(ctx.Compilation);
        var entries = new List<ODataNameEntry>();

        foreach (var ext in allPageExtensions)
        {
            if (ReferenceEquals(ext, currentExtension))
                continue;

            if (!SameApplicationObject(ext.Target?.OriginalDefinition, targetPage))
                continue;

            foreach (var control in ext.AddedControlsFlattened)
            {
                if (control.ControlKind != EnumProvider.ControlKind.Field)
                    continue;

                var odataName = ODataNameHelper.MangleIntoValidXmlIdentifier(control.Name);
                if (odataName is null)
                    continue;

                // Use null Control so sibling controls are not reportable
                entries.Add(new ODataNameEntry(odataName, control.Name, control.GetLocation(), null));
            }
        }

        return entries;
    }

    private static bool SameApplicationObject(ISymbol? source, ISymbol? target)
    {
        if (source is null || target is null)
            return false;

        source = source.OriginalDefinition;
        target = target.OriginalDefinition;

        if (ReferenceEquals(source, target))
            return true;

        if (source is ISymbolWithId lId && target is ISymbolWithId rId)
            return lId.Id == rId.Id && source.Kind == target.Kind;

        return source.Equals(target);
    }

    private static List<ODataNameEntry> CollectFieldControlEntries(ImmutableArray<IControlSymbol> controls)
    {
        var entries = new List<ODataNameEntry>();
        foreach (var control in controls)
        {
            if (control.ControlKind != EnumProvider.ControlKind.Field)
                continue;

            var odataName = ODataNameHelper.MangleIntoValidXmlIdentifier(control.Name);
            if (odataName is null)
                continue;

            entries.Add(new ODataNameEntry(odataName, control.Name, control.GetLocation(), control));
        }
        return entries;
    }

    private static List<ODataNameEntry> CollectPrimaryKeyEntries(ITableTypeSymbol? table)
    {
        var entries = new List<ODataNameEntry>();
        if (table?.PrimaryKey is null)
            return entries;

        foreach (var field in table.PrimaryKey.Fields)
        {
            var odataName = ODataNameHelper.MangleIntoValidXmlIdentifier(field.Name);
            if (odataName is null)
                continue;

            entries.Add(new ODataNameEntry(odataName, field.Name, field.GetLocation(), null));
        }
        return entries;
    }

    private static void ReportDuplicates(
        SymbolAnalysisContext ctx,
        List<ODataNameEntry> entries,
        Func<ODataNameEntry, bool>? reportableFilter)
    {
        var groups = entries
            .GroupBy(e => e.ODataName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            foreach (var entry in group)
            {
                if (reportableFilter is not null && !reportableFilter(entry))
                    continue;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateODataEntityName,
                    entry.Location,
                    entry.OriginalName,
                    group.Key));
            }
        }
    }


#if NETSTANDARD2_1
    private readonly struct ODataNameEntry
    {
        public string ODataName { get; }
        public string OriginalName { get; }
        public Location Location { get; }
        public IControlSymbol? Control { get; }

        public ODataNameEntry(string odataName, string originalName, Location location, IControlSymbol? control)
        {
            ODataName = odataName;
            OriginalName = originalName;
            Location = location;
            Control = control;
        }
    }
#else
    private readonly record struct ODataNameEntry(string ODataName, string OriginalName, Location Location, IControlSymbol? Control);
#endif
}
