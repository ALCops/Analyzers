using System.Text.RegularExpressions;
using ALCops.Common.Extensions;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;

namespace ALCops.ApplicationCop.Analyzers;

internal static class AppManifestRuleHelper
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    public static NavAppManifest? GetManifest(CompilationAnalysisContext context)
    {
        try
        {
            return ManifestHelper.GetManifest(context.Compilation);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public static bool HasConfiguredPattern(NamingPattern? pattern) =>
        !string.IsNullOrWhiteSpace(pattern?.AllowPattern) ||
        !string.IsNullOrWhiteSpace(pattern?.DisallowPattern);

    public static bool ViolatesPattern(string value, NamingPattern pattern, out string reason)
    {
        if (!string.IsNullOrWhiteSpace(pattern.AllowPattern))
        {
            if (!TryMatch(value, pattern.AllowPattern, out bool matches, out string? error))
            {
                reason = error!;
                return true;
            }

            if (!matches)
            {
                reason = "does not match AllowPattern";
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(pattern.DisallowPattern))
        {
            if (!TryMatch(value, pattern.DisallowPattern, out bool matches, out string? error))
            {
                reason = error!;
                return true;
            }

            if (matches)
            {
                reason = "matches DisallowPattern";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryMatch(string value, string pattern, out bool matches, out string? error)
    {
        try
        {
            matches = Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, RegexTimeout);
            error = null;
            return true;
        }
        catch (ArgumentException)
        {
            matches = false;
            error = "has an invalid configured regular expression";
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            matches = false;
            error = "could not be checked because its configured regular expression timed out";
            return false;
        }
    }
}
