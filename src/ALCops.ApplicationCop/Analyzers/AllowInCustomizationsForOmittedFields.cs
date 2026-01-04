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
        context.RegisterSymbolAction(
            AnalyzeSymbol,
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension);

    private static void AnalyzeSymbol(SymbolAnalysisContext ctx)
    {
        if (!VersionChecker.IsSupported(ctx.Symbol, EnumProvider.Feature.AddPageControlInPageCustomization))
            return;

        if (ctx.IsObsolete())
            return;

        if (!TryGetTableOrTargetTable(ctx.Symbol, out var table, out var isTableExtension))
            return;

        var candidateFields = GetCandidateFields(ctx.Symbol);
        if (candidateFields.Count == 0)
            return;

        var relatedPages = GetRelatedPages(ctx.Compilation, table);

        // Allow diagnostic if base table has Lookup/DrillDown page set even if no related pages exist directly
        if (!relatedPages.Any())
        {
            if (!isTableExtension)
                return;

            if (!BaseTableHasLookupOrDrillDown(table))
                return;
        }

        var referencedOnPages = GetReferencedPageFields(relatedPages);

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

    private static bool TryGetTableOrTargetTable(ISymbol symbol, out ITableTypeSymbol table, out bool isTableExtension)
    {
        table = null!;
        isTableExtension = false;

        var navKind = symbol.GetContainingObjectTypeSymbol().GetNavTypeKindSafe();

        if (navKind == EnumProvider.NavTypeKind.Record && symbol is ITableTypeSymbol t)
        {
            table = t;
            return true;
        }

        if (navKind == EnumProvider.NavTypeKind.TableExtension && symbol is IApplicationObjectExtensionTypeSymbol ext)
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
        var fields = GetDeclaredTableFields(symbol);
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

            var obsoleteState = field.GetEnumPropertyValue<ObsoleteStateKind>(EnumProvider.PropertyKind.ObsoleteState);
            if (obsoleteState is not null &&
               obsoleteState.Value != EnumProvider.ObsoleteStateKind.No)
            {
                continue;
            }

            var navTypeKind = field.OriginalDefinition.GetTypeSymbol().GetNavTypeKindSafe();
            if (!IsSupportedType(navTypeKind))
                continue;

            result.Add(field);
        }

        return result;
    }

    private static ICollection<IFieldSymbol> GetDeclaredTableFields(ISymbol symbol)
    {
        var navKind = symbol.GetContainingObjectTypeSymbol().GetNavTypeKindSafe();

        if (navKind == EnumProvider.NavTypeKind.Record && symbol is ITableTypeSymbol table)
            return table.Fields;

        if (navKind == EnumProvider.NavTypeKind.TableExtension && symbol is ITableExtensionTypeSymbol tableExt)
            return tableExt.AddedFields;

        return Array.Empty<IFieldSymbol>();
    }

    private static IReadOnlyList<IApplicationObjectTypeSymbol> GetRelatedPages(Compilation compilation, ITableTypeSymbol table)
    {
        // Materialize once, then filter. Avoid repeated enumeration of declared symbols.
        var declared = compilation.GetDeclaredApplicationObjectSymbols().ToList();

        var pages =
            declared.Where(x => x.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Page)
                    .Select(x => (IApplicationObjectTypeSymbol)x)
                    .Where(x =>
                    {
                        var page = (IPageTypeSymbol)x.GetTypeSymbol();
                        return page.PageType != PageTypeKind.API && page.RelatedTable == table;
                    });

        var pageExtensions =
            declared.Where(x => x.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.PageExtension)
                    .Select(x => (IApplicationObjectTypeSymbol)x)
                    .Where(x =>
                    {
                        if (x is not IApplicationObjectExtensionTypeSymbol ext || ext.Target is null)
                            return false;

                        var targetPage = (IPageTypeSymbol)ext.Target.GetTypeSymbol();
                        return targetPage.RelatedTable == table;
                    });

        return pages.Concat(pageExtensions).ToList();
    }

    private static HashSet<IFieldSymbol> GetReferencedPageFields(IEnumerable<IApplicationObjectTypeSymbol> relatedPages)
    {
        var set = new HashSet<IFieldSymbol>();

        foreach (var pageLike in relatedPages)
        {
            var navKind = pageLike.GetNavTypeKindSafe();

            if (navKind == EnumProvider.NavTypeKind.Page && pageLike is IPageTypeSymbol page)
            {
                AddFieldControls(set, page.FlattenedControls);
                continue;
            }

            if (navKind == EnumProvider.NavTypeKind.PageExtension && pageLike is IPageExtensionTypeSymbol pageExt)
            {
                AddFieldControls(set, pageExt.AddedControlsFlattened);
                continue;
            }
        }

        return set;
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
