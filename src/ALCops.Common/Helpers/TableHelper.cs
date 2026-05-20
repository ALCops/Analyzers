using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Helpers;

/// <summary>
/// Helper methods for classifying table types based on structural heuristics.
/// </summary>
public static class TableHelper
{
    /// <summary>
    /// Determines whether a table follows the standard BC setup table pattern:
    /// single primary key field of type Code, named "Primary Key" or "PrimaryKey" (case-insensitive).
    /// </summary>
    public static bool IsSetupTable(ITableTypeSymbol table)
    {
        if (table.PrimaryKey is null || table.PrimaryKey.Fields.Length != 1)
            return false;

        var pkField = table.PrimaryKey.Fields[0];

        if (pkField.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Code)
            return false;

        var name = pkField.Name;
        return SemanticFacts.IsSameName(name, "Primary Key")
            || SemanticFacts.IsSameName(name, "PrimaryKey");
    }
}
