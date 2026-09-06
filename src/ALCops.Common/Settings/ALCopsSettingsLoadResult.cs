using System.Collections.Immutable;

namespace ALCops.Common.Settings;

/// <summary>
/// The outcome of loading an alcops.json configuration: the effective settings
/// plus any failures encountered while producing them. Unreadable or invalid files
/// yield defaults; requested cancellation instead propagates without caching a result.
/// </summary>
public sealed class ALCopsSettingsLoadResult
{
    internal ALCopsSettingsLoadResult(ALCopsSettings settings, ImmutableArray<SettingsLoadFailure> failures)
    {
        Settings = settings;
        Failures = failures;
    }

    public ALCopsSettings Settings { get; }

    public ImmutableArray<SettingsLoadFailure> Failures { get; }
}
