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
        => HasSetupTablePrimaryKey(table) || HasGetRecordOnceMethod(table);

    /// <summary>
    /// Determines whether a table is a setup singleton or a small reference/lookup table
    /// whose rows stay resident in the NST record cache. Partial records
    /// (<c>SetLoadFields</c>) bypass that cache, causing repeated SQL round-trips that
    /// are slower than full-record cached reads for these few-row tables.
    /// Signals are structural only: the existing setup-table heuristic, a namespace
    /// ending in <c>.Setup</c>, or a single Code-type PK named <c>Code</c> or
    /// <c>Name</c>. An <c>AutoIncrement</c> first PK field vetoes the match
    /// (growing log-style tables that happen to live in a Setup namespace).
    /// </summary>
    public static bool IsSetupOrReferenceTable(ITableTypeSymbol table)
        => (IsSetupTable(table) || IsInSetupNamespace(table) || HasCodeOrNamePrimaryKey(table))
           && !HasAutoIncrementPrimaryKey(table);

    private static bool IsInSetupNamespace(ITableTypeSymbol table)
    {
        var ns = table.GetContainingNamespaceQualifiedNameWithReflection();
        if (string.IsNullOrEmpty(ns))
            return false;

        return SemanticFacts.IsSameName(ns, "Setup")
            || ns.EndsWith(".Setup", SemanticFacts.NameEqualityComparison);
    }

    private static bool HasCodeOrNamePrimaryKey(ITableTypeSymbol table)
    {
        if (table.PrimaryKey is null || table.PrimaryKey.Fields.Length != 1)
            return false;

        var pkField = table.PrimaryKey.Fields[0];

        if (pkField.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Code)
            return false;

        var name = pkField.Name;
        return SemanticFacts.IsSameName(name, "Code")
            || SemanticFacts.IsSameName(name, "Name");
    }

    private static bool HasAutoIncrementPrimaryKey(ITableTypeSymbol table)
    {
        if (table.PrimaryKey is null || table.PrimaryKey.Fields.Length == 0)
            return false;

        return table.PrimaryKey.Fields[0]
            .GetBooleanPropertyValue(EnumProvider.PropertyKind.AutoIncrement) == true;
    }

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
