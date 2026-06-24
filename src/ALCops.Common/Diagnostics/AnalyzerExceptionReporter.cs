using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.Common.Diagnostics;

/// <summary>
/// Builds the <c>XX0000</c> diagnostic reported when an analyzer callback throws
/// an unhandled exception. The message mirrors the useful part of the SDK's
/// <c>AD0001</c> text but is attached to the real location.
/// </summary>
internal static class AnalyzerExceptionReporter
{
    public static Diagnostic CreateDiagnostic(
        DiagnosticDescriptor descriptor,
        Location? location,
        string analyzerName,
        Exception exception) =>
        Diagnostic.Create(
            descriptor,
            location ?? Location.None,
            analyzerName,
            exception.GetType().ToString(),
            exception.Message);
}
