#if !NET10_0_OR_GREATER
using ALCops.Common.Reflection;
#endif

using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Null-tolerant wrapper around <see cref="SemanticFacts.IsSameName(string, string)"/>.
    /// Returns <c>false</c> when either argument is <c>null</c>.
    /// Use for AL identifier comparisons where the source value may be nullable
    /// (e.g. <c>SyntaxToken.ValueText</c>).
    /// </summary>
    public static bool IsSameName(this string? nameA, string? nameB)
        => nameA is not null && nameB is not null && SemanticFacts.IsSameName(nameA, nameB);

    /// <summary>
    /// Quotes the identifier if needed.
    /// COMPAT(netstandard2.1, net8.0): Uses reflection to handle breaking changes
    /// between version 17.0.29.41701 and 17.0.29.44223 of Microsoft.Dynamics.Nav.CodeAnalysis
    /// where the useRelaxedIdentifierRules parameter was added.
    /// On net10.0+, the SDK has the two-parameter overload so we call it directly.
    /// TODO: When netstandard2.1 and net8.0 are dropped, replace all calls with
    /// Microsoft.Dynamics.Nav.CodeAnalysis.Utilities.StringExtensions.QuoteIdentifierIfNeeded()
    /// and delete Reflection/StringHelper.cs.
    /// </summary>
    /// <param name="value">The identifier value to quote if needed.</param>
    /// <param name="useRelaxedIdentifierRules">Whether to use relaxed identifier rules (only used in newer versions).</param>
    /// <returns>The quoted identifier if quoting is needed, otherwise the original value.</returns>
    public static string QuoteIdentifierIfNeededWithReflection(this string value, bool useRelaxedIdentifierRules = false)
#if NET10_0_OR_GREATER
        => Microsoft.Dynamics.Nav.CodeAnalysis.Utilities.StringExtensions.QuoteIdentifierIfNeeded(value, useRelaxedIdentifierRules);
#else
        => StringHelper.QuoteIdentifierIfNeeded(value, useRelaxedIdentifierRules);
#endif
}
