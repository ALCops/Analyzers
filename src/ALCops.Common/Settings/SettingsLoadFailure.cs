namespace ALCops.Common.Settings;

/// <summary>
/// Classifies why (part of) an alcops.json configuration could not be applied.
/// </summary>
public enum SettingsLoadFailureKind
{
    /// <summary>The file exists but could not be read (I/O or permission error).</summary>
    Unreadable,

    /// <summary>The file content could not be deserialized (invalid JSON, wrong value types, unknown enum values).</summary>
    Invalid,

    /// <summary>A top-level property is not a recognized ALCops setting.</summary>
    UnknownSetting,
}

/// <summary>
/// A single failure recorded while loading an alcops.json configuration,
/// surfaced to users as diagnostic CM0001.
/// </summary>
public sealed class SettingsLoadFailure
{
    public SettingsLoadFailure(SettingsLoadFailureKind kind, string source, string detail)
    {
        Kind = kind;
        Source = source;
        Detail = detail;
    }

    public SettingsLoadFailureKind Kind { get; }

    /// <summary>The file path (or virtual-file name) the configuration was loaded from.</summary>
    public string Source { get; }

    /// <summary>Human-readable reason; may contain OS-localized exception text.</summary>
    public string Detail { get; }
}
