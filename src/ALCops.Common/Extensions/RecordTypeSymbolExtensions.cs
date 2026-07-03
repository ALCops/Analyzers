using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class RecordTypeSymbolInterfaceExtensions
{
    /// <summary>
    /// Returns true if the record is temporary by any means: the <c>temporary</c> keyword on the
    /// variable (<see cref="IRecordTypeSymbol.Temporary"/>) or a backing table declared with
    /// <c>TableType = Temporary</c>. Temporary records are in-memory and never touch the database,
    /// regardless of how the temporary table is implemented.
    /// </summary>
    public static bool IsTemporary(this IRecordTypeSymbol record) =>
        record.Temporary ||
        (record.OriginalDefinition is ITableTypeSymbol table && table.IsTemporary());
}
