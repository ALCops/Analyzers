namespace ALCops.Common.Settings;

public sealed class ManifestSettings
{
    public NamingPattern? Publisher { get; set; }
    public NamingPattern? ContextSensitiveHelpUrl { get; set; }
    public ManifestResourceExposurePolicySettings? ResourceExposurePolicy { get; set; }
    public string[]? RequiredFeatures { get; set; }
    public bool DisallowSuppressWarnings { get; set; }
    public bool VersionMajorMatchesApplication { get; set; }
}

public sealed class ManifestResourceExposurePolicySettings
{
    public bool? AllowDebugging { get; set; }
    public bool? AllowDownloadingSource { get; set; }
    public bool? IncludeSourceInSymbolFile { get; set; }
    public bool? ApplyToDevExtension { get; set; }
}
