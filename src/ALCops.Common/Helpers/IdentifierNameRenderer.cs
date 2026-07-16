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
/// suggest table/field/procedure names). Emits a single canonical spelling per input;
/// callers that validate user-written identifiers compare byte-for-byte against the result.
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
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Raw style bypasses word splitting and casing transforms entirely;
        // the input is emitted verbatim. Whitespace and special characters are preserved.
        if (style == IdentifierCaseStyle.Raw)
        {
            return input!;
        }

        var words = SplitIntoWords(input!);

        if (words.Count == 0)
        {
            return string.Empty;
        }

        return RenderStyle(words, style, acronyms);
    }

    private static string RenderStyle(
        IReadOnlyList<string> words,
        IdentifierCaseStyle style,
        AcronymRegistry acronyms)
    {
        switch (style)
        {
            case IdentifierCaseStyle.Pascal:
                var pascal = new StringBuilder();

                foreach (var w in words)
                {
                    pascal.Append(RenderWord(w, isFirstInCamel: false, acronyms));
                }

                return pascal.ToString();

            case IdentifierCaseStyle.Camel:
                var camel = new StringBuilder();
                camel.Append(RenderWord(words[0], isFirstInCamel: true, acronyms));

                for (int i = 1; i < words.Count; i++)
                {
                    camel.Append(RenderWord(words[i], isFirstInCamel: false, acronyms));
                }

                return camel.ToString();

            case IdentifierCaseStyle.Snake:
                return JoinLowered(words, '_');

            case IdentifierCaseStyle.Kebab:
                return JoinLowered(words, '-');

            default:
                return string.Empty;
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

    private static string RenderWord(
        string word,
        bool isFirstInCamel,
        AcronymRegistry acronyms)
    {
        // camelCase first word is unconditionally lowercased per C# convention
        // (xmlParser, ioStream, idBadge).
        if (isFirstInCamel)
        {
            return word.ToLowerInvariant();
        }

        // "ID" is an abbreviation (not an acronym) and is always rendered as "Id"
        // per Microsoft's C# naming guidelines.
        if (word.Equals("ID", StringComparison.OrdinalIgnoreCase))
        {
            return "Id";
        }

        // Two-letter all-uppercase abbreviations (IO, DX, AG, GL, ...) stay uppercase
        // per C# naming guidelines.
        if (word.Length == 2 && IsAllUpper(word))
        {
            return word;
        }

        // Original casing wins: as soon as the source word carries any uppercase
        // character (e.g. field "VAT Amount" -> word "VAT", or "Sales Header" -> word
        // "Sales"), the acronym registry is not consulted. The renderer only
        // guarantees a leading uppercase letter for Pascal / non-first Camel positions
        // and leaves the remaining characters untouched.
        //
        // This prevents the rule from suggesting an identifier whose acronym casing
        // differs from the source object/field name, which would create needless
        // friction with Microsoft- or partner-owned identifiers.
        if (HasAnyUpper(word))
        {
            return EnsureUpperFirst(word);
        }

        // Word is all-lowercase (e.g. field "vat amount", or the source was normalised
        // upstream). Only in this ambiguous case do we consult the shared registry
        // (defaults + user-configured) to recover a canonical casing.
        if (acronyms.TryGetCanonical(word, out var canonical))
        {
            return canonical;
        }

        return EnsureUpperFirst(word);
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
