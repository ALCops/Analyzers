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
    /// by returning its <c>ApplicationObjectTypeSymbol</c>, and guards against any other
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

        // Guard against BoundObjectAccess and any future types that report FieldAccess
        // but don't implement IFieldAccess. IObjectAccess is internal so we can't check
        // it directly; the is-not-IFieldAccess guard catches it generically.
        if (operation.Kind == EnumProvider.OperationKind.FieldAccess && operation is not IFieldAccess)
            return null;

        return operation.GetSymbol();
    }
}
