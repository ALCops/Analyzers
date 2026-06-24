using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.Common.Diagnostics;

/// <summary>
/// Base class for every ALCops analyzer. It transparently wraps the analysis
/// callbacks so that an unhandled exception thrown by a rule is reported as a
/// located <c>XX0000</c> diagnostic (see <see cref="AnalyzerExceptionDescriptor"/>)
/// instead of surfacing as the SDK's <c>AD0001</c> on <c>app.json</c> line 1.
///
/// Analyzers derive from the per-cop bridge (for example
/// <c>ApplicationCopAnalyzer</c>) rather than from this type directly, override
/// <see cref="SupportedDiagnosticsCore"/> instead of
/// <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/>, and override
/// <see cref="InitializeAnalyzer"/> instead of
/// <see cref="DiagnosticAnalyzer.Initialize"/>.
/// </summary>
public abstract class ALCopsDiagnosticAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The cop-specific <c>XX0000</c> descriptor reported when a rule in this cop
    /// throws an unhandled exception. Supplied by the per-cop bridge class.
    /// </summary>
    protected abstract DiagnosticDescriptor AnalyzerExceptionDescriptor { get; }

    /// <summary>
    /// The descriptors produced by the concrete analyzer. The
    /// <see cref="AnalyzerExceptionDescriptor"/> is appended automatically, so
    /// derived analyzers must not list it themselves.
    /// </summary>
    protected abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCore { get; }

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            ImmutableArray<DiagnosticDescriptor> core = SupportedDiagnosticsCore;
            return core.Contains(AnalyzerExceptionDescriptor)
                ? core
                : core.Add(AnalyzerExceptionDescriptor);
        }
    }

    public sealed override void Initialize(AnalysisContext context) =>
        InitializeAnalyzer(new SafeAnalysisContext(context, AnalyzerExceptionDescriptor, GetType().Name));

    /// <summary>
    /// Registers the analyzer's actions. The supplied <see cref="SafeAnalysisContext"/>
    /// wraps every registered callback so unhandled exceptions become located
    /// <c>XX0000</c> diagnostics.
    /// </summary>
    protected abstract void InitializeAnalyzer(SafeAnalysisContext context);
}
