namespace ALCops.Common.Helpers;

/// <summary>
/// Case-insensitive registry of acronyms whose canonical casing should be used when
/// generating identifier fragments from all-lowercase source words. Used by analyzers
/// that render or validate identifiers derived from natural-language input (e.g. field
/// names). The registry is intentionally the last-resort casing source: callers
/// implement "original casing wins" and only consult the registry when the source word
/// carries no case signal (i.e. it is all-lowercase), so that BC-domain terminology
/// like <c>LCY</c>, <c>VAT</c>, <c>OData</c>, <c>UoM</c> is recovered from lowered
/// input without ever re-casting Microsoft- or partner-owned identifiers whose
/// original casing is already unambiguous.
///
/// The registry supports <b>multiple registered variants per case-insensitive key</b>.
/// The first-added variant is the <i>preferred/canonical</i> form (returned by
/// <see cref="TryGetCanonical"/> and used as the CodeFix suggestion). All registered
/// variants for a key are exposed via <see cref="TryGetVariants"/> and are additionally
/// accepted alongside the source's original casing. Example: defaults list both
/// <c>UoM</c> (canonical) and <c>Uom</c>; a subscriber that wrote either — or <c>UOM</c>
/// via "original casing wins" — is accepted.
///
/// The registry ships a curated default list (<see cref="DefaultAcronyms"/>, a flat
/// array) and can be extended per project via <see cref="Create"/> (also a flat list).
/// When the user supplies any entries for a given upper-invariant key, those entries
/// <b>displace</b> the built-in variants for that key (the user list is authoritative
/// per key). User-supplied entries for the same key accumulate in the order provided.
/// </summary>
public sealed class AcronymRegistry
{
    /// <summary>
    /// Curated default acronyms. Includes common BC domain abbreviations,
    /// web/data protocols commonly used in BC field names and general
    /// business terms. The list is stored in canonical output casing,
    /// including project/domain-specific variants where needed.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultAcronyms = new[]
    {
        "Aad", "Abc", "Acy", "Adcs", "Api", "Arc", "Ascii", "Ato", "Bcc",
        "Bic", "Blob", "BoM", "Bom", "Bop", "Bwr", "Cal", "Cds", "Cogs",
        "Crm", "Csv", "Dach", "Dtd", "Dvr", "Ecsl", "Emu", "Eori", "Fefo",
        "Gln", "Gtin", "Guid", "Html", "Iban", "Id", "Iso", "Isv", "Json",
        "Kpi", "Lcid", "Lcy", "Lid", "Mps", "Mrp", "Nav", "Ocr", "Oob",
        "Pbix", "Pdf", "Pfx", "Qbd", "Sepa", "Sic", "Sid", "Sift", "Sku",
        "Smtp", "Sqm", "Swift", "Uid", "UoM", "Uom", "Ups", "Uri", "Url",
        "Urs", "Utc", "Utf", "Vat", "Wip", "Wms", "Xml", "Xsd", "Ytd"
    };

    private readonly Dictionary<string, List<string>> _variantsByKey;

    private AcronymRegistry(Dictionary<string, List<string>> variantsByKey)
    {
        _variantsByKey = variantsByKey;
    }

    /// <summary>
    /// Registry populated with <see cref="DefaultAcronyms"/> only.
    /// </summary>
    public static AcronymRegistry Default { get; } = Create(userAcronyms: null);

    /// <summary>
    /// Builds a registry containing <see cref="DefaultAcronyms"/> merged with the caller's
    /// list. When the user supplies any entries for a given case-insensitive key, those
    /// entries displace the built-in variants for that key (user list is authoritative
    /// per key). Multiple user entries under the same key accumulate in the order
    /// supplied; the first becomes the preferred/canonical form. <c>null</c>, empty or
    /// whitespace entries in <paramref name="userAcronyms"/> are ignored. Each surviving
    /// entry is trimmed.
    /// </summary>
    public static AcronymRegistry Create(IEnumerable<string>? userAcronyms)
    {
        var variantsByKey = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var acronym in DefaultAcronyms)
        {
            AddVariantIfValid(variantsByKey, acronym);
        }

        if (userAcronyms is not null)
        {
            var displacedKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var acronym in userAcronyms)
            {
                if (string.IsNullOrWhiteSpace(acronym))
                {
                    continue;
                }

                var trimmed = acronym!.Trim();
                var key = trimmed.ToUpperInvariant();

                if (displacedKeys.Add(key))
                {
                    // First time this key appears in the user list: wipe built-in
                    // variants so the user list is authoritative for this key.
                    variantsByKey.Remove(key);
                }

                AddVariantToKey(variantsByKey, key, trimmed);
            }
        }

        return new AcronymRegistry(variantsByKey);
    }

    private static void AddVariantIfValid(Dictionary<string, List<string>> target, string? acronym)
    {
        if (string.IsNullOrWhiteSpace(acronym))
        {
            return;
        }

        var trimmed = acronym!.Trim();
        AddVariantToKey(target, trimmed.ToUpperInvariant(), trimmed);
    }

    private static void AddVariantToKey(Dictionary<string, List<string>> target, string key, string variant)
    {
        if (!target.TryGetValue(key, out var list))
        {
            list = new List<string>(capacity: 1);
            target[key] = list;
        }

        // Case-sensitive dedup: exact-cased duplicates are collapsed.
        foreach (var existing in list)
        {
            if (string.Equals(existing, variant, StringComparison.Ordinal))
            {
                return;
            }
        }

        list.Add(variant);
    }

    /// <summary>
    /// Attempts to find the preferred canonical casing for a word (case-insensitive
    /// lookup). The canonical is the <b>first-added</b> registered variant for the
    /// upper-invariant key. Returns <c>true</c> when the word is a registered acronym.
    /// </summary>
    public bool TryGetCanonical(string word, out string canonical)
    {
        if (TryGetVariants(word, out var variants))
        {
            canonical = variants[0];

            return true;
        }

        canonical = string.Empty;

        return false;
    }

    /// <summary>
    /// Attempts to return all registered variants for a word (case-insensitive lookup).
    /// The list is in insertion order; the first entry is the preferred canonical form,
    /// remaining entries are additionally accepted alternate spellings. Returns
    /// <c>true</c> when the word is a registered acronym.
    /// </summary>
    public bool TryGetVariants(string word, out IReadOnlyList<string> variants)
    {
        if (string.IsNullOrEmpty(word))
        {
            variants = Array.Empty<string>();

            return false;
        }

        if (_variantsByKey.TryGetValue(word.ToUpperInvariant(), out var list))
        {
            variants = list;

            return true;
        }

        variants = Array.Empty<string>();

        return false;
    }
}
