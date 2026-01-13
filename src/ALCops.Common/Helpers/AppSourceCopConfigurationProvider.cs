using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class AppSourceCopConfigurationProvider
{
    public static AppSourceCopConfiguration? GetAppSourceCopConfiguration(Compilation compilation)
    {
        var appSourceCopConf =
            Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration.AppSourceCopConfigurationProvider
                .GetAppSourceCopConfiguration(compilation);

        return AppSourceCopConfiguration.From(appSourceCopConf);
    }
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