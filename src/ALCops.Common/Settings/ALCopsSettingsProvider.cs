using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
#if NETSTANDARD2_1
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif


namespace ALCops.Common.Settings;

/// <summary>
/// Provides cached access to ALCops settings.
/// Settings are loaded once per workspace path and cached for the analyzer session.
/// </summary>
public static class ALCopsSettingsProvider
{
    private static readonly ConcurrentDictionary<string, ALCopsSettingsLoadResult> _cache = new();
#if NETSTANDARD2_1
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        Converters = { new StringEnumConverter() }
    };
#else
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Must mirror _jsonOptions: a file with comments or trailing commas that deserializes
    // fine would otherwise throw in the unknown-key scan and be misreported as Invalid.
    private static readonly JsonDocumentOptions _documentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
#endif

    private const string SettingsFileName = "alcops.json";
    private const string SchemaKey = "$schema";

    // Both serializers match properties case-insensitively, so the unknown-key check must too.
    private static readonly HashSet<string> _knownTopLevelKeys = BuildKnownTopLevelKeys();

    /// <summary>
    /// Gets the settings from the compilation's file system.
    /// First checks the app folder via the virtual file system, then walks up parent directories
    /// on the physical file system, and finally falls back to the assembly location.
    /// Results are cached per directory path.
    /// </summary>
    public static ALCopsSettings GetSettings(IFileSystem? fileSystem) => GetLoadResult(fileSystem).Settings;

    /// <summary>
    /// Gets the settings plus any failures recorded while loading them (unreadable file,
    /// malformed JSON, unknown top-level settings). Failures are surfaced as diagnostic CM0001
    /// by <see cref="Analyzers.ConfigurationCouldNotBeLoaded"/>; the cache keeps them so every
    /// compilation re-reports.
    /// </summary>
    public static ALCopsSettingsLoadResult GetLoadResult(IFileSystem? fileSystem)
    {
        if (fileSystem is null)
            return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray<SettingsLoadFailure>.Empty);

        string directoryPath = fileSystem.GetDirectoryPath();

        if (string.IsNullOrEmpty(directoryPath))
            return LoadSettingsFromFileSystem(fileSystem, directoryPath);

        return _cache.GetOrAdd(directoryPath, _ => LoadSettingsFromFileSystem(fileSystem, directoryPath));
    }

    private static ALCopsSettingsLoadResult LoadSettingsFromFileSystem(IFileSystem fileSystem, string directoryPath)
    {
        var json = TryReadFromVirtualFileSystem(fileSystem, directoryPath, out var readFailure);
        if (json != null)
            return DeserializeSettings(json, GetVirtualSource(directoryPath));

        // An app-folder alcops.json that exists but cannot be read was intended to win;
        // do not fall through to a parent-directory file.
        if (readFailure != null)
            return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray.Create(readFailure));

        if (!string.IsNullOrEmpty(directoryPath))
        {
            var settingsFilePath = FindSettingsFileInParentOrAssemblyDirectory(directoryPath);
            if (settingsFilePath != null)
            {
                string physicalJson;
                try
                {
                    physicalJson = File.ReadAllText(settingsFilePath);
                }
                catch (Exception ex)
                {
                    return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray.Create(
                        new SettingsLoadFailure(SettingsLoadFailureKind.Unreadable, settingsFilePath, ex.Message)));
                }
                return DeserializeSettings(physicalJson, settingsFilePath);
            }
        }

        return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray<SettingsLoadFailure>.Empty);
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

        try
        {
            return Path.Combine(directoryPath, SettingsFileName);
        }
        catch (ArgumentException)
        {
            return SettingsFileName;
        }
    }

    private static ALCopsSettingsLoadResult DeserializeSettings(string json, string source)
    {
        // Malformed JSON (invalid syntax, unknown enum values, wrong types) falls back to defaults —
        // consumers rely on always getting a usable settings object — and the failure is recorded
        // so CM0001 can tell the user why the file was not applied (issue #328).
        try
        {
#if NETSTANDARD2_1
            var settings = JsonConvert.DeserializeObject<ALCopsSettings>(json, _jsonSettings) ?? new ALCopsSettings();
#else
            var settings = JsonSerializer.Deserialize<ALCopsSettings>(json, _jsonOptions) ?? new ALCopsSettings();
#endif
            // Explicit `null` on nested settings deserializes without JsonException; restore defaults
            // so consumers can rely on the property being non-null.
            settings.StatementBlockSpacing ??= new StatementBlockSpacingSettings();

            var unknownKeys = GetUnknownTopLevelKeys(json);
            if (unknownKeys is null)
                return new ALCopsSettingsLoadResult(settings, ImmutableArray<SettingsLoadFailure>.Empty);

            var failures = unknownKeys
                .Select(key => new SettingsLoadFailure(SettingsLoadFailureKind.UnknownSetting, source, $"unknown setting '{key}'"))
                .ToImmutableArray();
            return new ALCopsSettingsLoadResult(settings, failures);
        }
        catch (JsonException ex)
        {
            return new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray.Create(
                new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, source, ex.Message)));
        }
    }

    private static HashSet<string> BuildKnownTopLevelKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SchemaKey };
        foreach (var property in typeof(ALCopsSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            keys.Add(property.Name);
        return keys;
    }

#if NETSTANDARD2_1
    private static List<string>? GetUnknownTopLevelKeys(string json)
    {
        // Newtonsoft tolerates comments and trailing commas by default, matching _jsonSettings.
        if (JToken.Parse(json) is not JObject root)
            return null;

        List<string>? unknown = null;
        foreach (var property in root.Properties())
        {
            if (!_knownTopLevelKeys.Contains(property.Name))
                (unknown ??= new List<string>()).Add(property.Name);
        }
        return unknown;
    }
#else
    private static List<string>? GetUnknownTopLevelKeys(string json)
    {
        using var document = JsonDocument.Parse(json, _documentOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return null;

        List<string>? unknown = null;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!_knownTopLevelKeys.Contains(property.Name))
                (unknown ??= new List<string>()).Add(property.Name);
        }
        return unknown;
    }
#endif

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
