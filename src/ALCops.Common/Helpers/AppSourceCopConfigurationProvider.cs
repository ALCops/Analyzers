using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

// Acts as a lightweight adapter between ALCops analyzers and the AppSourceCop configuration
// This indirection avoids an ALCops analyzers to take a direct dependency on
// Microsoft.Dynamics.Nav.Analyzers.Common, while still exposing the required configuration
public static class AppSourceCopConfigurationProvider
{
    public static AppSourceCopConfiguration? GetAppSourceCopConfiguration(Compilation compilation)
    {
        var appSourceCopConf =
            Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration.AppSourceCopConfigurationProvider
                .GetAppSourceCopConfiguration(compilation);

        return AppSourceCopConfiguration.From(appSourceCopConf);
    }

    /// <summary>
    /// Returns the merged, distinct, non-empty list of mandatory affixes (mandatoryPrefix,
    /// mandatorySuffix and mandatoryAffixes) for the compilation's AppSourceCop.json, or an
    /// empty array when none is present. Delegates to the SDK so the merge semantics stay
    /// identical to AppSourceCop's own affix validation. Note: unlike
    /// <see cref="GetAppSourceCopConfiguration"/>, the underlying SDK overload re-reads the
    /// configuration on every call; cache the result per compilation at the call site.
    /// </summary>
    public static string[] GetMandatoryNameAffixes(Compilation compilation)
        => Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration.AppSourceCopConfigurationProvider
            .GetMandatoryNameAffixes(compilation);
}

public sealed class AppSourceCopConfiguration
{
#if NETSTANDARD2_1
    public string[]? MandatoryAffixes { get; set; }
    public string? MandatorySuffix { get; set; }
    public string? MandatoryPrefix { get; set; }
#else
    public string[]? MandatoryAffixes { get; init; }
    public string? MandatorySuffix { get; init; }
    public string? MandatoryPrefix { get; init; }
#endif

    internal static AppSourceCopConfiguration? From(
        Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration.AppSourceCopConfiguration? appSourceCopConf)
    {
        if (appSourceCopConf is null)
            return null;

        return new AppSourceCopConfiguration
        {
            MandatoryAffixes = appSourceCopConf.MandatoryAffixes,
            MandatorySuffix = appSourceCopConf.MandatorySuffix,
            MandatoryPrefix = appSourceCopConf.MandatoryPrefix
        };
    }
}