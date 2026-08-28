using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Permissions;

/// <summary>
/// One <c>#region</c> block (or the root) of a <c>Permissions</c> list, mirroring the group tree the
/// AZ AL Dev Tools "Sort Permissions" command builds (<c>SyntaxNodesGroup</c> in anzwdev/al-code-outline).
/// Entries are sorted per group; when flattened a group emits its own entries first and its child
/// regions afterwards, each region keeping the directive trivia that opened and closed it.
/// </summary>
public sealed class PermissionRegionGroup
{
    public PermissionRegionGroup? Parent { get; }

    /// <summary>Entries owned directly by this group, with their directive trivia already removed.</summary>
    public List<PermissionSyntax> Entries { get; } = new();

    public List<PermissionRegionGroup> Children { get; } = new();

    /// <summary>Trivia up to and including the <c>#region</c> directive that opened this group.</summary>
    public List<SyntaxTrivia> LeadingTrivia { get; } = new();

    /// <summary>Trivia up to and including the <c>#endregion</c> directive that closed this group.</summary>
    public List<SyntaxTrivia> TrailingTrivia { get; } = new();

    public PermissionRegionGroup(PermissionRegionGroup? parent)
    {
        Parent = parent;
        parent?.Children.Add(this);
    }

    /// <summary>
    /// Sorts the entries of this group and of every child group with <paramref name="comparer"/>.
    /// The sort is stable so entries with equal keys keep their relative order.
    /// </summary>
    public void Sort(IComparer<PermissionSyntax> comparer)
    {
        var sorted = Entries.OrderBy(entry => entry, comparer).ToList();
        Entries.Clear();
        Entries.AddRange(sorted);

        foreach (var child in Children)
            child.Sort(comparer);
    }

    /// <summary>
    /// Appends the entries of this tree in output order: own entries, then each child group.
    /// <paramref name="directiveTrivia"/> receives, per output position, the directive trivia that
    /// must precede the entry at that position; trivia left in <paramref name="pending"/> after the
    /// call belongs after the last entry.
    /// </summary>
    public void Flatten(
        List<PermissionSyntax> output,
        List<List<SyntaxTrivia>> directiveTrivia,
        List<SyntaxTrivia> pending)
    {
        pending.AddRange(LeadingTrivia);

        foreach (var entry in Entries)
        {
            output.Add(entry);
            directiveTrivia.Add(new List<SyntaxTrivia>(pending));
            pending.Clear();
        }

        foreach (var child in Children)
            child.Flatten(output, directiveTrivia, pending);

        pending.AddRange(TrailingTrivia);
    }
}
