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
    /// Checks whether the permission entries are already in <see cref="PermissionEntryComparer"/> order.
    /// This is a flat check that ignores <c>#region</c> grouping; FC0004 uses
    /// <see cref="TryBuildRegionTree"/> + <see cref="NeedsReordering"/> instead.
    /// Returns true if 0 or 1 entries (trivially sorted).
    /// </summary>
    public static bool ArePermissionsSorted(SeparatedSyntaxList<PermissionSyntax> permissions) =>
        ArePermissionsSorted((IReadOnlyList<PermissionSyntax>)permissions);

    public static bool ArePermissionsSorted(IReadOnlyList<PermissionSyntax> permissions)
    {
        for (int i = 1; i < permissions.Count; i++)
        {
            if (PermissionEntryComparer.Instance.Compare(permissions[i - 1], permissions[i]) > 0)
                return false;
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
    /// When <paramref name="qualifyingNamespace"/> is non-null, creates a qualified name
    /// (e.g., <c>MyNamespace."Customer"</c>); otherwise creates a simple identifier.
    /// </summary>
    public static PermissionSyntax CreatePermissionSyntax(string tableName, string? qualifyingNamespace, string permissions)
    {
        var objectType = SyntaxFactory.Token(EnumProvider.SyntaxKind.TableDataKeyword);
        var objectReference = CreateObjectReference(tableName, qualifyingNamespace);
        var permissionsToken = SyntaxFactory.Identifier(permissions);

        return SyntaxFactory.Permission(objectType, objectReference, permissionsToken);
    }

    /// <summary>
    /// Finds the insertion index for a new <c>tabledata</c> entry named <paramref name="tableName"/>
    /// in a sorted permission list. Prefer the <see cref="PermissionSyntax"/> overload, which sorts on
    /// the exact key of the entry that will be inserted. If not sorted, returns the count (append).
    /// </summary>
    public static int FindInsertionIndex(SeparatedSyntaxList<PermissionSyntax> permissions, string tableName, bool isSorted) =>
        FindInsertionIndex(permissions, CreatePermissionSyntax(tableName, null, string.Empty), isSorted);

    /// <summary>
    /// Finds the index at which <paramref name="newEntry"/> belongs in a sorted permission list
    /// according to <see cref="PermissionEntryComparer"/>. If not sorted, returns the count (append).
    /// </summary>
    public static int FindInsertionIndex(SeparatedSyntaxList<PermissionSyntax> permissions, PermissionSyntax newEntry, bool isSorted)
    {
        if (!isSorted)
            return permissions.Count;

        for (int i = 0; i < permissions.Count; i++)
        {
            if (PermissionEntryComparer.Instance.Compare(newEntry, permissions[i]) < 0)
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

    private static ObjectNameOrIdSyntax CreateObjectReference(string tableName, string? qualifyingNamespace)
    {
        if (qualifyingNamespace is not null)
        {
            var qualifiedName = SyntaxFactory.QualifiedName(
                ParseNamespaceName(qualifyingNamespace),
                SyntaxFactory.IdentifierName(tableName));
            return SyntaxFactory.ObjectNameOrId(qualifiedName);
        }

        return SyntaxFactory.ObjectNameOrId(SyntaxFactory.IdentifierName(tableName));
    }

    /// <summary>
    /// Builds a <see cref="NameSyntax"/> from a namespace string that may contain dots
    /// (e.g., "MyPTE.Sales" becomes <c>QualifiedName(IdentifierName("MyPTE"), IdentifierName("Sales"))</c>).
    /// </summary>
    private static NameSyntax ParseNamespaceName(string namespaceName)
    {
        var segments = namespaceName.Split('.');
        NameSyntax result = SyntaxFactory.IdentifierName(segments[0]);
        for (int i = 1; i < segments.Length; i++)
            result = SyntaxFactory.QualifiedName(result, SyntaxFactory.IdentifierName(segments[i]));
        return result;
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
    /// Sorts the permission entries with <see cref="PermissionEntryComparer"/> (stable sort, so
    /// duplicate entries keep their relative order). Returns a new list; trivia is left untouched
    /// (callers are responsible for applying formatting).
    /// </summary>
    public static List<PermissionSyntax> GetSortedPermissions(SeparatedSyntaxList<PermissionSyntax> permissions) =>
        permissions.OrderBy(permission => permission, PermissionEntryComparer.Instance).ToList();

    /// <summary>
    /// Builds the <c>#region</c> tree of a Permissions property the way AZ AL Dev Tools does:
    /// <c>#region</c>/<c>#endregion</c> directives in an entry's leading trivia open and close groups,
    /// and the trivia run up to and including the directive travels with the group. Entries stay the
    /// original nodes so callers can compare identity. Returns false - and callers must leave the
    /// property alone - when the list contains any other directive (<c>#if</c>, <c>#pragma</c>, ...)
    /// or the regions are unbalanced. Directives in the leading trivia of the closing <c>;</c> only
    /// take part in the balance check and are never moved.
    /// </summary>
    public static bool TryBuildRegionTree(PropertySyntax permissionsProperty, out PermissionRegionGroup root, out bool hasDirectives)
    {
        root = new PermissionRegionGroup(null);
        hasDirectives = false;

        if (permissionsProperty.Value is not PermissionPropertyValueSyntax value)
            return false;

        var current = root;
        var cache = new List<SyntaxTrivia>();
        foreach (var entry in value.PermissionProperties)
        {
            cache.Clear();
            foreach (var trivia in entry.GetLeadingTrivia())
            {
                cache.Add(trivia);
                if (!trivia.IsDirective)
                    continue;

                hasDirectives = true;
                if (trivia.Kind == EnumProvider.SyntaxKind.RegionDirectiveTrivia)
                {
                    current = new PermissionRegionGroup(current);
                    current.LeadingTrivia.AddRange(cache);
                    cache.Clear();
                }
                else if (trivia.Kind == EnumProvider.SyntaxKind.EndRegionDirectiveTrivia)
                {
                    if (current.Parent is null)
                        return false;

                    current.TrailingTrivia.AddRange(cache);
                    cache.Clear();
                    current = current.Parent;
                }
                else
                {
                    return false;
                }
            }

            current.Entries.Add(entry);
        }

        foreach (var trivia in permissionsProperty.SemicolonToken.LeadingTrivia)
        {
            if (!trivia.IsDirective)
                continue;

            hasDirectives = true;
            if (trivia.Kind == EnumProvider.SyntaxKind.RegionDirectiveTrivia)
            {
                current = new PermissionRegionGroup(current);
            }
            else if (trivia.Kind == EnumProvider.SyntaxKind.EndRegionDirectiveTrivia)
            {
                if (current.Parent is null)
                    return false;

                current = current.Parent;
            }
            else
            {
                return false;
            }
        }

        return current.Parent is null;
    }

    /// <summary>
    /// Sorts <paramref name="root"/> (built by <see cref="TryBuildRegionTree"/>) and reports whether
    /// the resulting entry order differs from the order in <paramref name="permissionValue"/>, i.e.
    /// whether <see cref="ReorderPreservingLayout"/> would move any entry.
    /// </summary>
    public static bool NeedsReordering(PermissionPropertyValueSyntax permissionValue, PermissionRegionGroup root)
    {
        root.Sort(PermissionEntryComparer.Instance);

        var original = permissionValue.PermissionProperties;
        var flattened = new List<PermissionSyntax>(original.Count);
        root.Flatten(flattened, new List<List<SyntaxTrivia>>(), new List<SyntaxTrivia>());
        if (flattened.Count != original.Count)
            return false;

        for (int i = 0; i < original.Count; i++)
        {
            if (!ReferenceEquals(flattened[i], original[i]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Rewrites the Permissions property with its entries in <see cref="PermissionEntryComparer"/>
    /// order while keeping the existing layout: position <c>i</c> receives the <c>i</c>-th sorted entry
    /// but keeps the indentation and comments that were at position <c>i</c>, separators are untouched,
    /// and <c>#region</c>/<c>#endregion</c> directives are re-emitted where their group now starts and
    /// ends (a group's own entries are emitted before its nested regions, as AZ AL Dev Tools does).
    /// </summary>
    public static PropertySyntax ReorderPreservingLayout(PropertySyntax permissionsProperty, PermissionRegionGroup root)
    {
        if (permissionsProperty.Value is not PermissionPropertyValueSyntax permissionValue)
            return permissionsProperty;

        var original = permissionValue.PermissionProperties;
        root.Sort(PermissionEntryComparer.Instance);

        var flattened = new List<PermissionSyntax>(original.Count);
        var directiveTrivia = new List<List<SyntaxTrivia>>(original.Count);
        var pending = new List<SyntaxTrivia>();
        root.Flatten(flattened, directiveTrivia, pending);
        if (flattened.Count != original.Count)
            return permissionsProperty;

        var replacements = new PermissionSyntax[original.Count];
        for (int i = 0; i < original.Count; i++)
        {
            var slot = original[i];
            var slotLeadingTrivia = slot.GetLeadingTrivia();
            var leadingTrivia = new List<SyntaxTrivia>();

            if (directiveTrivia[i].Count > 0)
            {
                // A directive must start on its own line; the slot only guarantees that when it
                // carried a directive itself.
                if (!ContainsDirective(slotLeadingTrivia)
                    && !HasNewlineTrivia(slot.GetFirstToken().GetPreviousToken().TrailingTrivia))
                {
                    leadingTrivia.AddRange(SyntaxFactory.ParseLeadingTrivia("\n"));
                }

                leadingTrivia.AddRange(directiveTrivia[i]);
            }

            leadingTrivia.AddRange(GetTriviaAfterLastDirective(slotLeadingTrivia));

            replacements[i] = flattened[i]
                .WithLeadingTrivia(SyntaxFactory.TriviaList(leadingTrivia))
                .WithTrailingTrivia(slot.GetTrailingTrivia());
        }

        var newValue = permissionValue.ReplaceNodes(
            original,
            (oldNode, _) => replacements[IndexOf(original, oldNode)]);
        var newProperty = permissionsProperty.WithValue(newValue);

        if (pending.Count > 0)
        {
            var semicolon = permissionsProperty.SemicolonToken;
            var semicolonLeading = new List<SyntaxTrivia>();
            if (!HasNewlineTrivia(original[original.Count - 1].GetTrailingTrivia()))
                semicolonLeading.AddRange(SyntaxFactory.ParseLeadingTrivia("\n"));
            semicolonLeading.AddRange(pending);
            semicolonLeading.AddRange(semicolon.LeadingTrivia);
            newProperty = newProperty.WithSemicolonToken(
                semicolon.WithLeadingTrivia(SyntaxFactory.TriviaList(semicolonLeading)));
        }

        return newProperty;
    }

    private static int IndexOf(SeparatedSyntaxList<PermissionSyntax> list, PermissionSyntax node)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], node))
                return i;
        }

        throw new InvalidOperationException("Permission entry is not part of the list being rewritten.");
    }

    private static bool ContainsDirective(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (trivia.IsDirective)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the trivia that follows the last directive in the list (the entry's own indentation
    /// and comments); the whole list when it holds no directive.
    /// </summary>
    private static IEnumerable<SyntaxTrivia> GetTriviaAfterLastDirective(SyntaxTriviaList triviaList)
    {
        int start = 0;
        for (int i = 0; i < triviaList.Count; i++)
        {
            if (triviaList[i].IsDirective)
                start = i + 1;
        }

        for (int i = start; i < triviaList.Count; i++)
            yield return triviaList[i];
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
