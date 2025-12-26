using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ExtensiblePropertyExplicitlySet : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ExtensiblePropertyExplicitlySet);

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Fall2019OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            this.AnalyzeExtensibleProperty,
                EnumProvider.SymbolKind.Table,
                EnumProvider.SymbolKind.Page,
                EnumProvider.SymbolKind.Report
        );

    private void AnalyzeExtensibleProperty(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var typeSymbol = ctx.Symbol.GetTypeSymbol();
        if (typeSymbol is null)
            return;

        if (typeSymbol.Kind == EnumProvider.SymbolKind.Table &&
            ctx.Symbol.DeclaredAccessibility != EnumProvider.Accessibility.Public)
            return;

        if (typeSymbol.Kind == EnumProvider.SymbolKind.Page &&
            typeSymbol is IPageTypeSymbol page &&
            page.PageType == EnumProvider.PageTypeKind.API)
            return;

        if (ctx.Symbol.GetProperty(EnumProvider.PropertyKind.Extensible) is not null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ExtensiblePropertyExplicitlySet,
            ctx.Symbol.GetLocation()));
    }
}