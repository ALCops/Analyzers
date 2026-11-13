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

    public static ImmutableArray<IApplicationObjectTypeSymbol> GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(this Compilation compilation, SymbolKind kind)
        => CompilationHelper.GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(compilation, kind);

    public static ImmutableArray<IApplicationObjectTypeSymbol> GetApplicationObjectTypeSymbolsByNameAcrossModulesWithReflection(this Compilation compilation, SymbolKind kind, string name)
        => CompilationHelper.GetApplicationObjectTypeSymbolsByNameAcrossModulesWithReflection(compilation, kind, name);

    /// <summary>
    /// The namespace of the first object of <paramref name="kind"/> named <paramref name="name"/>, searching the
    /// compiling module first and then each referenced module. Null when no object matches or it is declared
    /// in the global namespace.
    /// </summary>
    public static string? GetApplicationObjectNamespaceWithReflection(this Compilation compilation, SymbolKind kind, string name)
    {
        foreach (var symbol in compilation.GetApplicationObjectTypeSymbolsByNameAcrossModulesWithReflection(kind, name))
        {
            if (!symbol.Name.IsSameName(name))
                continue;

            var namespaceName = symbol.GetContainingNamespaceQualifiedNameWithReflection();
            return string.IsNullOrEmpty(namespaceName) ? null : namespaceName;
        }

        return null;
    }

    public static bool IsDiagnosticEnabled(this Compilation compilation, DiagnosticDescriptor descriptor)
    {
        if (compilation.Options.SpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var report))
            return report != ReportDiagnostic.Suppress;

        return true;
    }
}