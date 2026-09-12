using ALCops.Common.Extensions;
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
        => HasSingleCodePrimaryKeyNamed(table, "Primary Key", "PrimaryKey") || HasGetRecordOnceMethod(table);

    /// <summary>
    /// Matches setup singletons and small reference/lookup tables. Signals (cheapest first):
    /// a single Code-type PK field named <c>Primary Key</c>, <c>PrimaryKey</c>, <c>Code</c>
    /// or <c>Name</c>; a namespace ending in <c>.Setup</c>; a parameterless <c>GetRecordOnce</c>
    /// method. Any <c>AutoIncrement</c> field in the primary key vetoes the match.
    /// </summary>
    public static bool IsSetupOrReferenceTable(ITableTypeSymbol table)
        => (HasSingleCodePrimaryKeyNamed(table, "Primary Key", "PrimaryKey", "Code", "Name")
            || IsInSetupNamespace(table)
            || HasGetRecordOnceMethod(table))
           && !HasAutoIncrementPrimaryKey(table);

    private static bool IsInSetupNamespace(ITableTypeSymbol table)
    {
        var ns = table.GetContainingNamespaceQualifiedNameWithReflection();
        if (string.IsNullOrEmpty(ns))
            return false;

        return SemanticFacts.IsSameName(ns, "Setup")
            || ns.EndsWith(".Setup", SemanticFacts.NameEqualityComparison);
    }

    private static bool HasSingleCodePrimaryKeyNamed(ITableTypeSymbol table, params string[] names)
    {
        if (table.PrimaryKey is null || table.PrimaryKey.Fields.Length != 1)
            return false;

        var pkField = table.PrimaryKey.Fields[0];

        if (pkField.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Code)
            return false;

        var fieldName = pkField.Name;
        foreach (var name in names)
        {
            if (SemanticFacts.IsSameName(fieldName, name))
                return true;
        }

        return false;
    }

    private static bool HasAutoIncrementPrimaryKey(ITableTypeSymbol table)
    {
        if (table.PrimaryKey is null || table.PrimaryKey.Fields.Length == 0)
            return false;

        foreach (var field in table.PrimaryKey.Fields)
        {
            if (field.GetBooleanPropertyValue(EnumProvider.PropertyKind.AutoIncrement) == true)
                return true;
        }

        return false;
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
