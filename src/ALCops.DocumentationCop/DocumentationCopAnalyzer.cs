using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using ALCops.Common.Diagnostics;

namespace ALCops.DocumentationCop;

/// <summary>
/// Base class for every DocumentationCop analyzer. Supplies the cop-specific
/// <see cref="DiagnosticDescriptors.AnalyzerException"/> (DC0000) reported when a
/// rule throws an unhandled exception. See <see cref="ALCopsDiagnosticAnalyzer"/>.
/// </summary>
public abstract class DocumentationCopAnalyzer : ALCopsDiagnosticAnalyzer
{
    protected sealed override DiagnosticDescriptor AnalyzerExceptionDescriptor =>
        DiagnosticDescriptors.AnalyzerException;
}
