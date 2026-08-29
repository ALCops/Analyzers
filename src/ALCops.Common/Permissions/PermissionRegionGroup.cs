using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Permissions;

/// <summary>
/// One entry of a <c>Permissions</c> list inside a <see cref="PermissionRegionGroup"/>: its position in
/// the source list, the node with only its own (non-directive) leading trivia, and its sort key computed
/// once so sorting does not rebuild it per comparison.
/// </summary>
public sealed class PermissionRegionEntry
{
    public int Index { get; }
    public PermissionSyntax Node { get; }
    public int TypePriority { get; }
    public string SortKey { get; }

    public PermissionRegionEntry(int index, PermissionSyntax node)
    {
        Index = index;
        Node = node;
        TypePriority = PermissionEntryComparer.GetTypePriority(node);
        SortKey = PermissionEntryComparer.GetSortKey(node);
    }
}

/// <summary>
/// One <c>#region</c> block (or the root) of a <c>Permissions</c> list, mirroring the group tree the
/// AZ AL Dev Tools "Sort Permissions" command builds (<c>SyntaxNodesGroup</c> in anzwdev/al-code-outline).
/// Entries are sorted per group; when flattened a group emits its own entries first and its child
/// regions afterwards, each region keeping the directive trivia that opened and closed it.
/// </summary>
public sealed class PermissionRegionGroup
{
    private static readonly Comparison<PermissionRegionEntry> EntryComparison = (x, y) =>
        PermissionEntryComparer.Compare(x.TypePriority, x.SortKey, y.TypePriority, y.SortKey);

    public PermissionRegionGroup? Parent { get; }

    public List<PermissionRegionEntry> Entries { get; } = new();

    public List<PermissionRegionGroup> Children { get; } = new();

    /// <summary>Trivia up to and including the <c>#region</c> directive that opened this group.</summary>
    public List<SyntaxTrivia> LeadingTrivia { get; } = new();

    /// <summary>Trivia up to and including the <c>#endregion</c> directive that closed this group.</summary>
    public List<SyntaxTrivia> TrailingTrivia { get; } = new();

    /// <summary>Set on the root when any preprocessor directive occurs inside the list.</summary>
    public bool ContainsDirectives { get; set; }

    public PermissionRegionGroup(PermissionRegionGroup? parent)
    {
        Parent = parent;
        parent?.Children.Add(this);
    }

    /// <summary>
    /// Sorts the entries of this group and of every child group (stable, so entries with equal
    /// keys keep their relative order).
    /// </summary>
    public void Sort()
    {
        var sorted = Entries.OrderBy(entry => entry, Comparer<PermissionRegionEntry>.Create(EntryComparison)).ToList();
        Entries.Clear();
        Entries.AddRange(sorted);

        foreach (var child in Children)
            child.Sort();
    }

    /// <summary>
    /// Appends the entries of this tree in output order: own entries, then each child group.
    /// When <paramref name="directiveTrivia"/> is given it receives, per output position, the directive
    /// trivia that must precede the entry at that position; trivia left in <paramref name="pending"/>
    /// after the call belongs after the last entry.
    /// </summary>
    public void Flatten(
        List<PermissionRegionEntry> output,
        List<List<SyntaxTrivia>>? directiveTrivia,
        List<SyntaxTrivia> pending)
    {
        pending.AddRange(LeadingTrivia);

        foreach (var entry in Entries)
        {
            output.Add(entry);
            directiveTrivia?.Add(new List<SyntaxTrivia>(pending));
            pending.Clear();
        }

        foreach (var child in Children)
            child.Flatten(output, directiveTrivia, pending);

        pending.AddRange(TrailingTrivia);
    }
}
