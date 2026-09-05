using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common;

/// <summary>
/// Identifies built-in AL methods that unconditionally end execution of the calling code.
/// </summary>
public static class FlowTerminatingBuiltIns
{
    private const string ErrorMethodName = "Error";
    private const string FieldErrorMethodName = "FieldError";
    private const string DialogClassName = "Dialog";
    private const string TableClassName = "Table";
    private const string FieldRefClassName = "FieldRef";

    /// <summary>
    /// Gets the canonical name of the built-in AL method called by <paramref name="operation"/>,
    /// when that method never returns control.
    /// </summary>
    /// <remarks>
    /// Valid calls must match the exact built-in class and method pair: <c>Dialog.Error</c>,
    /// <c>Table.FieldError</c>, or <c>FieldRef.FieldError</c>. Invalid calls are accepted only for
    /// the corresponding built-in receiver types the binder synthesizes while one of these calls is
    /// incomplete. This prevents analyzer flicker while preserving the distinction from user-defined
    /// methods with the same name.
    /// </remarks>
    /// <remarks>
    /// Collectible <c>Error(ErrorInfo)</c> calls can return when invoked in an
    /// <c>ErrorBehavior::Collect</c> scope. This method currently classifies them as terminating
    /// because the invocation alone does not expose the surrounding collection behavior.
    /// </remarks>
    /// <returns>The canonical built-in method name, or <c>null</c> when the operation can return control.</returns>
    public static string? GetFlowTerminatingBuiltInName(IOperation? operation)
    {
        if (operation is not IInvocationExpression { TargetMethod: IMethodSymbol method } ||
            (!IsKnownBuiltInMethod(method) &&
             (!operation.IsInvalid || !IsKnownInvalidBinding(method))))
        {
            return null;
        }

        return SemanticFacts.IsSameName(method.Name, ErrorMethodName)
            ? ErrorMethodName
            : FieldErrorMethodName;
    }

    /// <summary>
    /// Returns whether <paramref name="operation"/> calls a built-in AL method that never returns control.
    /// </summary>
    public static bool IsFlowTerminatingCall(IOperation? operation) =>
        GetFlowTerminatingBuiltInName(operation) is not null;

    private static bool IsKnownBuiltInMethod(IMethodSymbol method) =>
        method.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
        method.ContainingSymbol is IClassTypeSymbol containingClass &&
        ((SemanticFacts.IsSameName(method.Name, ErrorMethodName) &&
          SemanticFacts.IsSameName(containingClass.Name, DialogClassName)) ||
         (SemanticFacts.IsSameName(method.Name, FieldErrorMethodName) &&
          (SemanticFacts.IsSameName(containingClass.Name, TableClassName) ||
           SemanticFacts.IsSameName(containingClass.Name, FieldRefClassName))));

    private static bool IsKnownInvalidBinding(IMethodSymbol method)
    {
        if (method.ContainingSymbol is not ITypeSymbol containingType)
        {
            return false;
        }

        var containingTypeKind = containingType.GetNavTypeKindSafe();

        return (SemanticFacts.IsSameName(method.Name, ErrorMethodName) &&
                containingTypeKind == EnumProvider.NavTypeKind.Dialog) ||
               (SemanticFacts.IsSameName(method.Name, FieldErrorMethodName) &&
                (containingTypeKind == EnumProvider.NavTypeKind.Record ||
                 containingTypeKind == EnumProvider.NavTypeKind.FieldRef));
    }
}