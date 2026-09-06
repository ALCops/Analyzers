using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.Dynamics.Nav.CodeAnalysis;
#if NETSTANDARD2_1
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace ALCops.Common.Settings;

/// <summary>
/// Shares successfully loaded settings across a workspace and keeps a consistent snapshot per
/// compilation. Failed HTTP requests are retried by later compilations; cancellation is never cached.
/// </summary>
public static class ALCopsSettingsProvider
{
    private static readonly ConcurrentDictionary<string, ALCopsSettingsCacheEntry> _cache = new();
    private static readonly ConditionalWeakTable<Compilation, ALCopsSettingsCacheEntry> _compilationCache = new();
    private const string SettingsFileName = "alcops.json";

    public static ALCopsSettings GetSettings(Compilation compilation, CancellationToken cancellationToken) =>
        GetLoadResult(compilation, cancellationToken).Settings;

    /// <summary>All callbacks for a compilation share the same settings and failures, independent of callback order.</summary>
    public static ALCopsSettingsLoadResult GetLoadResult(Compilation compilation, CancellationToken cancellationToken) =>
        _compilationCache.GetValue(compilation, _ => new ALCopsSettingsCacheEntry()).GetOrLoad(
            () => GetLoadResult(compilation.FileSystem, cancellationToken), cacheHttpFailures: true, cancellationToken);

    public static ALCopsSettings GetSettings(IFileSystem? fileSystem, CancellationToken cancellationToken = default) =>
        GetLoadResult(fileSystem, cancellationToken).Settings;

    /// <summary>
    /// Loads outside a compilation snapshot. Analyzer callbacks use the Compilation overload so
    /// failed HTTP requests are attempted at most once in each compilation, not once per callback.
    /// </summary>
    public static ALCopsSettingsLoadResult GetLoadResult(IFileSystem? fileSystem, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (fileSystem is null)
            return Defaults();
        string directoryPath = fileSystem.GetDirectoryPath();
        if (string.IsNullOrEmpty(directoryPath))
            return LoadSettingsFromFileSystem(fileSystem, directoryPath, cancellationToken);
        return _cache.GetOrAdd(directoryPath, _ => new ALCopsSettingsCacheEntry()).GetOrLoad(
            () => LoadSettingsFromFileSystem(fileSystem, directoryPath, cancellationToken), cacheHttpFailures: false, cancellationToken);
    }

    private static ALCopsSettingsLoadResult Defaults() => new(new ALCopsSettings(), ImmutableArray<SettingsLoadFailure>.Empty);

    private static ALCopsSettingsLoadResult LoadSettingsFromFileSystem(IFileSystem fileSystem, string directoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? json = TryReadFromVirtualFileSystem(fileSystem, directoryPath, out var readFailure);
        if (json is not null)
            return DeserializeSettings(json, GetVirtualSource(directoryPath), cancellationToken);

        // An unreadable app-folder file was intended to win; never use a parent file instead.
        if (readFailure is not null)
            return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray.Create(readFailure));

        if (!string.IsNullOrEmpty(directoryPath))
        {
            string? path = FindSettingsFileInParentOrAssemblyDirectory(directoryPath);
            if (path is not null)
            {
                string physicalJson;
                try { physicalJson = File.ReadAllText(path); }
                catch (Exception ex)
                {
                    return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray.Create(
                        new SettingsLoadFailure(SettingsLoadFailureKind.Unreadable, path, ex.Message)));
                }
                return DeserializeSettings(physicalJson, path, cancellationToken);
            }
        }
        return Defaults();
    }

    private static string? TryReadFromVirtualFileSystem(IFileSystem fileSystem, string directoryPath, out SettingsLoadFailure? failure)
    {
        failure = null;

        bool exists;
        try
        {
            exists = fileSystem.Exists(SettingsFileName);
        }
        catch (Exception)
        {
            // Exists has no defined exception contract across implementations; treat as absent.
            return null;
        }

        if (!exists)
            return null;

        try
        {
            using Stream stream = fileSystem.OpenRead(SettingsFileName);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            failure = new SettingsLoadFailure(SettingsLoadFailureKind.Unreadable, GetVirtualSource(directoryPath), ex.Message);
            return null;
        }
    }

    private static string GetVirtualSource(string directoryPath)
    {
        // IFileSystem.GetAbsolutePath does not exist at the oldest SDK the netstandard2.1
        // binary runs on (AL 12), so build the display path from the directory instead.
        if (string.IsNullOrEmpty(directoryPath))
            return SettingsFileName;

        return Path.Combine(directoryPath, SettingsFileName);
    }

    private static ALCopsSettingsLoadResult DeserializeSettings(string json, string source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ALCopsSettingsDocument? local = ALCopsSettingsDocument.ParseLocal(json);
            if (local is null)
                return Defaults();

            // Validate local values before network access. This is still required when the
            // effective settings will later be deserialized from the merged document.
            ALCopsSettings localSettings = local.DeserializeSettings();
            ImmutableArray<SettingsLoadFailure> localFailures = local.GetUnknownSettingFailures(source);
            if (!local.HasExtends)
                return new ALCopsSettingsLoadResult(localSettings, localFailures);

            if (ALCopsSettingsInheritanceResolver.TryResolve(local, source, out string inheritedSource,
                    out ALCopsSettingsDocument? inherited, out SettingsLoadFailure? failure, cancellationToken))
            {
                try
                {
                    // Invalid base values must not be hidden by local overrides.
                    _ = inherited!.DeserializeSettings();
                    ImmutableArray<SettingsLoadFailure> failures = localFailures.AddRange(inherited.GetUnknownSettingFailures(inheritedSource));
                    inherited.MergeOverrides(local);
                    return new ALCopsSettingsLoadResult(inherited.DeserializeSettings(), failures);
                }
                catch (JsonException ex)
                {
                    failure = new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, inheritedSource, ex.Message);
                }
            }

            // A declared base and its overrides form one configuration, including on failure.
            return new ALCopsSettingsLoadResult(new ALCopsSettings(), localFailures.Add(failure!));
        }
        catch (JsonException ex)
        {
            return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray.Create(
                new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, source, ex.Message)));
        }
    }

    private static string? FindSettingsFileInParentOrAssemblyDirectory(string directoryPath)
    {
        var settingsFile = FindSettingsFileInParentDirectories(directoryPath);
        if (settingsFile != null)
            return settingsFile;

        var assemblyLocation = Path.GetDirectoryName(typeof(ALCopsSettings).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyLocation) && !string.Equals(assemblyLocation, directoryPath, StringComparison.OrdinalIgnoreCase))
            return FindSettingsFileInDirectory(assemblyLocation);

        return null;
    }

    private static string? FindSettingsFileInParentDirectories(string startingPath)
    {
        try
        {
            var parent = Directory.GetParent(startingPath);
            while (parent != null)
            {
                var settingsFile = FindSettingsFileInDirectory(parent.FullName);
                if (settingsFile != null)
                    return settingsFile;

                parent = parent.Parent;
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Stop traversal at inaccessible directory
        }
        catch (IOException)
        {
            // Stop traversal on I/O errors
        }

        return null;
    }

    private static string? FindSettingsFileInDirectory(string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
            return null;

        var settingsFilePath = Path.Combine(directoryPath, SettingsFileName);
        if (File.Exists(settingsFilePath))
            return settingsFilePath;

        if (!Directory.Exists(directoryPath))
            return null;

        return Directory.EnumerateFiles(directoryPath, "*.json")
            .FirstOrDefault(f => string.Equals(
                Path.GetFileName(f), SettingsFileName, StringComparison.OrdinalIgnoreCase));
    }
}
