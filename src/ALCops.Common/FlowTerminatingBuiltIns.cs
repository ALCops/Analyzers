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
    /// Returns whether <paramref name="operation"/> calls a built-in AL method that never returns control.
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
    public static bool IsFlowTerminatingCall(IOperation? operation) =>
        operation is IInvocationExpression { TargetMethod: IMethodSymbol method } &&
        (IsKnownBuiltInMethod(method) ||
         (operation.IsInvalid && IsKnownInvalidBinding(method)));

    private static bool IsKnownBuiltInMethod(IMethodSymbol method) =>
        method.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
        method.ContainingSymbol is IClassTypeSymbol containingClass &&
        (IsErrorOnDialog(method.Name, containingClass.Name) ||
         IsFieldErrorOn(method.Name, containingClass.Name));

    private static bool IsKnownInvalidBinding(IMethodSymbol method) =>
        method.ContainingSymbol is ITypeSymbol containingType &&
        (IsSameName(method.Name, ErrorMethodName) &&
         containingType.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Dialog ||
         IsSameName(method.Name, FieldErrorMethodName) &&
         (containingType.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Record ||
          containingType.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.FieldRef));

    private static bool IsErrorOnDialog(string methodName, string className) =>
        IsSameName(methodName, ErrorMethodName) && IsSameName(className, DialogClassName);

    private static bool IsFieldErrorOn(string methodName, string className) =>
        IsSameName(methodName, FieldErrorMethodName) &&
        (IsSameName(className, TableClassName) || IsSameName(className, FieldRefClassName));

    private static bool IsSameName(string left, string right) =>
        SemanticFacts.NameEqualityComparer.Equals(left, right);
}