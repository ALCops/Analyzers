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
/// <item>when every run is equal the shorter original string (spaces included) comes first.</item>
/// </list>
/// The only intentional divergence from AZ: digit runs are compared without parsing, so over-long
/// digit sequences never overflow.
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

            string leftChunk = left.Substring(leftStart, leftMarker - leftStart);
            string rightChunk = right.Substring(rightStart, rightMarker - rightStart);

            int result = leftIsDigit && rightIsDigit
                ? CompareDigitChunks(leftChunk, rightChunk)
                : string.Compare(
                    leftChunk.Replace(" ", string.Empty),
                    rightChunk.Replace(" ", string.Empty),
                    StringComparison.InvariantCultureIgnoreCase);

            if (result != 0)
                return result;
        }

        return leftLength - rightLength;
    }

    /// <summary>
    /// Compares two all-digit strings by numeric value without parsing: leading zeros are
    /// ignored, a longer remaining sequence is larger, equal lengths compare ordinally.
    /// </summary>
    private static int CompareDigitChunks(string left, string right)
    {
        int leftStart = 0;
        while (leftStart < left.Length - 1 && left[leftStart] == '0')
            leftStart++;

        int rightStart = 0;
        while (rightStart < right.Length - 1 && right[rightStart] == '0')
            rightStart++;

        int leftDigits = left.Length - leftStart;
        int rightDigits = right.Length - rightStart;
        if (leftDigits != rightDigits)
            return leftDigits - rightDigits;

        return string.CompareOrdinal(left, leftStart, right, rightStart, leftDigits);
    }
}
