using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public class ApplicationAreaRedundancy : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ApplicationAreaRedundancy);
    public override VersionCompatibility SupportedVersions { get; } =
        VersionProvider.VersionCompatibility.Fall2022OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            CheckDataClassificationRedundancy,
                EnumProvider.SymbolKind.Control);

    private void CheckDataClassificationRedundancy(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IControlSymbol control)
            return;

        IApplicationObjectTypeSymbol? applicationObject = control.GetContainingApplicationObjectTypeSymbol();
        if (applicationObject is not IPageTypeSymbol page || applicationObject.IsObsolete())
            return;

        IPropertySymbol? controlApplicationArea = control.GetProperty(EnumProvider.PropertyKind.ApplicationArea);
        if (controlApplicationArea is null)
            return;

        IPropertySymbol? pageApplicationArea = page.GetProperty(EnumProvider.PropertyKind.ApplicationArea);
        if (pageApplicationArea is null)
            return;

        if (!string.Equals(pageApplicationArea.ValueText, controlApplicationArea.ValueText, StringComparison.OrdinalIgnoreCase))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ApplicationAreaRedundancy,
            controlApplicationArea.GetLocation()));
    }
}