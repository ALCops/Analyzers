using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common;

/// <summary>
/// Identifies built-in AL methods that unconditionally end execution of the calling code.
/// </summary>
public static class FlowTerminatingBuiltIns
{
    private static readonly ImmutableHashSet<string> MethodNames =
        ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, "Error", "FieldError");

    /// <summary>
    /// Returns whether <paramref name="operation"/> calls a built-in AL method that never returns control.
    /// </summary>
    /// <remarks>
    /// Invalid calls are accepted only for the built-in receiver types the binder synthesizes while
    /// an <c>Error</c> or <c>FieldError</c> call is incomplete. This prevents analyzer flicker while
    /// preserving the distinction from user-defined methods with the same name.
    /// </remarks>
    public static bool IsFlowTerminatingCall(IOperation? operation) =>
        operation is IInvocationExpression { TargetMethod: IMethodSymbol method } &&
        MethodNames.Contains(method.Name) &&
        (method.MethodKind == EnumProvider.MethodKind.BuiltInMethod ||
         (operation.IsInvalid && IsBuiltInReceiver(method.ContainingSymbol)));

    private static bool IsBuiltInReceiver(ISymbol? containingSymbol) =>
        containingSymbol is ITypeSymbol type &&
        (type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Dialog ||
         type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Record ||
         type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.FieldRef);
}