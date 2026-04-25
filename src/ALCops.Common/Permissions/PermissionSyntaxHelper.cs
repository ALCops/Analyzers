using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.Common.Permissions;

/// <summary>
/// Helpers for analyzing and constructing Permissions property syntax nodes.
/// </summary>
public static class PermissionSyntaxHelper
{
    private const string CanonicalOrder = "rimd";

    /// <summary>
    /// Checks whether the permission entries in a PermissionPropertyValueSyntax are sorted
    /// alphabetically by table name (case-insensitive).
    /// Returns true if 0 or 1 entries (trivially sorted).
    /// </summary>
    public static bool ArePermissionsSorted(SeparatedSyntaxList<PermissionSyntax> permissions)
    {
        if (permissions.Count <= 1)
            return true;

        string? previousName = null;
        foreach (var permission in permissions)
        {
            var name = GetTableNameFromPermission(permission);
            if (name is null)
                continue;

            if (previousName is not null &&
                string.Compare(previousName, name, StringComparison.OrdinalIgnoreCase) > 0)
                return false;

            previousName = name;
        }

        return true;
    }

    /// <summary>
    /// Detects whether the permission list uses multi-line format by checking
    /// if any comma separator has trailing newline trivia.
    /// </summary>
    public static bool IsMultiLineFormat(PermissionPropertyValueSyntax permissionValue)
    {
        var permissions = permissionValue.PermissionProperties;
        if (permissions.Count <= 1)
            return false;

        var separators = permissions.GetSeparators();
        foreach (var separator in separators)
        {
            if (HasNewlineTrivia(separator.TrailingTrivia))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Merges a new permission char into an existing permission string,
    /// returning the result in canonical 'rimd' order.
    /// </summary>
    public static string NormalizePermissionString(string existing, char newChar)
    {
        var chars = new HashSet<char>();
        foreach (var c in existing)
            chars.Add(char.ToLowerInvariant(c));

        chars.Add(char.ToLowerInvariant(newChar));

        var result = new char[CanonicalOrder.Length];
        int count = 0;
        foreach (var c in CanonicalOrder)
        {
            if (chars.Contains(c))
                result[count++] = c;
        }

        return new string(result, 0, count);
    }

    /// <summary>
    /// Creates a new PermissionSyntax node for the given table name and permission string.
    /// Handles both simple names ("Customer") and qualified names ("MyNamespace.Customer").
    /// </summary>
    public static PermissionSyntax CreatePermissionSyntax(string tableName, string permissions)
    {
        var objectType = SyntaxFactory.Token(EnumProvider.SyntaxKind.TableDataKeyword);
        var objectReference = CreateObjectReference(tableName);
        var permissionsToken = SyntaxFactory.Identifier(permissions);

        return SyntaxFactory.Permission(objectType, objectReference, permissionsToken);
    }

    /// <summary>
    /// Finds the insertion index for a new entry in a sorted permission list.
    /// If not sorted, returns the count (append).
    /// </summary>
    public static int FindInsertionIndex(SeparatedSyntaxList<PermissionSyntax> permissions, string tableName, bool isSorted)
    {
        if (!isSorted)
            return permissions.Count;

        for (int i = 0; i < permissions.Count; i++)
        {
            var name = GetTableNameFromPermission(permissions[i]);
            if (name is not null && string.Compare(name, tableName, StringComparison.OrdinalIgnoreCase) > 0)
                return i;
        }

        return permissions.Count;
    }

    /// <summary>
    /// Finds an existing PermissionSyntax entry for a given table name.
    /// Matches by simple name or qualified name (case-insensitive).
    /// </summary>
    public static PermissionSyntax? FindExistingEntry(PermissionPropertyValueSyntax permissionValue, string tableName)
    {
        foreach (var permission in permissionValue.PermissionProperties)
        {
            if (!permission.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                continue;

            var name = GetTableNameFromPermission(permission);
            if (name is not null && string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase))
                return permission;
        }

        return null;
    }

    /// <summary>
    /// Gets the indentation string for multi-line formatting by examining existing entries.
    /// Returns the leading whitespace of the first tabledata keyword.
    /// </summary>
    public static string GetEntryIndentation(PermissionPropertyValueSyntax permissionValue)
    {
        var permissions = permissionValue.PermissionProperties;
        if (permissions.Count == 0)
            return "                  ";

        var firstEntry = permissions[0];
        var leadingTrivia = firstEntry.GetLeadingTrivia();
        foreach (var trivia in leadingTrivia)
        {
            var text = trivia.ToString();
            if (!string.IsNullOrEmpty(text) && text.Trim().Length == 0)
                return text;
        }

        return "                  ";
    }

    private static string? GetTableNameFromPermission(PermissionSyntax permission)
    {
        var identifier = permission.ObjectReference?.Identifier;
        if (identifier is null)
            return null;

        if (identifier.Kind == EnumProvider.SyntaxKind.IdentifierName)
            return ((IdentifierNameSyntax)identifier).Identifier.ValueText?.UnquoteIdentifier();

        if (identifier.Kind == EnumProvider.SyntaxKind.QualifiedName)
        {
            var qualified = (QualifiedNameSyntax)identifier;
            var qualifier = qualified.Left.GetText().ToString();
            var name = qualified.Right.Identifier.ValueText?.UnquoteIdentifier();
            return name is null ? null : $"{qualifier}.{name}";
        }

        return null;
    }

    private static ObjectNameOrIdSyntax CreateObjectReference(string tableName)
    {
        var dotIndex = tableName.LastIndexOf('.');
        if (dotIndex >= 0)
        {
            var namespacePart = tableName.Substring(0, dotIndex);
            var namePart = tableName.Substring(dotIndex + 1);
            var qualifiedName = SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName(namespacePart),
                SyntaxFactory.IdentifierName(namePart));
            return SyntaxFactory.ObjectNameOrId(qualifiedName);
        }

        return SyntaxFactory.ObjectNameOrId(SyntaxFactory.IdentifierName(tableName));
    }

    /// <summary>
    /// Inserts a permission entry into a multi-line permission list, preserving
    /// the multi-line format by fixing separator trivia after insertion.
    /// </summary>
    public static PermissionPropertyValueSyntax InsertIntoMultiLineList(
        PermissionPropertyValueSyntax permissionValue,
        SeparatedSyntaxList<PermissionSyntax> existing,
        int insertIndex,
        PermissionSyntax newEntry)
    {
        var newPermissions = existing.Insert(insertIndex, newEntry);
        var result = permissionValue.WithPermissionProperties(newPermissions);

        // Insert() creates a new comma separator without newline trailing trivia.
        // Copy the trivia pattern from an existing separator to fix multi-line formatting.
        var existingSeparators = existing.GetSeparators().ToList();
        if (existingSeparators.Count == 0)
            return result;

        var templateSeparator = existingSeparators[0];
        var resultSeparators = result.PermissionProperties.GetSeparators().ToList();

        foreach (var separator in resultSeparators)
        {
            if (!HasNewlineTrivia(separator.TrailingTrivia))
            {
                result = (PermissionPropertyValueSyntax)result.ReplaceToken(separator, templateSeparator);
                break;
            }
        }

        return result;
    }

    private static bool HasNewlineTrivia(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            var text = trivia.ToString();
            if (text.Contains('\n') || text.Contains('\r'))
                return true;
        }

        return false;
    }
}
