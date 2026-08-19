using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Helpers;

/// <summary>
/// Shared logic for AppSourceCop mandatory affixes (mandatoryPrefix, mandatorySuffix and
/// mandatoryAffixes). Mirrors the SDK's loose semantics where every configured value is a
/// candidate at either end of a name (see AppSourceCop's RuleIdentifiersMustHaveValidAffixes
/// and AppSourceCopConfigurationProvider.GetMandatoryNameAffixes).
/// </summary>
public static class MandatoryAffixes
{
    /// <summary>
    /// Returns the merged, distinct, non-empty list of mandatory affixes for the compilation.
    /// Returns an empty array when no AppSourceCop.json is present or no affixes are configured.
    /// Delegates to the SDK's own merge (AppSourceCopConfigurationProvider.GetMandatoryNameAffixes),
    /// which re-reads the configuration on every call; cache the result per compilation at the
    /// call site (e.g. a CompilationStartAction closure or a ConditionalWeakTable).
    /// </summary>
    public static string[] GetAffixes(Compilation compilation)
        => AppSourceCopConfigurationProvider.GetMandatoryNameAffixes(compilation);

    /// <summary>
    /// Returns the index of the first character after a leading affix, or <c>null</c> when the
    /// name does not start with any affix or no character follows the affix.
    /// </summary>
    public static int? GetIndexAfterLeadingAffix(string name, string[] affixes)
    {
        foreach (string affix in affixes)
        {
            if (name.Length > affix.Length && name.StartsWith(affix, SemanticFacts.NameEqualityComparison))
                return affix.Length;
        }

        return null;
    }

    /// <summary>
    /// Removes at most one affix from the start and at most one affix from the end of the name,
    /// trimming residual whitespace after each removal. Never returns an empty string: a removal
    /// that would consume the entire name is skipped.
    /// </summary>
    public static string StripAffixes(string name, string[] affixes)
        => StripTrailingAffix(StripLeadingAffix(name, affixes), affixes);

    private static string StripLeadingAffix(string name, string[] affixes)
    {
        foreach (string affix in affixes)
        {
            if (name.Length <= affix.Length || !name.StartsWith(affix, SemanticFacts.NameEqualityComparison))
                continue;

            string stripped = name.Substring(affix.Length).TrimStart();
            if (stripped.Length > 0)
                return stripped;
        }

        return name;
    }

    private static string StripTrailingAffix(string name, string[] affixes)
    {
        foreach (string affix in affixes)
        {
            if (name.Length <= affix.Length || !name.EndsWith(affix, SemanticFacts.NameEqualityComparison))
                continue;

            string stripped = name.Substring(0, name.Length - affix.Length).TrimEnd();
            if (stripped.Length > 0)
                return stripped;
        }

        return name;
    }
}
