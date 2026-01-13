using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ApplicationAreaOnApiPage : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ApplicationAreaOnApiPage);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzePropertyApplicationAreaOnApiPage,
            EnumProvider.SymbolKind.Page);

    private void AnalyzePropertyApplicationAreaOnApiPage(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IPageTypeSymbol pageTypeSymbol)
            return;

        if (pageTypeSymbol.PageType != EnumProvider.PageTypeKind.API)
            return;

        if (pageTypeSymbol.GetProperty(EnumProvider.PropertyKind.ApplicationArea) is IPropertySymbol propertyOnPageObject)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ApplicationAreaOnApiPage,
                propertyOnPageObject.GetLocation()));
        }

        foreach (var control in pageTypeSymbol.FlattenedControls)
        {
            if (control.ControlKind != EnumProvider.ControlKind.Field)
                continue;

            if (control.GetProperty(EnumProvider.PropertyKind.ApplicationArea) is not IPropertySymbol property)
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ApplicationAreaOnApiPage,
                property.GetLocation()));
        }
    }
}