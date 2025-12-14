using ALCops.Common.Reflection;

namespace ALCops.Common.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Quotes the identifier if needed, using reflection to handle breaking changes
    /// between version 17.0.29.41701 and 17.0.29.44223 of Microsoft.Dynamics.Nav.CodeAnalysis 
    /// </summary>
    /// <param name="value">The identifier value to quote if needed.</param>
    /// <param name="useRelaxedIdentifierRules">Whether to use relaxed identifier rules (only used in newer versions).</param>
    /// <returns>The quoted identifier if quoting is needed, otherwise the original value.</returns>
    public static string QuoteIdentifierIfNeededWithReflection(this string value, bool useRelaxedIdentifierRules = false)
        => StringHelper.QuoteIdentifierIfNeeded(value, useRelaxedIdentifierRules);
}
