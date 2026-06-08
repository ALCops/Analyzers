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
    /// Gets the settings from the compilation's file system.
    /// First checks the app folder via the virtual file system, then walks up parent directories
    /// on the physical file system, and finally falls back to the assembly location.
    /// Results are cached per directory path.
    /// </summary>
    public static ALCopsSettings GetSettings(IFileSystem? fileSystem)
    {
        if (fileSystem is null)
            return new ALCopsSettings();

        string directoryPath = fileSystem.GetDirectoryPath();

        if (string.IsNullOrEmpty(directoryPath))
            return LoadSettingsFromFileSystem(fileSystem, directoryPath);

        return _cache.GetOrAdd(directoryPath, _ => LoadSettingsFromFileSystem(fileSystem, directoryPath));
    }

    private static ALCopsSettings LoadSettingsFromFileSystem(IFileSystem fileSystem, string directoryPath)
    {
        var json = TryReadFromVirtualFileSystem(fileSystem);
        if (json != null)
            return DeserializeSettings(json);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            var settingsFilePath = FindSettingsFileInParentOrAssemblyDirectory(directoryPath);
            if (settingsFilePath != null)
                return DeserializeSettings(File.ReadAllText(settingsFilePath));
        }

        return new ALCopsSettings();
    }

    private static string? TryReadFromVirtualFileSystem(IFileSystem fileSystem)
    {
        try
        {
            using Stream stream = fileSystem.OpenRead(SettingsFileName);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            // IFileSystem.OpenRead has no defined exception contract —
            // implementations throw IOException, KeyNotFoundException, or other types
            return null;
        }
    }

    private static ALCopsSettings DeserializeSettings(string json)
    {
#if NETSTANDARD2_1
        return JsonConvert.DeserializeObject<ALCopsSettings>(json) ?? new ALCopsSettings();
#else
        return JsonSerializer.Deserialize<ALCopsSettings>(json, _jsonOptions) ?? new ALCopsSettings();
#endif
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