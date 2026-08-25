using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common;

/// <summary>
/// Single source of truth for built-in AL methods that unconditionally end execution of the
/// calling code (<c>Error</c>, <c>FieldError</c>). Consumed by PC0038, LC0089/LC0090, and FC0007
/// so their terminator sets cannot drift apart.
/// </summary>
public static class FlowTerminatingBuiltIns
{
    /// <summary>
    /// Names of built-in methods that never return control to the caller.
    /// </summary>
    public static ImmutableHashSet<string> MethodNames { get; } =
        ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, "Error", "FieldError");

    /// <summary>
    /// Returns true when <paramref name="method"/> is a built-in method whose name is a known
    /// flow terminator. User-defined procedures sharing the same name never match, because they
    /// have <see cref="MethodKind.BuiltInMethod"/> not set.
    /// </summary>
    public static bool IsFlowTerminatingCall(IMethodSymbol? method) =>
        method is not null &&
        method.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
        MethodNames.Contains(method.Name);
}
