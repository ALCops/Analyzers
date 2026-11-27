using System.Collections.Immutable;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class AppManifestPublisher : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.AppManifestPublisher);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationAction(AnalyzeCompilation);

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        ManifestSettings settings = ALCopsSettingsProvider.GetSettings(context.Compilation, context.CancellationToken).Manifest;
        if (!AppManifestRuleHelper.HasConfiguredPattern(settings.Publisher))
            return;

        var manifest = AppManifestRuleHelper.GetManifest(context);
        if (manifest is null || !AppManifestRuleHelper.ViolatesPattern(manifest.AppPublisher ?? string.Empty, settings.Publisher!, out string reason))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AppManifestPublisher,
            manifest.GetDiagnosticLocation("publisher"),
            $"'{manifest.AppPublisher}' {reason}"));
    }
}
