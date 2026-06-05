using System.Collections.Concurrent;
using Microsoft.Dynamics.Nav.CodeAnalysis;
#if NETSTANDARD2_1
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif


namespace ALCops.Common.Settings;

/// <summary>
/// Provides cached access to ALCops settings.
/// Settings are loaded once per workspace path and cached for the analyzer session.
/// </summary>
public static class ALCopsSettingsProvider
{
    private static readonly ConcurrentDictionary<string, ALCopsSettings> _cache = new();
#if !NETSTANDARD2_1
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
#endif

    private const string SettingsFileName = "alcops.json";

    /// <summary>
    /// Gets the settings for the specified workspace path.
    /// Returns cached settings if already loaded, otherwise loads from file or returns defaults.
    /// </summary>
    /// <param name="workspacePath">
    /// The workspace directory path, typically from context.SemanticModel.Compilation.FileSystem?.GetDirectoryPath()
    /// </param>
    /// <returns>The settings instance (never null)</returns>
    public static ALCopsSettings GetSettings(string? workspacePath)
    {
        if (string.IsNullOrEmpty(workspacePath))
            return new ALCopsSettings();

        return _cache.GetOrAdd(workspacePath, LoadSettings);
    }

    /// <summary>
    /// Gets the settings from the compilation's file system.
    /// Not cached, since each compilation may have different files.
    /// Falls back to default settings when no alcops.json is found or fileSystem is null.
    /// </summary>
    public static ALCopsSettings GetSettings(IFileSystem? fileSystem)
    {
        if (fileSystem is null)
            return new ALCopsSettings();

        try
        {
            using Stream stream = fileSystem.OpenRead(SettingsFileName);
            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            return DeserializeSettings(json);
        }
        catch
        {
            return new ALCopsSettings();
        }
    }

    private static ALCopsSettings LoadSettings(string workspacePath)
    {
        var settingsFilePath = FindSettingsFile(workspacePath);

        if (settingsFilePath == null)
            return new ALCopsSettings();

        var json = File.ReadAllText(settingsFilePath);
        return DeserializeSettings(json);
    }

    private static ALCopsSettings DeserializeSettings(string json)
    {
#if NETSTANDARD2_1
        return JsonConvert.DeserializeObject<ALCopsSettings>(json) ?? new ALCopsSettings();
#else
        return JsonSerializer.Deserialize<ALCopsSettings>(json, _jsonOptions) ?? new ALCopsSettings();
#endif
    }

    private static string? FindSettingsFile(string workspacePath)
    {
        // First, try to find in workspace path
        var settingsFile = FindSettingsFileInDirectory(workspacePath);
        if (settingsFile != null)
            return settingsFile;

        // Second, look in the directory where assembly (ALCops.Common.dll) is located
        var assemblyLocation = Path.GetDirectoryName(typeof(ALCopsSettings).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyLocation) && !string.Equals(assemblyLocation, workspacePath, StringComparison.OrdinalIgnoreCase))
        {
            settingsFile = FindSettingsFileInDirectory(assemblyLocation);
            if (settingsFile != null)
                return settingsFile;
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

        return Directory.EnumerateFiles(directoryPath)
            .FirstOrDefault(f => string.Equals(
                Path.GetFileName(f), SettingsFileName, StringComparison.OrdinalIgnoreCase));
    }

}
