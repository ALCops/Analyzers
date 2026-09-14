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

    /// <summary>
    /// Resolves the table backing the record receiver of a member operation: an
    /// <see cref="IInvocationExpression"/> (built-in record method or user procedure) or an
    /// <see cref="IFieldAccess"/>. Handles all four AL receiver forms uniformly:
    /// <list type="bullet">
    ///   <item><c>MyVar.M()</c> / <c>Rec.M()</c> / <c>this.M()</c>: the operation's <c>Instance</c> is non-null;
    ///     the record type comes from its <c>Type</c>.</item>
    ///   <item>Bare <c>M()</c> / <c>F</c>: <c>Instance</c> is null; the table comes from
    ///     <paramref name="containingSymbol"/>'s containing type (table, or target of a table extension).</item>
    /// </list>
    /// The SDK also returns a null <c>Instance</c> for every static built-in call
    /// (<c>IsolatedStorage.Get(...)</c>, <c>NumberSequence.Next(...)</c>, bare <c>Error(...)</c>), so only the
    /// target method can tell that apart from a bare self call; a static built-in has no record receiver.
    /// </summary>
    /// <param name="operation">The invocation or field access whose receiver is resolved.</param>
    /// <param name="containingSymbol">The symbol whose body contains the operation (e.g. <c>ctx.ContainingSymbol</c>);
    ///   used only when the receiver is implicit.</param>
    /// <param name="recordType">The <see cref="IRecordTypeSymbol"/> when available (non-null for variable / Rec / this
    ///   receivers); null for bare self access where only the table shape is known.</param>
    /// <returns>The backing <see cref="ITableTypeSymbol"/>, or null when the receiver is not a record/table
    ///   (e.g. inside a codeunit or page), the invoked method is static, or <paramref name="operation"/> is neither
    ///   an invocation nor a field access.</returns>
    public static ITableTypeSymbol? GetReceiverTableType(
        this IOperation operation, ISymbol? containingSymbol, out IRecordTypeSymbol? recordType)
    {
        recordType = null;
        IOperation? instance;

        switch (operation)
        {
            case IInvocationExpression invocation:
                if (invocation.Instance is null)
                {
#if NETSTANDARD2_1
                    // IMethodSymbol.IsStatic does not exist at the AL 12 floor. Static built-ins are declared on a
                    // language class other than Table (IsolatedStorage, NumberSequence, Dialog, System, ...), while
                    // the record built-ins live on the Table class and user procedures on the object symbol.
                    bool isStatic = invocation.TargetMethod is { ContainingSymbol: IClassTypeSymbol containingClass } &&
                        !SemanticFacts.IsSameName(containingClass.Name, "Table");
#else
                    bool isStatic = invocation.TargetMethod is { IsStatic: true };
#endif
                    if (isStatic)
                        return null;
                }

                instance = invocation.Instance;
                break;

            case IFieldAccess fieldAccess:
                instance = fieldAccess.Instance;
                break;

            default:
                return null;
        }

        if (instance is not null)
        {
            if (instance.Type is IRecordTypeSymbol record)
            {
                recordType = record;
                return record.OriginalDefinition as ITableTypeSymbol;
            }

            return instance.Type as ITableTypeSymbol;
        }

        var containingType = containingSymbol?.ContainingType;

        if (containingType is IRecordTypeSymbol selfRecord)
        {
            recordType = selfRecord;
            return selfRecord.OriginalDefinition as ITableTypeSymbol;
        }

        if (containingType is ITableTypeSymbol table)
            return table;

        if (containingType is IApplicationObjectExtensionTypeSymbol extension)
            return extension.Target as ITableTypeSymbol;

        return null;
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
