using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Helpers;

/// <summary>
/// Helper methods for classifying table types based on structural heuristics.
/// </summary>
public static class TableHelper
{
    private const string GetRecordOnceMethodName = "GetRecordOnce";

    /// <summary>
    /// Determines whether a table follows a standard BC setup table pattern:
    /// either a single Code-type PK field named "Primary Key"/"PrimaryKey" (case-insensitive),
    /// or a parameterless, return-less <c>GetRecordOnce</c> method declared on the table itself.
    /// </summary>
    public static bool IsSetupTable(ITableTypeSymbol table)
        => HasSetupTablePrimaryKey(table) || HasGetRecordOnceMethod(table);

    private static bool HasSetupTablePrimaryKey(ITableTypeSymbol table)
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

    private static bool HasGetRecordOnceMethod(ITableTypeSymbol table)
    {
        foreach (var member in table.GetMembers(GetRecordOnceMethodName))
        {
            if (member.Kind != EnumProvider.SymbolKind.Method || member is not IMethodSymbol method)
                continue;

            if (method.Parameters.Length == 0
                && (method.ReturnValueSymbol?.ReturnType.NavTypeKind ?? EnumProvider.NavTypeKind.None) == EnumProvider.NavTypeKind.None)
                return true;
        }

        return false;
    }
}
