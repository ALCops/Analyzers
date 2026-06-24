using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using ALCops.Common.Diagnostics;

namespace ALCops.TestAutomationCop;

/// <summary>
/// Base class for every TestAutomationCop analyzer. Supplies the cop-specific
/// <see cref="DiagnosticDescriptors.AnalyzerException"/> (TA0000) reported when a
/// rule throws an unhandled exception. See <see cref="ALCopsDiagnosticAnalyzer"/>.
/// </summary>
public abstract class TestAutomationCopAnalyzer : ALCopsDiagnosticAnalyzer
{
    protected sealed override DiagnosticDescriptor AnalyzerExceptionDescriptor =>
        DiagnosticDescriptors.AnalyzerException;
}
