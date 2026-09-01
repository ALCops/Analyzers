using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
    public void GetSettings_ExtendsAbsoluteFileWithLocalOverrides()
    {
        var inheritedFile = Path.Combine(_tempRoot, "company.alcops.json");
        File.WriteAllText(
            inheritedFile,
            """
            {
              "CognitiveComplexityThreshold": 31,
              "CyclomaticComplexityThreshold": 12,
              "KnownAcronyms": ["Base"],
              "NamingPatterns": {
                "Procedure": {
                  "AllowPattern": "^Base",
                  "DisallowPattern": "Bad$",
                  "DisallowDescription": "comes from the base"
                },
                "Variable": {
                  "AllowPattern": "^V"
                }
              }
            }
            """);

        var appFolder = Path.Combine(_tempRoot, "ExtendedApp");
        Directory.CreateDirectory(appFolder);
        string serializedSource = JsonSerializer.Serialize(inheritedFile);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            $$"""
            {
              "Extends": {
                "Source": {{serializedSource}}
              },
              "KnownAcronyms": ["Local"],
              "CyclomaticComplexityThreshold": 17,
              "NamingPatterns": {
                "Procedure": {
                  "AllowPattern": "^Local",
                  "AllowDescription": "comes from the project"
                }
              }
            }
            """);

        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(settings.CognitiveComplexityThreshold, Is.EqualTo(31));
            Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(17));
            Assert.That(settings.KnownAcronyms, Is.EqualTo(new[] { "Local" }));
            Assert.That(settings.NamingPatterns, Contains.Key("Procedure"));
            Assert.That(settings.NamingPatterns, Contains.Key("Variable"));
            Assert.That(settings.NamingPatterns!["Procedure"].AllowPattern, Is.EqualTo("^Local"));
            Assert.That(settings.NamingPatterns["Procedure"].DisallowPattern, Is.EqualTo("Bad$"));
            Assert.That(settings.NamingPatterns["Procedure"].AllowDescription, Is.EqualTo("comes from the project"));
            Assert.That(settings.NamingPatterns["Procedure"].DisallowDescription, Is.EqualTo("comes from the base"));
        });
    }

    [Test]
    public void GetSettings_ExtendsFallsBackToLocalWhenSourceIsUnavailable()
    {
        var appFolder = Path.Combine(_tempRoot, "UnavailableSourceApp");
        Directory.CreateDirectory(appFolder);
        string serializedSource = JsonSerializer.Serialize(Path.Combine(_tempRoot, "missing.alcops.json"));
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            $$"""
            {
              "Extends": { "Source": {{serializedSource}} },
              "CyclomaticComplexityThreshold": 41
            }
            """);

        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(41));
            Assert.That(settings.CognitiveComplexityThreshold, Is.EqualTo(15));
        });
    }

    [Test]
    public void GetSettings_ExtendsFallsBackToLocalWhenInheritedSettingsAreInvalid()
    {
        var inheritedFile = Path.Combine(_tempRoot, "invalid.alcops.json");
        File.WriteAllText(inheritedFile, """{"StatementBlockSpacing":{"ScopeLeavingMode":"Invalid"}}""");

        var appFolder = Path.Combine(_tempRoot, "InvalidSourceApp");
        Directory.CreateDirectory(appFolder);
        string serializedSource = JsonSerializer.Serialize(inheritedFile);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            $$"""
            {
              "Extends": { "Source": {{serializedSource}} },
              "CyclomaticComplexityThreshold": 43
            }
            """);

        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(43));
            Assert.That(settings.StatementBlockSpacing.ScopeLeavingMode, Is.EqualTo(ScopeLeavingMode.ExitAndError));
        });
    }

    [Test]
    public void GetSettings_ExtendsFallsBackToLocalWhenInheritedJsonIsMalformed()
    {
        var inheritedFile = Path.Combine(_tempRoot, "malformed.alcops.json");
        File.WriteAllText(inheritedFile, "{ not valid JSON");

        var appFolder = Path.Combine(_tempRoot, "MalformedSourceApp");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            JsonSerializer.Serialize(new
            {
                Extends = new { Source = inheritedFile },
                MaintainabilityIndexThreshold = 44
            }));

        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(settings.MaintainabilityIndexThreshold, Is.EqualTo(44));
            Assert.That(settings.CognitiveComplexityThreshold, Is.EqualTo(15));
        });
    }

    [Test]
    public void GetSettings_ExtendsRejectsInheritanceChains()
    {
        var inheritedFile = Path.Combine(_tempRoot, "chained.alcops.json");
        File.WriteAllText(
            inheritedFile,
            """
            {
              "Extends": { "Source": "https://example.com/another.alcops.json" },
              "CognitiveComplexityThreshold": 99
            }
            """);

        var appFolder = Path.Combine(_tempRoot, "ChainedSourceApp");
        Directory.CreateDirectory(appFolder);
        string serializedSource = JsonSerializer.Serialize(inheritedFile);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            $$"""
            {
              "Extends": { "Source": {{serializedSource}} },
              "CyclomaticComplexityThreshold": 47
            }
            """);

        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(settings.CognitiveComplexityThreshold, Is.EqualTo(15));
            Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(47));
        });
    }

    [Test]
    public void GetSettings_ExtendsIsLoadedOncePerWorkspacePath()
    {
        var inheritedFile = Path.Combine(_tempRoot, "cached.alcops.json");
        File.WriteAllText(inheritedFile, """{"CognitiveComplexityThreshold":21}""");

        var appFolder = Path.Combine(_tempRoot, "CachedSourceApp");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            JsonSerializer.Serialize(new { Extends = new { Source = inheritedFile } }));

        var fileSystem = new RelativeFileSystem(appFolder);
        var firstSettings = ALCopsSettingsProvider.GetSettings(fileSystem);
        File.WriteAllText(inheritedFile, """{"CognitiveComplexityThreshold":22}""");
        var secondSettings = ALCopsSettingsProvider.GetSettings(fileSystem);

        Assert.Multiple(() =>
        {
            Assert.That(firstSettings.CognitiveComplexityThreshold, Is.EqualTo(21));
            Assert.That(secondSettings, Is.SameAs(firstSettings));
            Assert.That(secondSettings.CognitiveComplexityThreshold, Is.EqualTo(21));
        });
    }

    [Test]
    public async Task GetSettings_ExtendsAnonymousHttpSource()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        Task<string> requestTask = ServeSingleResponseAsync(
            listener,
            """{"MaintainabilityIndexThreshold":37}""");

        var appFolder = Path.Combine(_tempRoot, "HttpSourceApp");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            $$"""
            {
              "extends": { "source": "http://127.0.0.1:{{endpoint.Port}}/alcops.json" },
              "CyclomaticComplexityThreshold": 53
            }
            """);

        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));
        string requestHeaders = await requestTask.ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(settings.MaintainabilityIndexThreshold, Is.EqualTo(37));
            Assert.That(settings.CyclomaticComplexityThreshold, Is.EqualTo(53));
            Assert.That(requestHeaders, Does.Not.Contain("Authorization:"));
        });
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

    [Test]
    public void PunctuationSettings()
    {
        // Arrange: alcops.json in the app folder itself
        string appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);

        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            @"{
	""ToolTipAllowedPunctuations"": [
		{
			""Character"": ""."",
			""Name"": ""dot""
		},
		{
			""Character"": ""!"",
			""Name"": ""exclamation mark""
		}
	]
}");

        List<Punctuation>? expectedPunctuations = [
            new Punctuation { Character = ".", Name = "dot" },
            new Punctuation { Character = "!", Name = "exclamation mark" }
        ];

        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        Assert.That(settings.ToolTipAllowedPunctuations?.Count, Is.EqualTo(2));

        if (settings.ToolTipAllowedPunctuations != null)
        {
            foreach (Punctuation punctuation in settings.ToolTipAllowedPunctuations)
            {
                var expected = expectedPunctuations.FirstOrDefault(p => p.Character == punctuation.Character);

                Assert.That(punctuation.Character, Is.EqualTo(expected?.Character));
                Assert.That(punctuation.Name, Is.EqualTo(expected?.Name));
            }
        }
    }

    [Test]
    public void GetSettings_ParsesKnownAcronymsList()
    {
        // Arrange: alcops.json with a multi-entry KnownAcronyms list
        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            """{"KnownAcronyms": ["Acme", "FooBar", "XYZ"]}""");

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        // Assert
        Assert.That(settings.KnownAcronyms, Is.Not.Null);
        Assert.That(settings.KnownAcronyms, Is.EqualTo(new[] { "Acme", "FooBar", "XYZ" }));
    }

    [Test]
    public void GetSettings_KnownAcronyms_DefaultsToNullWhenAbsent()
    {
        // Arrange: alcops.json without a KnownAcronyms key
        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            """{"CyclomaticComplexityThreshold": 10}""");

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        // Assert
        Assert.That(settings.KnownAcronyms, Is.Null);
    }

    [Test]
    public void GetSettings_KnownAcronyms_AcceptsEmptyArray()
    {
        // Arrange: alcops.json with an explicit empty array
        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(
            Path.Combine(appFolder, "alcops.json"),
            """{"KnownAcronyms": []}""");

        // Act
        var settings = ALCopsSettingsProvider.GetSettings(new RelativeFileSystem(appFolder));

        // Assert
        Assert.That(settings.KnownAcronyms, Is.Not.Null);
        Assert.That(settings.KnownAcronyms, Is.Empty);
    }

    private static async Task<string> ServeSingleResponseAsync(TcpListener listener, string responseJson)
    {
        using TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
        await using NetworkStream stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
        var requestHeaders = new StringBuilder();

        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
            requestHeaders.AppendLine(line);

        byte[] responseBody = Encoding.UTF8.GetBytes(responseJson);
        byte[] responseHeaders = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {responseBody.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(responseHeaders).ConfigureAwait(false);
        await stream.WriteAsync(responseBody).ConfigureAwait(false);

        return requestHeaders.ToString();
    }
}
