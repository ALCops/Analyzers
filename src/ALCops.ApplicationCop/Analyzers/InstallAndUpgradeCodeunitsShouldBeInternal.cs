using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class InstallAndUpgradeCodeunitsShouldBeInternal : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.InstallAndUpgradeCodeunitsShouldBeInternal);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            this.CheckAccessOnInstallAndUpgradeCodeunits,
            EnumProvider.SymbolKind.Codeunit);

    private void CheckAccessOnInstallAndUpgradeCodeunits(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not ICodeunitTypeSymbol symbol)
            return;

        var subtype = symbol.Subtype;
        if (subtype != EnumProvider.CodeunitSubtypeKind.Install &&
            subtype != EnumProvider.CodeunitSubtypeKind.Upgrade)
            return;

        if (symbol.DeclaredAccessibility != EnumProvider.Accessibility.Public)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InstallAndUpgradeCodeunitsShouldBeInternal,
            symbol.GetLocation()));
    }
}
