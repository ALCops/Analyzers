using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class CompilationExtensions
{
    // The method GetApplicationObjectTypeSymbolsByIdAcrossModules(SymbolKind kind, int id) in the class Compilation is internal so we need to use reflection for this.
    public static ImmutableArray<IApplicationObjectTypeSymbol> GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(this Compilation compilation, SymbolKind kind, int id)
        => CompilationHelper.GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(compilation, kind, id);
}