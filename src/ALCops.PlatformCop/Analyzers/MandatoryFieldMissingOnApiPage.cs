using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class MandatoryFieldMissingOnApiPage : DiagnosticAnalyzer
{
    private static readonly ImmutableDictionary<string, string> MandatoryFields =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SystemId"] = "id",
            ["SystemModifiedAt"] = "lastModifiedDateTime",
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.MandatoryFieldMissingOnApiPage);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeRule0062MandatoryFieldOnApiPage,
            EnumProvider.SymbolKind.Page);

    private void AnalyzeRule0062MandatoryFieldOnApiPage(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IPageTypeSymbol pageTypeSymbol)
            return;

        if (pageTypeSymbol.PageType != EnumProvider.PageTypeKind.API)
            return;

        if (pageTypeSymbol.GetBooleanPropertyValue(EnumProvider.PropertyKind.SourceTableTemporary).GetValueOrDefault())
            return;

        var fieldControls = pageTypeSymbol.FlattenedControls
            .Where(c => c.ControlKind == EnumProvider.ControlKind.Field && c.RelatedFieldSymbol is not null)
            .ToArray();

        if (fieldControls.Length == 0)
            return;

        var exposed = new HashSet<(string FieldName, string ControlName)>();
        foreach (var c in fieldControls)
        {
            var fieldName = c.RelatedFieldSymbol!.Name;
            var controlName = c.Name;

            if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(controlName))
                exposed.Add((fieldName, controlName));
        }

        foreach (var mf in MandatoryFields)
        {
            if (exposed.Contains((mf.Key, mf.Value)))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MandatoryFieldMissingOnApiPage,
                pageTypeSymbol.GetLocation(),
                mf.Key,
                mf.Value));
        }
    }
}