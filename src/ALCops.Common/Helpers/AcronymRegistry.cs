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
/// When a source word already carries uppercase but the registry contains an entry with
/// a different casing under the same upper-invariant key (e.g. source <c>LCY</c> with a
/// user-registered <c>Lcy</c>), that entry is additionally accepted as a valid variant
/// alongside the original spelling. The original casing remains the preferred/canonical
/// form suggested by any CodeFix.
///
/// The registry ships a curated default list (<see cref="DefaultAcronyms"/>) and can be
/// extended per project via <see cref="Create"/>. User entries override built-ins when they
/// share the same case-insensitive key.
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
        "Bic", "Blob", "Bom", "BoM", "Bop", "Bwr", "Cal", "Cds", "Cogs",
        "Crm", "Csv", "Dach", "Dtd", "Dvr", "Ecsl", "Emu", "Eori", "Fefo",
        "Gln", "Gtin", "Guid", "Html", "Iban", "Id", "Iso", "Isv", "Json",
        "Kpi", "Lcid", "Lcy", "Lid", "Mps", "Mrp", "Nav", "Ocr", "Oob",
        "Pbix", "Pdf", "Pfx", "Qbd", "Sepa", "Sic", "Sid", "Sift", "Sku",
        "Smtp", "Sqm", "Swift", "Uid", "Uom", "UoM", "Ups", "Uri", "Url",
        "Urs", "Utc", "Utf", "Vat", "Wip", "Wms", "Xml", "Xsd", "Ytd"
    };

    private readonly Dictionary<string, string> _canonicalByKey;

    private AcronymRegistry(Dictionary<string, string> canonicalByKey)
    {
        _canonicalByKey = canonicalByKey;
    }

    /// <summary>
    /// Registry populated with <see cref="DefaultAcronyms"/> only.
    /// </summary>
    public static AcronymRegistry Default { get; } = Create(userAcronyms: null);

    /// <summary>
    /// Builds a registry containing <see cref="DefaultAcronyms"/> merged with the caller's
    /// list. User entries override built-in canonical casing when their upper-invariant
    /// form matches. <c>null</c>, empty or whitespace entries in <paramref name="userAcronyms"/>
    /// are ignored. Each surviving entry is trimmed.
    /// </summary>
    public static AcronymRegistry Create(IEnumerable<string>? userAcronyms)
    {
        var canonicalByKey = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var acronym in DefaultAcronyms)
        {
            AddIfValid(canonicalByKey, acronym);
        }

        if (userAcronyms is not null)
        {
            foreach (var acronym in userAcronyms)
            {
                AddIfValid(canonicalByKey, acronym);
            }
        }

        return new AcronymRegistry(canonicalByKey);
    }

    private static void AddIfValid(Dictionary<string, string> target, string? acronym)
    {
        if (string.IsNullOrWhiteSpace(acronym))
        {
            return;
        }

        var trimmed = acronym!.Trim();
        target[trimmed.ToUpperInvariant()] = trimmed;
    }

    /// <summary>
    /// Attempts to find the canonical casing for a word (case-insensitive lookup).
    /// Returns <c>true</c> when the word is a registered acronym.
    /// </summary>
    public bool TryGetCanonical(string word, out string canonical)
    {
        if (string.IsNullOrEmpty(word))
        {
            canonical = string.Empty;

            return false;
        }

        if (_canonicalByKey.TryGetValue(word.ToUpperInvariant(), out var value))
        {
            canonical = value;

            return true;
        }

        canonical = string.Empty;

        return false;
    }
}
