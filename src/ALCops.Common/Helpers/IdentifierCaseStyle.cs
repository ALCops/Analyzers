namespace ALCops.Common.Helpers;

/// <summary>
/// Output style for identifiers generated from natural-language input via
/// <see cref="IdentifierNameRenderer"/>.
/// </summary>
public enum IdentifierCaseStyle
{
    /// <summary>PascalCase — every word first-letter uppercased.</summary>
    Pascal,

    /// <summary>camelCase — first word fully lowercased, subsequent words pascal.</summary>
    Camel,

    /// <summary>snake_case — every word fully lowercased, joined with underscores.</summary>
    Snake,

    /// <summary>kebab-case — every word fully lowercased, joined with hyphens.</summary>
    Kebab,

    /// <summary>
    /// Raw — input emitted verbatim without word-splitting, casing changes, or acronym transforms.
    /// Preserves whitespace and special characters as they appear in the source (e.g. BC object
    /// names like <c>Sales Header</c> or field names like <c>Line Discount %</c>). The consumer is
    /// responsible for quoting the resulting identifier when it contains characters requiring it.
    /// </summary>
    Raw
}
