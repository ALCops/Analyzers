using System.Text;

namespace ALCops.Common.Helpers;

/// <summary>
/// Renders a natural-language string (a phrase, a BC field name, a PascalCase identifier)
/// into a chosen <see cref="IdentifierCaseStyle"/>, honoring acronym canonical casing
/// from an <see cref="AcronymRegistry"/>. Splits the input on whitespace, common punctuation,
/// and PascalCase/camelCase boundaries; then applies per-word casing rules including the
/// C# guideline exceptions for two-letter abbreviations and the abbreviation "ID".
///
/// Shared infrastructure for analyzers that generate or validate identifiers derived from
/// natural-language input (e.g. LC0098 subscriber names, potentially future rules that
/// suggest table/field/procedure names). <see cref="Render"/> returns the single
/// preferred spelling; <see cref="RenderAccepted"/> additionally returns registered
/// acronym variants alongside the original casing when the same upper-invariant key
/// is present in the registry (Pascal / non-first Camel positions only).
/// </summary>
public static class IdentifierNameRenderer
{
    private static readonly char[] WordDelimiters =
        { ' ', '_', '-', '.', '/', '&', '(', ')', '+', '%' };

    /// <summary>
    /// Renders the input in the requested style. Follows "original casing wins":
    /// as long as a source word carries any uppercase character, its casing is
    /// preserved (with only the leading character forced to uppercase for Pascal /
    /// non-first Camel positions). The acronym registry is consulted only when the
    /// source word is all-lowercase, in which case a registered acronym recovers its
    /// canonical casing (e.g. <c>vat</c> -> <c>VAT</c>, <c>odata</c> -> <c>OData</c>).
    /// Two-letter all-uppercase abbreviations and the <c>ID</c> abbreviation follow
    /// the C# naming guidelines and are handled before the original-casing check.
    /// Returns an empty string when the input is null, empty, or contains no word characters.
    /// </summary>
    public static string Render(
        string? input,
        IdentifierCaseStyle style,
        AcronymRegistry acronyms)
        => RenderAccepted(input, style, acronyms)[0];

    /// <summary>
    /// Renders every accepted spelling of the input. Element <c>[0]</c> is the preferred /
    /// canonical form (identical to <see cref="Render"/>): "original casing wins" — as long
    /// as a source word carries any uppercase character, its casing is preserved.
    ///
    /// Additional elements are produced when, for a Pascal / non-first Camel position, a
    /// source word that already carries uppercase has a registered acronym on the same
    /// upper-invariant key with a different casing. This lets callers accept an author's
    /// deliberate alternate casing (e.g. user pins <c>Lcy</c> to accept <c>...BalanceLcy</c>
    /// alongside the canonical <c>...BalanceLCY</c>) without changing what the CodeFix
    /// suggests. The result is the deduplicated cross product of per-word alternatives.
    /// Snake and Kebab styles as well as the Camel first word never produce alternates
    /// (they are always fully lowercased). Raw style never produces alternates (the input
    /// is emitted verbatim). Callers that only need the preferred form should call
    /// <see cref="Render"/>.
    /// </summary>
    public static IReadOnlyList<string> RenderAccepted(
        string? input,
        IdentifierCaseStyle style,
        AcronymRegistry acronyms)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new[] { string.Empty };
        }

        // Raw style bypasses word splitting and casing transforms entirely;
        // the input is emitted verbatim.
        if (style == IdentifierCaseStyle.Raw)
        {
            return new[] { input! };
        }

        var words = SplitIntoWords(input!);

        if (words.Count == 0)
        {
            return new[] { string.Empty };
        }

        return RenderStyleAccepted(words, style, acronyms);
    }

    private static IReadOnlyList<string> RenderStyleAccepted(
        IReadOnlyList<string> words,
        IdentifierCaseStyle style,
        AcronymRegistry acronyms)
    {
        switch (style)
        {
            case IdentifierCaseStyle.Pascal:
            case IdentifierCaseStyle.Camel:
                var buckets = new List<IReadOnlyList<string>>(words.Count);

                for (int i = 0; i < words.Count; i++)
                {
                    bool isFirstInCamel = style == IdentifierCaseStyle.Camel && i == 0;
                    buckets.Add(RenderWordAlternatives(words[i], isFirstInCamel, acronyms));
                }

                return CrossProduct(buckets);

            case IdentifierCaseStyle.Snake:
                return new[] { JoinLowered(words, '_') };

            case IdentifierCaseStyle.Kebab:
                return new[] { JoinLowered(words, '-') };

            default:
                return new[] { string.Empty };
        }
    }

    private static string JoinLowered(IReadOnlyList<string> words, char separator)
    {
        var lowered = new List<string>(words.Count);

        foreach (var w in words)
        {
            lowered.Add(w.ToLowerInvariant());
        }

        return string.Join(separator.ToString(), lowered);
    }

    private static IReadOnlyList<string> RenderWordAlternatives(
        string word,
        bool isFirstInCamel,
        AcronymRegistry acronyms)
    {
        // camelCase first word is unconditionally lowercased per C# convention
        // (xmlParser, ioStream, idBadge). Never produces alternates.
        if (isFirstInCamel)
        {
            return new[] { word.ToLowerInvariant() };
        }

        // "ID" is an abbreviation (not an acronym) and is always rendered as "Id"
        // per Microsoft's C# naming guidelines.
        if (word.Equals("ID", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Id" };
        }

        // Two-letter all-uppercase abbreviations (IO, DX, AG, GL, ...) stay uppercase
        // per C# naming guidelines. No alternate: two-letter words are excluded from the
        // registry by design.
        if (word.Length == 2 && IsAllUpper(word))
        {
            return new[] { word };
        }

        // Original casing wins: as soon as the source word carries any uppercase
        // character (e.g. field "VAT Amount" -> word "VAT", or event
        // "OnAfterCalcOverdueBalanceLCY" -> tail word "LCY"), the primary spelling is the
        // original casing with a guaranteed leading uppercase. This is the preferred /
        // canonical form and always occupies element [0].
        //
        // Every registered variant on the same upper-invariant key is *additionally
        // accepted* when its stored casing differs from the primary. Example: registry
        // holds ["UoM", "Uom"] and the source carries "UOM" -> alternatives are
        // ["UOM", "UoM", "Uom"]. Element [0] remains the preferred spelling suggested by
        // any CodeFix.
        if (HasAnyUpper(word))
        {
            var primary = EnsureUpperFirst(word);

            if (acronyms.TryGetVariants(word, out var variants))
            {
                var alternatives = new List<string>(variants.Count + 1) { primary };

                foreach (var variant in variants)
                {
                    if (!string.Equals(variant, primary, StringComparison.Ordinal))
                    {
                        alternatives.Add(variant);
                    }
                }

                return alternatives;
            }

            return new[] { primary };
        }

        // Word is all-lowercase (e.g. field "vat amount", or the source was normalised
        // upstream). Only in this ambiguous case does the registry supply a canonical.
        if (acronyms.TryGetCanonical(word, out var canonical))
        {
            return new[] { canonical };
        }

        return new[] { EnsureUpperFirst(word) };
    }

    private static IReadOnlyList<string> CrossProduct(
        IReadOnlyList<IReadOnlyList<string>> buckets)
    {
        // Start with a single empty accumulator. Extend once per bucket; when the bucket
        // has a single alternative, append in place (no allocation of a new outer list).
        var accumulators = new List<StringBuilder>(1) { new StringBuilder() };

        foreach (var bucket in buckets)
        {
            if (bucket.Count == 1)
            {
                var only = bucket[0];

                foreach (var sb in accumulators)
                {
                    sb.Append(only);
                }

                continue;
            }

            var next = new List<StringBuilder>(accumulators.Count * bucket.Count);

            foreach (var sb in accumulators)
            {
                var prefix = sb.ToString();

                foreach (var alt in bucket)
                {
                    var combined = new StringBuilder(prefix.Length + alt.Length);
                    combined.Append(prefix);
                    combined.Append(alt);
                    next.Add(combined);
                }
            }

            accumulators = next;
        }

        // Materialise and dedup, preserving the first-seen order so element [0] stays the
        // preferred spelling.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>(accumulators.Count);

        foreach (var sb in accumulators)
        {
            var s = sb.ToString();

            if (seen.Add(s))
            {
                result.Add(s);
            }
        }

        return result;
    }

    private static bool HasAnyUpper(string s)
    {
        foreach (var c in s)
        {
            if (char.IsUpper(c))
            {
                return true;
            }
        }

        return false;
    }

    private static string EnsureUpperFirst(string word)
    {
        if (word.Length == 0)
        {
            return word;
        }

        return char.IsUpper(word[0])
            ? word
            : char.ToUpperInvariant(word[0]) + word.Substring(1);
    }

    private static bool IsAllUpper(string s)
    {
        foreach (var c in s)
        {
            if (!char.IsUpper(c))
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> SplitIntoWords(string input)
    {
        var result = new List<string>();
        var parts = input.Split(WordDelimiters, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            foreach (var word in SplitPascalCase(part))
            {
                if (word.Length > 0)
                {
                    result.Add(word);
                }
            }
        }

        return result;
    }

    private static IEnumerable<string> SplitPascalCase(string word)
    {
        if (word.Length == 0)
        {
            yield break;
        }

        int start = 0;

        for (int i = 1; i < word.Length; i++)
        {
            if (char.IsUpper(word[i]))
            {
                bool prevIsLower = char.IsLower(word[i - 1]);
                bool nextIsLower = i + 1 < word.Length && char.IsLower(word[i + 1]);

                if (prevIsLower || (nextIsLower && i - start > 1))
                {
                    yield return word.Substring(start, i - start);
                    start = i;
                }
            }
        }

        yield return word.Substring(start);
    }
}
