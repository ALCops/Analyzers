using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Permissions;

#if NETSTANDARD2_1
public readonly struct ResolvedTableName
{
    public string TableName { get; }
    public string? QualifyingNamespace { get; }

    public ResolvedTableName(string tableName, string? qualifyingNamespace)
    {
        TableName = tableName;
        QualifyingNamespace = qualifyingNamespace;
    }
}
#else
public readonly record struct ResolvedTableName(string TableName, string? QualifyingNamespace);
#endif

/// <summary>
/// Resolves the appropriate table name for use in a Permissions property,
/// using C#-like namespace resolution: simple name when the table's namespace
/// is the same as the containing object's or is imported via a using directive,
/// fully qualified otherwise.
/// </summary>
public static class PermissionTableNameResolver
{
    /// <summary>
    /// Returns the table name suitable for the Permissions property.
    /// Simple name if namespace is resolvable from context, qualified otherwise.
    /// </summary>
    public static string ResolveTableName(string tableName, string? tableNamespace, string? containingNamespace, IEnumerable<string> importedNamespaces)
    {
        if (string.IsNullOrEmpty(tableNamespace))
            return tableName.QuoteIdentifierIfNeededWithReflection();

        if (containingNamespace is not null && SemanticFacts.IsSameName(tableNamespace, containingNamespace))
            return tableName.QuoteIdentifierIfNeededWithReflection();

        if (importedNamespaces.Any(ns => SemanticFacts.IsSameName(ns, tableNamespace)))
            return tableName.QuoteIdentifierIfNeededWithReflection();

        return $"{tableNamespace}.{tableName.QuoteIdentifierIfNeededWithReflection()}";
    }

    /// <summary>
    /// Returns the table name and optional qualifying namespace as separate values,
    /// avoiding the need to re-parse a combined string.
    /// </summary>
    public static ResolvedTableName ResolveTableNameParts(string tableName, string? tableNamespace, string? containingNamespace, IEnumerable<string> importedNamespaces)
    {
        var quotedName = tableName.QuoteIdentifierIfNeededWithReflection();

        if (string.IsNullOrEmpty(tableNamespace))
            return new ResolvedTableName(quotedName, null);

        if (containingNamespace is not null && SemanticFacts.IsSameName(tableNamespace, containingNamespace))
            return new ResolvedTableName(quotedName, null);

        if (importedNamespaces.Any(ns => SemanticFacts.IsSameName(ns, tableNamespace)))
            return new ResolvedTableName(quotedName, null);

        return new ResolvedTableName(quotedName, tableNamespace);
    }
}
