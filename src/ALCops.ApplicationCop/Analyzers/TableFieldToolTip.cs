using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TableFieldToolTip : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableFieldToolTipShouldBeDefined,
            DiagnosticDescriptors.DuplicateToolTipBetweenPageAndTable);

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Spring2024OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeToolTipProperty,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.PageExtension);

    private void AnalyzeToolTipProperty(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var controls = ctx.Symbol.GetFlattenedControls();
        if (controls is null)
            return;

        foreach (var control in controls)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (control.ControlKind != EnumProvider.ControlKind.Field)
                continue;

            var relatedField = control.RelatedFieldSymbol;
            if (relatedField is null)
                continue;

            var pageToolTip = control.GetProperty(EnumProvider.PropertyKind.ToolTip);
            if (pageToolTip is null)
                continue;

            var tableToolTip = control.RelatedFieldSymbol?.GetProperty(EnumProvider.PropertyKind.ToolTip);

            // Page field has a value for the ToolTip property and table field does not have a value for the ToolTip property
            if (tableToolTip is null && relatedField.IsSourceSymbol())
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TableFieldToolTipShouldBeDefined,
                    pageToolTip.GetLocation(),
                    relatedField.Name,
                    control.Name));

                continue;
            }

            // Page field has a value for the ToolTip property and table field also has a value for the ToolTip property but the value is exactly the same
            if (tableToolTip is not null && string.Equals(pageToolTip.ValueText, tableToolTip.ValueText, StringComparison.Ordinal))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateToolTipBetweenPageAndTable,
                    pageToolTip.GetLocation(),
                    control.Name,
                    relatedField.Name));
            }
        }
    }
}