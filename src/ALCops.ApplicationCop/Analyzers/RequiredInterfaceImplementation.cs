using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class RequiredInterfaceImplementation : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.RestClientRequiresHttpClientHandler,
            DiagnosticDescriptors.TelemetryRequiresTelemetryLogger);

    public override void Initialize(AnalysisContext context)
    {
    }
}
