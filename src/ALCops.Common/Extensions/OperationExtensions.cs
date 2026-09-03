using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

/// <summary>
/// Extension methods for <see cref="IOperation"/> that provide safe alternatives
/// to SDK methods with known bugs.
/// </summary>
public static class OperationSafeExtensions
{
    /// <summary>
    /// Safe replacement for the SDK's <c>OperationExtensions.GetSymbol()</c> which
    /// crashes with <see cref="InvalidCastException"/> on certain bound types.
    /// <para>
    /// The SDK's method switches on <c>OperationKind.FieldAccess</c> and casts to
    /// <see cref="IFieldAccess"/>, but <c>BoundApplicationObjectAccess</c> (for
    /// <c>DATABASE::X</c>, <c>CODEUNIT::X</c> etc.) and <c>BoundObjectAccess</c>
    /// both report <c>FieldAccess</c> kind while implementing different interfaces.
    /// </para>
    /// <para>
    /// This method handles <see cref="IApplicationObjectAccess"/> (public SDK interface)
    /// and <see cref="IOptionAccess"/> by returning their directly exposed symbols, and guards against any other
    /// <c>FieldAccess</c>-kind operations that don't implement <see cref="IFieldAccess"/>
    /// by returning <c>null</c>.
    /// </para>
    /// </summary>
    /// <param name="operation">The operation to resolve.</param>
    /// <returns>The resolved symbol, or <c>null</c> if the operation type is unsupported.</returns>
    public static ISymbol? GetSymbolSafe(this IOperation operation)
    {
        // BoundApplicationObjectAccess: reports FieldAccess kind but implements
        // IApplicationObjectAccess (DATABASE::X, CODEUNIT::X, TABLE::X, etc.)
        if (operation is IApplicationObjectAccess appObjAccess)
            return appObjAccess.ApplicationObjectTypeSymbol;

        if (operation is IOptionAccess optionAccess)
            return optionAccess.OptionSymbol;

        // Guard against BoundObjectAccess and any future types that report FieldAccess
        // but don't implement IFieldAccess. IObjectAccess is internal so we can't check
        // it directly; the is-not-IFieldAccess guard catches it generically.
        if (operation.Kind == EnumProvider.OperationKind.FieldAccess && operation is not IFieldAccess)
            return null;

        return operation.GetSymbol();
    }

    /// <summary>
    /// Peels off <see cref="IConversionExpression"/> and <see cref="IParenthesizedExpression"/> wrappers
    /// around a bound expression and returns the innermost operand, so callers can resolve the symbol or
    /// type the source code actually named.
    /// </summary>
    public static IOperation UnwrapConversions(this IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionExpression conversion:
                    operation = conversion.Operand;
                    break;

                case IParenthesizedExpression parenthesized:
                    operation = parenthesized.Operand;
                    break;

                default:
                    return operation;
            }
        }
    }

    public static bool IsNamedReturnTarget(this IOperation? target, string returnVariableName)
    {
        if (target is null)
            return false;

        if (target.Kind == EnumProvider.OperationKind.ReturnValueReferenceExpression)
            return true;

        // Fall back to symbol identity, but only accept symbols whose kind is `ReturnValue`.
        // Comparing by name alone would incorrectly match unrelated members that happen to
        // share the return variable's name (e.g. `Buf.Result := 5;` where `Buf` is a record
        // with a field named `Result`).
        var symbol = target.GetSymbolSafe();

        return symbol is not null
            && symbol.Kind == EnumProvider.SymbolKind.ReturnValue
            && symbol.Name.IsSameName(returnVariableName);
    }
}
