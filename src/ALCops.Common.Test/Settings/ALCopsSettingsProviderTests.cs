using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Test;

/// <summary>
/// Tests for the ALCopsSettingsProvider parent directory traversal behavior.
/// Verifies that alcops.json is found when placed in parent directories.
/// </summary>
[NonParallelizable]
public class ALCopsSettingsProviderTests
{
    private string _tempRoot = null!;

    [SetUp]
    public void Setup()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"alcops_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    [Test]
    public void GetSettings_FindsSettingsInCurrentDirectory()
    {
        // Arrange: alcops.json in the app folder itself
        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            """{"CyclomaticComplexityThreshold": 42}""");

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        // Assert
        Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(42));
    }

    [Test]
    public void GetSettings_FindsSettingsInParentDirectory()
    {
        // Arrange: alcops.json at workspace root, app folder is one level deeper
        File.WriteAllText(
            Path.Combine(_tempRoot, "alcops.json"),
            """{"CyclomaticComplexityThreshold": 99}""");

        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        // Assert
        Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(99));
    }

    [Test]
    public void GetSettings_FindsSettingsInGrandparentDirectory()
    {
        // Arrange: alcops.json two levels above the app folder
        File.WriteAllText(
            Path.Combine(_tempRoot, "alcops.json"),
            """{"CognitiveComplexityThreshold": 50}""");

        var nestedApp = Path.Combine(_tempRoot, "src", "apps", "App1");
        Directory.CreateDirectory(nestedApp);

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(nestedApp));

        // Assert
        Assert.That(settings.CognitiveComplexityThreshold, Is.EqualTo(50));
    }

    [Test]
    public void GetSettings_ClosestSettingsWins()
    {
        // Arrange: alcops.json at both workspace root and app folder level
        File.WriteAllText(
            Path.Combine(_tempRoot, "alcops.json"),
            """{"CyclomaticComplexityThreshold": 100}""");

        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            """{"CyclomaticComplexityThreshold": 5}""");

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        // Assert: app-level setting wins over parent
        Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(5));
    }

    [Test]
    public void GetSettings_ReturnsDefaultsWhenNoSettingsFileExists()
    {
        // Arrange: empty directory hierarchy with no alcops.json
        var appFolder = Path.Combine(_tempRoot, "EmptyApp");
        Directory.CreateDirectory(appFolder);

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        // Assert: defaults are used
        Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(8));
        Assert.That(settings.CognitiveComplexityThreshold, Is.EqualTo(15));
        Assert.That(settings.MaintainabilityIndexThreshold, Is.EqualTo(20));
    }

    [Test]
    public void GetSettings_WithIFileSystem_FallsBackToParentTraversal()
    {
        // Arrange: alcops.json in parent, not in the app folder
        File.WriteAllText(
            Path.Combine(_tempRoot, "alcops.json"),
            """{"MaintainabilityIndexThreshold": 30}""");

        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);

        var fileSystem = new RelativeFileSystem(appFolder);

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(fileSystem);

        // Assert
        Assert.That(settings.MaintainabilityIndexThreshold, Is.EqualTo(30));
    }

    [Test]
    public void GetSettings_WithIFileSystem_PrefersAppFolderOverParent()
    {
        // Arrange: alcops.json in both parent and app folder
        File.WriteAllText(
            Path.Combine(_tempRoot, "alcops.json"),
            """{"CyclomaticComplexityThreshold": 100}""");

        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            """{"CyclomaticComplexityThreshold": 7}""");

        var fileSystem = new RelativeFileSystem(appFolder);

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(fileSystem);

        // Assert: app-level wins
        Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(7));
    }

    [Test]
    public void GetSettings_WithMemoryFileSystem_UsesVirtualSettings()
    {
        // Arrange: MemoryFileSystem with alcops.json (simulates test environment)
        var settingsJson = """{"CyclomaticComplexityThreshold": 55}"""u8.ToArray();
        var files = new Dictionary<string, byte[]>
        {
            { "alcops.json", settingsJson }
        };
        var fileSystem = new MemoryFileSystem(files);

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(fileSystem);

        // Assert
        Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(55));
    }

    [Test]
    public void GetSettings_WithMemoryFileSystem_ReturnsDefaultsWhenNoConfig()
    {
        // Arrange: MemoryFileSystem without alcops.json
        var fileSystem = new MemoryFileSystem(new Dictionary<string, byte[]>());

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(fileSystem);

        // Assert: defaults
        Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(8));
    }
}
