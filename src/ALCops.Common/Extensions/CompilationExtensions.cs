using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.Common.Extensions;

public static class CompilationExtensions
{
    // The method GetApplicationObjectTypeSymbolsByIdAcrossModules(SymbolKind kind, int id) in the class Compilation is internal so we need to use reflection for this.
    public static ImmutableArray<IApplicationObjectTypeSymbol> GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(this Compilation compilation, SymbolKind kind, int id)
        => CompilationHelper.GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(compilation, kind, id);

    public static bool IsDiagnosticEnabled(this Compilation compilation, DiagnosticDescriptor descriptor)
    {
        if (compilation.Options.SpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var report))
            return report != ReportDiagnostic.Suppress;

        return true;
    }
}