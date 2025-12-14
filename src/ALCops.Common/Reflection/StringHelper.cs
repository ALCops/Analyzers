using System.Reflection;

namespace ALCops.Common.Reflection;

public static class StringHelper
{
    // Cache the method info for QuoteIdentifierIfNeeded with the optional parameter >= 17.0.29.44223
    private static readonly Lazy<MethodInfo?> _quoteIdentifierIfNeededWithParam =
        new(() => typeof(Microsoft.Dynamics.Nav.CodeAnalysis.Utilities.StringExtensions)
            .GetMethod(nameof(Microsoft.Dynamics.Nav.CodeAnalysis.Utilities.StringExtensions.QuoteIdentifierIfNeeded),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(bool)],
                null));

    // Cache the method info for QuoteIdentifierIfNeeded without the optional parameter <= 17.0.29.41701
    private static readonly Lazy<MethodInfo?> _quoteIdentifierIfNeededWithoutParam =
        new(() => typeof(Microsoft.Dynamics.Nav.CodeAnalysis.Utilities.StringExtensions)
            .GetMethod(nameof(Microsoft.Dynamics.Nav.CodeAnalysis.Utilities.StringExtensions.QuoteIdentifierIfNeeded),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string)],
                null));

    /// <summary>
    /// Quotes the identifier if needed, handling breaking changes between different versions
    /// of Microsoft.Dynamics.Nav.CodeAnalysis where the useRelaxedIdentifierRules parameter
    /// was added in newer versions.
    /// </summary>
    /// <param name="value">The identifier value to quote if needed.</param>
    /// <param name="useRelaxedIdentifierRules">Whether to use relaxed identifier rules (only used in newer versions).</param>
    /// <returns>The quoted identifier if quoting is needed, otherwise the original value.</returns>
    public static string QuoteIdentifierIfNeeded(string value, bool useRelaxedIdentifierRules = false)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Try the newer version with the bool parameter first
        var methodWithParam = _quoteIdentifierIfNeededWithParam.Value;
        if (methodWithParam != null)
        {
            return (string)methodWithParam.Invoke(null, [value, useRelaxedIdentifierRules])!;
        }

        // Fall back to the older version without the bool parameter
        var methodWithoutParam = _quoteIdentifierIfNeededWithoutParam.Value;
        if (methodWithoutParam != null)
        {
            return (string)methodWithoutParam.Invoke(null, [value])!;
        }

        // If neither method is found, return the value as-is (shouldn't happen in practice)
        return value;
    }
}
