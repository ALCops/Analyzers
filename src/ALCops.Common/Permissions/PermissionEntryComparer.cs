using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Permissions;

/// <summary>
/// Orders <see cref="PermissionSyntax"/> entries the way the AZ AL Dev Tools "Sort Permissions"
/// command does (<c>PermissionComparer</c> in anzwdev/al-code-outline):
/// <list type="bullet">
/// <item><c>table</c> and <c>tabledata</c> entries form one group that comes first and is sorted by
/// object name; on an identical name <c>table</c> precedes <c>tabledata</c>;</item>
/// <item>the remaining entries follow in the fixed order <c>codeunit</c>, <c>page</c>, <c>query</c>,
/// <c>report</c>, <c>xmlport</c>, each type sorted by object name;</item>
/// <item>any other type keyword (for example <c>system</c>) gets the <c>table</c> priority and is
/// therefore interleaved with the table group - this mirrors AZ's fallback on purpose;</item>
/// <item>names compare with <see cref="NaturalStringComparer"/> on the key produced by <see cref="GetSortKey"/>.</item>
/// </list>
/// </summary>
public sealed class PermissionEntryComparer : IComparer<PermissionSyntax>
{
    private const int TablePriority = 0;
    private const int TableDataPriority = 1;

    // Type keywords are AL keywords, not identifiers, so OrdinalIgnoreCase is appropriate here.
    private static readonly Dictionary<string, int> TypePriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["table"] = TablePriority,
        ["tabledata"] = TableDataPriority,
        ["codeunit"] = 2,
        ["page"] = 3,
        ["query"] = 4,
        ["report"] = 5,
        ["xmlport"] = 6,
    };

    public static PermissionEntryComparer Instance { get; } = new();

    private PermissionEntryComparer()
    {
    }

    /// <summary>
    /// Gets the AZ type priority of the entry; unknown keywords map to the <c>table</c> priority.
    /// </summary>
    public static int GetTypePriority(PermissionSyntax permission)
    {
        var type = PermissionSyntaxHelper.GetPermissionTypeText(permission);
        return type is not null && TypePriorities.TryGetValue(type, out var priority)
            ? priority
            : TablePriority;
    }

    /// <summary>
    /// Gets the name key AZ sorts on: the object reference text (without surrounding trivia); when it
    /// starts with a quote the outer quotes are removed and doubled quotes unescaped, otherwise the
    /// text is used verbatim (so <c>MyNs."My Table"</c> keeps namespace and quotes, and an object id
    /// stays numeric text). A wildcard entry (<c>tabledata * = RIMD</c>) has no reference and yields
    /// an empty key, which sorts last.
    /// </summary>
    public static string GetSortKey(PermissionSyntax permission)
    {
        var identifier = permission.ObjectReference?.Identifier;
        if (identifier is null)
            return string.Empty;

        var text = identifier.ToString().Trim();
        if (text.Length == 0 || text[0] != '"')
            return text;

        text = text.Substring(1);
        if (text.Length > 0 && text[text.Length - 1] == '"')
            text = text.Substring(0, text.Length - 1);

        return text.Replace("\"\"", "\"");
    }

    public int Compare(PermissionSyntax? x, PermissionSyntax? y)
    {
        if (x is null || y is null)
            return x is null ? (y is null ? 0 : 1) : -1;

        int xPriority = GetTypePriority(x);
        int yPriority = GetTypePriority(y);
        bool bothTableTypes = IsTableType(xPriority) && IsTableType(yPriority);

        if (!bothTableTypes && xPriority != yPriority)
            return xPriority - yPriority;

        int nameResult = NaturalStringComparer.Instance.Compare(GetSortKey(x), GetSortKey(y));
        if (!bothTableTypes || nameResult != 0)
            return nameResult;

        return xPriority - yPriority;
    }

    private static bool IsTableType(int priority) =>
        priority == TablePriority || priority == TableDataPriority;
}
