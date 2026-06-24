using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using ALCops.Common.Diagnostics;

namespace ALCops.LinterCop;

/// <summary>
/// Base class for every LinterCop analyzer. Supplies the cop-specific
/// <see cref="DiagnosticDescriptors.AnalyzerException"/> (LC0000) reported when a
/// rule throws an unhandled exception. See <see cref="ALCopsDiagnosticAnalyzer"/>.
/// </summary>
public abstract class LinterCopAnalyzer : ALCopsDiagnosticAnalyzer
{
    protected sealed override DiagnosticDescriptor AnalyzerExceptionDescriptor =>
        DiagnosticDescriptors.AnalyzerException;
}
