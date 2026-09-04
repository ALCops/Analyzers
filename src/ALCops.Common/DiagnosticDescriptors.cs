using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.Common;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ConfigurationCouldNotBeLoaded = new(
        id: DiagnosticIds.ConfigurationCouldNotBeLoaded,
        title: CommonAnalyzers.ConfigurationCouldNotBeLoadedTitle,
        messageFormat: CommonAnalyzers.ConfigurationCouldNotBeLoadedMessageFormat,
        category: Category.Configuration,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CommonAnalyzers.ConfigurationCouldNotBeLoadedDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ConfigurationCouldNotBeLoaded));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://alcops.dev/docs/analyzers/common/{0}/", identifier.ToLowerInvariant());
    }

    /// <summary>
    /// Categories used to group diagnostics. These follow Roslyn conventions,
    /// and make it easy to filter diagnostics in rulesets or suppressions.
    /// </summary>
    internal static class Category
    {
        /// <summary>
        /// Configuration issues: problems with the ALCops configuration itself
        /// (for example an alcops.json that cannot be loaded), not problems in user code.
        /// </summary>
        public const string Configuration = "Configuration";
    }
}
