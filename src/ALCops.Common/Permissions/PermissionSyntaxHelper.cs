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
    private const string CanonicalOrder = MethodOperationMap.CanonicalOrder;

    /// <summary>
    /// Checks whether the permission entries are sorted alphabetically by
    /// (type keyword, object name), both case-insensitive.
    /// Returns true if 0 or 1 entries (trivially sorted).
    /// </summary>
    public static bool ArePermissionsSorted(SeparatedSyntaxList<PermissionSyntax> permissions)
    {
        if (permissions.Count <= 1)
            return true;

        string? previousType = null;
        string? previousName = null;
        foreach (var permission in permissions)
        {
            var type = GetPermissionTypeText(permission);
            var name = GetObjectNameFromPermission(permission);
            if (name is null || type is null)
                continue;

            if (previousType is not null && previousName is not null)
            {
                int typeCompare = string.Compare(previousType, type, StringComparison.OrdinalIgnoreCase);
                if (typeCompare > 0)
                    return false;
                if (typeCompare == 0 &&
                    string.Compare(previousName, name, StringComparison.OrdinalIgnoreCase) > 0)
                    return false;
            }

            previousType = type;
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
    /// Preserves the casing convention of the existing string: if existing chars
    /// are uppercase, the new char is added as uppercase (and vice versa).
    /// </summary>
    public static string NormalizePermissionString(string existing, char newChar)
    {
        var useUpperCase = IsUpperCaseConvention(existing);

        var chars = new HashSet<char>();
        foreach (var c in existing)
            chars.Add(char.ToLowerInvariant(c));

        chars.Add(char.ToLowerInvariant(newChar));

        var result = new char[CanonicalOrder.Length];
        int count = 0;
        foreach (var c in CanonicalOrder)
        {
            if (chars.Contains(c))
                result[count++] = useUpperCase ? char.ToUpperInvariant(c) : c;
        }

        return new string(result, 0, count);
    }

    /// <summary>
    /// Detects whether the existing permission string uses uppercase convention.
    /// Returns true if the majority of non-empty chars are uppercase.
    /// Defaults to false (lowercase) for empty strings.
    /// </summary>
    private static bool IsUpperCaseConvention(string permissions)
    {
        int upper = 0, lower = 0;
        foreach (var c in permissions)
        {
            if (char.IsUpper(c)) upper++;
            else if (char.IsLower(c)) lower++;
        }

        return upper > lower;
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
    /// Finds the insertion index for a new entry in a sorted permission list,
    /// comparing by (type keyword, object name). The new entry type defaults to "tabledata"
    /// since AC0031 only inserts tabledata entries.
    /// If not sorted, returns the count (append).
    /// </summary>
    public static int FindInsertionIndex(SeparatedSyntaxList<PermissionSyntax> permissions, string tableName, bool isSorted)
    {
        if (!isSorted)
            return permissions.Count;

        const string newType = "tabledata";
        for (int i = 0; i < permissions.Count; i++)
        {
            var entryType = GetPermissionTypeText(permissions[i]);
            var entryName = GetObjectNameFromPermission(permissions[i]);
            if (entryType is null || entryName is null)
                continue;

            int typeCompare = string.Compare(newType, entryType, StringComparison.OrdinalIgnoreCase);
            if (typeCompare < 0)
                return i;
            if (typeCompare == 0 && string.Compare(tableName, entryName, StringComparison.OrdinalIgnoreCase) < 0)
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

            var name = GetObjectNameFromPermission(permission);
            if (name is not null && string.Equals(name, tableName, StringComparison.OrdinalIgnoreCase))
                return permission;
        }

        return null;
    }

    /// <summary>
    /// Gets the indentation string for multi-line formatting by examining existing entries.
    /// In multi-line format, the first entry shares the line with "Permissions = " and has
    /// no indentation trivia, so entries at index &gt; 0 are checked first.
    /// </summary>
    public static string GetEntryIndentation(PermissionPropertyValueSyntax permissionValue)
    {
        var permissions = permissionValue.PermissionProperties;
        if (permissions.Count == 0)
            return "                  ";

        for (int i = 1; i < permissions.Count; i++)
        {
            var indentation = GetWhitespaceOnlyTrivia(permissions[i].GetLeadingTrivia());
            if (indentation is not null)
                return indentation;
        }

        var firstIndentation = GetWhitespaceOnlyTrivia(permissions[0].GetLeadingTrivia());
        return firstIndentation ?? "                  ";
    }

    private static string? GetWhitespaceOnlyTrivia(SyntaxTriviaList leadingTrivia)
    {
        foreach (var trivia in leadingTrivia)
        {
            var text = trivia.ToString();
            if (!string.IsNullOrEmpty(text) && text.Trim().Length == 0)
                return text;
        }

        return null;
    }

    /// <summary>
    /// Gets the type keyword text from a PermissionSyntax (e.g. "tabledata", "codeunit", "page").
    /// </summary>
    public static string? GetPermissionTypeText(PermissionSyntax permission)
    {
        var objectType = permission.ObjectType;
        return objectType.ValueText?.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the object name from a PermissionSyntax entry.
    /// Works for any permission type (tabledata, codeunit, page, etc.).
    /// </summary>
    public static string? GetObjectNameFromPermission(PermissionSyntax permission)
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
    /// When inserting at index 0, the displaced first entry receives indentation trivia
    /// since it moves from position 0 (same line as "Permissions = ") to position 1 (own line).
    /// </summary>
    public static PermissionPropertyValueSyntax InsertIntoMultiLineList(
        PermissionPropertyValueSyntax permissionValue,
        SeparatedSyntaxList<PermissionSyntax> existing,
        int insertIndex,
        PermissionSyntax newEntry)
    {
        var newPermissions = existing.Insert(insertIndex, newEntry);
        var result = permissionValue.WithPermissionProperties(newPermissions);

        // When inserting at index 0, the displaced first entry (now at index 1) was on
        // the same line as "Permissions = " and has no indentation trivia. Add it.
        if (insertIndex == 0 && existing.Count > 0)
        {
            var indentation = GetEntryIndentation(permissionValue);
            var displacedEntry = result.PermissionProperties[1];
            var indentedEntry = displacedEntry.WithLeadingTrivia(
                SyntaxFactory.ParseLeadingTrivia(indentation));
            result = result.WithPermissionProperties(
                result.PermissionProperties.Replace(displacedEntry, indentedEntry));
        }

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

    /// <summary>
    /// Sorts the permission entries by (type keyword, object name), both case-insensitive.
    /// Returns a new list with the entries in sorted order, stripping leading trivia from
    /// all entries (callers are responsible for applying formatting).
    /// </summary>
    public static List<PermissionSyntax> GetSortedPermissions(SeparatedSyntaxList<PermissionSyntax> permissions)
    {
        var entries = new List<PermissionSyntax>(permissions.Count);
        foreach (var permission in permissions)
            entries.Add(permission);

        entries.Sort((a, b) =>
        {
            var typeA = GetPermissionTypeText(a) ?? string.Empty;
            var typeB = GetPermissionTypeText(b) ?? string.Empty;
            int typeCompare = string.Compare(typeA, typeB, StringComparison.OrdinalIgnoreCase);
            if (typeCompare != 0)
                return typeCompare;

            var nameA = GetObjectNameFromPermission(a) ?? string.Empty;
            var nameB = GetObjectNameFromPermission(b) ?? string.Empty;
            return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
        });

        return entries;
    }

    /// <summary>
    /// Builds a multi-line PermissionPropertyValueSyntax from an ordered list of entries.
    /// The first entry has no leading trivia (it shares the line with "Permissions = ").
    /// Subsequent entries are indented and preceded by a newline.
    /// </summary>
    public static PermissionPropertyValueSyntax BuildMultiLinePermissionValue(
        List<PermissionSyntax> sortedEntries,
        string indentation)
    {
        if (sortedEntries.Count == 0)
            return SyntaxFactory.PermissionPropertyValue();

        // Strip trivia from all entries, then apply formatting
        var formatted = new List<PermissionSyntax>(sortedEntries.Count);
        for (int i = 0; i < sortedEntries.Count; i++)
        {
            var entry = sortedEntries[i]
                .WithLeadingTrivia(SyntaxFactory.TriviaList())
                .WithTrailingTrivia(SyntaxFactory.TriviaList());

            if (i > 0)
                entry = entry.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(indentation));

            formatted.Add(entry);
        }

        // Build the list with comma separators that have newline trailing trivia
        var result = SyntaxFactory.PermissionPropertyValue()
            .AddPermissionProperties(formatted[0]);

        for (int i = 1; i < formatted.Count; i++)
        {
            var currentList = result.PermissionProperties;
            var newList = currentList.Add(formatted[i]);
            result = result.WithPermissionProperties(newList);

            // Fix the separator trivia: the newly added separator needs newline trailing trivia
            var separators = result.PermissionProperties.GetSeparators().ToList();
            var lastSeparator = separators[separators.Count - 1];
            if (!HasNewlineTrivia(lastSeparator.TrailingTrivia))
            {
                var newlineTrivia = SyntaxFactory.ParseTrailingTrivia("\n");
                var fixedSeparator = lastSeparator.WithTrailingTrivia(newlineTrivia);
                result = (PermissionPropertyValueSyntax)result.ReplaceToken(lastSeparator, fixedSeparator);
            }
        }

        return result;
    }
}
