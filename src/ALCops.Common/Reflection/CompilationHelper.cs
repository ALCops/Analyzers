using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Reflection;

public static class CompilationHelper
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T GetNonPublicProp<T>(object obj, string name) where T : class
        => (obj.GetType().GetProperty(name, Flags)?.GetValue(obj) as T)!;

    internal static IReferenceManager GetReferenceManager(Compilation compilation)
        => GetNonPublicProp<IReferenceManager>(compilation, "ReferenceManager");

    internal static IModuleSymbol GetCompiledModule(Compilation compilation)
        // CompiledModule is ModuleSymbol; it implements IModuleSymbol
        => (IModuleSymbol)GetNonPublicProp<object>(compilation, "CompiledModule");

    public static ImmutableArray<IApplicationObjectTypeSymbol> GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(Compilation compilation, SymbolKind kind, int id)
    {
        var referenceManager = GetReferenceManager(compilation);
        var referencingModule = GetCompiledModule(compilation);

        var symbolWithId = referenceManager.GetObjectSymbolsByIdAcrossModules(referencingModule, kind, id);

        return symbolWithId.OfType<IApplicationObjectTypeSymbol>().ToImmutableArray();
    }

    public static ImmutableArray<IApplicationObjectTypeSymbol> GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(Compilation compilation, SymbolKind kind)
    {
        var referenceManager = GetReferenceManager(compilation);
        var referencingModule = GetCompiledModule(compilation);

        var symbolWithId = referenceManager.GetObjectSymbolsByKindAcrossModules(referencingModule, kind);

        return symbolWithId.OfType<IApplicationObjectTypeSymbol>().ToImmutableArray();
    }
}