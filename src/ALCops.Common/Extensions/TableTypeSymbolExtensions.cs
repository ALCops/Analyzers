using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class TableTypeSymbolInterfaceExtensions
{
    /// <summary>
    /// Returns true if the table object itself is temporary (<c>TableType = Temporary</c>).
    /// This is independent of the <c>temporary</c> keyword on a variable: a plain
    /// <c>Record MyTable</c> where <c>MyTable</c> has <c>TableType = Temporary</c> is still
    /// temporary at runtime, yet <see cref="IRecordTypeSymbol.Temporary"/> would be false.
    /// </summary>
    public static bool IsTemporary(this ITableTypeSymbol table) =>
        table.TableType == EnumProvider.TableTypeKind.Temporary;
}
