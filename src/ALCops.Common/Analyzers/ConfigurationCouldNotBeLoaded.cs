using System.Collections.Immutable;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.Common.Analyzers;

// Derives directly from the SDK DiagnosticAnalyzer: alc resolves an analyzer's base type only
// from the compiler's own assemblies or from already-loaded analyzer references (a base class in
// a sibling DLL fails type-load with AL1003), and ALCops.Common.dll is itself an analyzer
// reference on every documented install path, so an analyzer hosted here loads like any cop.
[DiagnosticAnalyzer]
public sealed class ConfigurationCouldNotBeLoaded : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.ConfigurationCouldNotBeLoaded);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationAction(ReportConfigurationLoadFailures);

    private static void ReportConfigurationLoadFailures(CompilationAnalysisContext ctx)
    {
        // Compilation-level actions run under every partial-analysis pass, and the settings
        // cache keeps load failures, so each compilation re-reports them here.
        var result = ALCopsSettingsProvider.GetLoadResult(ctx.Compilation.FileSystem);
        foreach (var failure in result.Failures)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ConfigurationCouldNotBeLoaded,
                Location.None,
                failure.Source,
                failure.Detail));
        }
    }
}
