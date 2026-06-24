using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using ALCops.Common.Diagnostics;

namespace ALCops.PlatformCop;

/// <summary>
/// Base class for every PlatformCop analyzer. Supplies the cop-specific
/// <see cref="DiagnosticDescriptors.AnalyzerException"/> (PC0000) reported when a
/// rule throws an unhandled exception. See <see cref="ALCopsDiagnosticAnalyzer"/>.
/// </summary>
public abstract class PlatformCopAnalyzer : ALCopsDiagnosticAnalyzer
{
    protected sealed override DiagnosticDescriptor AnalyzerExceptionDescriptor =>
        DiagnosticDescriptors.AnalyzerException;
}
