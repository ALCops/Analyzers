namespace ALCops.Common.Permissions;

/// <summary>
/// Natural (alphanumeric) string comparer that reproduces the name ordering of the
/// AZ AL Dev Tools "Sort Permissions" command (<c>NullableStringComparer</c> over
/// <c>AlphanumComparatorFast</c> in anzwdev/al-code-outline):
/// <list type="bullet">
/// <item>null, empty and whitespace-only strings sort after everything else and equal to each other;</item>
/// <item>strings are split into maximal runs of digits / non-digits;</item>
/// <item>two digit runs compare numerically (<c>"Item 2"</c> &lt; <c>"Item 10"</c>);</item>
/// <item>any other pair of runs compares with spaces removed, <see cref="StringComparison.InvariantCultureIgnoreCase"/>;</item>
/// <item>when every run is equal the shorter string comes first.</item>
/// </list>
/// Two intentional divergences from AZ, both needed for a consistent <see cref="IComparer{T}"/>
/// contract or robustness: digit runs are compared without parsing (no overflow on long numbers),
/// and the final length tie-break ignores spaces, exactly like the run comparison does (AZ uses the
/// raw lengths, which makes <c>"x 1"</c>, <c>"x1y"</c>, <c>"x1z"</c> non-transitive).
/// </summary>
public sealed class NaturalStringComparer : IComparer<string?>
{
    public static NaturalStringComparer Instance { get; } = new();

    private NaturalStringComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        bool xEmpty = string.IsNullOrWhiteSpace(x);
        bool yEmpty = string.IsNullOrWhiteSpace(y);
        if (xEmpty != yEmpty)
            return xEmpty ? 1 : -1;
        if (xEmpty)
            return 0;

        string left = x!;
        string right = y!;
        int leftLength = left.Length;
        int rightLength = right.Length;
        int leftMarker = 0;
        int rightMarker = 0;

        while (leftMarker < leftLength && rightMarker < rightLength)
        {
            int leftStart = leftMarker;
            bool leftIsDigit = char.IsDigit(left[leftMarker]);
            while (leftMarker < leftLength && char.IsDigit(left[leftMarker]) == leftIsDigit)
                leftMarker++;

            int rightStart = rightMarker;
            bool rightIsDigit = char.IsDigit(right[rightMarker]);
            while (rightMarker < rightLength && char.IsDigit(right[rightMarker]) == rightIsDigit)
                rightMarker++;

            int result = leftIsDigit && rightIsDigit
                ? CompareDigitChunks(left, leftStart, leftMarker, right, rightStart, rightMarker)
                : string.Compare(
                    ChunkWithoutSpaces(left, leftStart, leftMarker),
                    ChunkWithoutSpaces(right, rightStart, rightMarker),
                    StringComparison.InvariantCultureIgnoreCase);

            if (result != 0)
                return result;
        }

        return LengthWithoutSpaces(left) - LengthWithoutSpaces(right);
    }

    private static string ChunkWithoutSpaces(string value, int start, int end)
    {
        var chunk = value.Substring(start, end - start);
        return chunk.Contains(' ') ? chunk.Replace(" ", string.Empty) : chunk;
    }

    private static int LengthWithoutSpaces(string value)
    {
        int length = 0;
        foreach (var c in value)
        {
            if (c != ' ')
                length++;
        }

        return length;
    }

    /// <summary>
    /// Compares two all-digit runs by numeric value without parsing: leading zeros are
    /// ignored, a longer remaining sequence is larger, equal lengths compare ordinally.
    /// </summary>
    private static int CompareDigitChunks(string left, int leftStart, int leftEnd, string right, int rightStart, int rightEnd)
    {
        while (leftStart < leftEnd - 1 && left[leftStart] == '0')
            leftStart++;
        while (rightStart < rightEnd - 1 && right[rightStart] == '0')
            rightStart++;

        int leftDigits = leftEnd - leftStart;
        int rightDigits = rightEnd - rightStart;
        if (leftDigits != rightDigits)
            return leftDigits - rightDigits;

        return string.CompareOrdinal(left, leftStart, right, rightStart, leftDigits);
    }
}
