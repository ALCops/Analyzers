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
    /// Gets the name key AZ sorts on: the object reference text (without surrounding trivia). A fully
    /// quoted reference loses its outer quotes and has doubled quotes unescaped; any other text is used
    /// verbatim, so <c>MyNs."My Table"</c> keeps namespace and quotes and the quote character takes part
    /// in the comparison. A wildcard entry (<c>tabledata * = RIMD</c>) has no reference and yields an
    /// empty key, which sorts last. (AZ also strips the leading quote of <c>"MyNs".Item</c>, leaving the
    /// closing one in the middle of the key; that mangling is not reproduced.)
    /// </summary>
    public static string GetSortKey(PermissionSyntax permission)
    {
        var identifier = permission.ObjectReference?.Identifier;
        if (identifier is null)
            return string.Empty;

        var text = identifier.ToString().Trim();
        if (text.Length < 2 || text[0] != '"' || text[text.Length - 1] != '"')
            return text;

        return text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
    }

    /// <summary>
    /// True when both entries belong to the same sort group: both table types, or the same priority.
    /// </summary>
    public static bool IsSameGroup(PermissionSyntax x, PermissionSyntax y)
    {
        int xPriority = GetTypePriority(x);
        int yPriority = GetTypePriority(y);
        return xPriority == yPriority || (IsTableType(xPriority) && IsTableType(yPriority));
    }

    public int Compare(PermissionSyntax? x, PermissionSyntax? y)
    {
        if (x is null || y is null)
            return x is null ? (y is null ? 0 : 1) : -1;

        return Compare(GetTypePriority(x), GetSortKey(x), GetTypePriority(y), GetSortKey(y));
    }

    /// <summary>
    /// Compares two entries by their precomputed priority and sort key (AZ's exact branch structure).
    /// </summary>
    public static int Compare(int xPriority, string xKey, int yPriority, string yKey)
    {
        bool bothTableTypes = IsTableType(xPriority) && IsTableType(yPriority);

        if (!bothTableTypes && xPriority != yPriority)
            return xPriority - yPriority;

        int nameResult = NaturalStringComparer.Instance.Compare(xKey, yKey);
        if (!bothTableTypes || nameResult != 0)
            return nameResult;

        return xPriority - yPriority;
    }

    private static bool IsTableType(int priority) =>
        priority == TablePriority || priority == TableDataPriority;
}
