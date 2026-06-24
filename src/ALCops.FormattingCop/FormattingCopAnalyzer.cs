using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using ALCops.Common.Diagnostics;

namespace ALCops.FormattingCop;

/// <summary>
/// Base class for every FormattingCop analyzer. Supplies the cop-specific
/// <see cref="DiagnosticDescriptors.AnalyzerException"/> (FC0000) reported when a
/// rule throws an unhandled exception. See <see cref="ALCopsDiagnosticAnalyzer"/>.
/// </summary>
public abstract class FormattingCopAnalyzer : ALCopsDiagnosticAnalyzer
{
    protected sealed override DiagnosticDescriptor AnalyzerExceptionDescriptor =>
        DiagnosticDescriptors.AnalyzerException;
}
