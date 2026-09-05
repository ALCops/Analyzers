using System.Text.Json;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Test;

/// <summary>
/// Tests for the load-failure reporting of <see cref="ALCopsSettingsProvider.GetLoadResult"/>:
/// unreadable files, malformed JSON, and unknown top-level settings.
/// </summary>
[NonParallelizable]
public class ALCopsSettingsLoadFailureTests
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

    private string CreateAppFolder(string settingsJson)
    {
        var appFolder = Path.Combine(_tempRoot, "App1");
        Directory.CreateDirectory(appFolder);
        File.WriteAllText(Path.Combine(appFolder, "alcops.json"), settingsJson);
        return appFolder;
    }

    [Test]
    public void GetLoadResult_MalformedJson_ReturnsDefaultsWithInvalidFailure()
    {
        var appFolder = CreateAppFolder("{ this is not json");

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(8), "defaults expected");
        Assert.That(result.Failures, Has.Length.EqualTo(1));
        Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Invalid));
        Assert.That(result.Failures[0].Source, Does.Contain("alcops.json"));
    }

    [Test]
    public void GetLoadResult_UnknownTopLevelKey_ValidSettingsStillApply()
    {
        var appFolder = CreateAppFolder(
            """{"CyclomaticComplexityThreshold": 42, "CognitivComplexityThreshold": 20}""");

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(42));
        Assert.That(result.Failures, Has.Length.EqualTo(1));
        Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.UnknownSetting));
        Assert.That(result.Failures[0].Detail, Does.Contain("CognitivComplexityThreshold"));
    }

    [Test]
    public void GetLoadResult_MultipleUnknownKeys_OneFailurePerKey()
    {
        var appFolder = CreateAppFolder(
            """{"Bogus": 1, "AlsoBogus": true}""");

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Failures, Has.Length.EqualTo(2));
        Assert.That(result.Failures.Select(f => f.Kind),
            Is.All.EqualTo(SettingsLoadFailureKind.UnknownSetting));
        Assert.That(result.Failures.Select(f => f.Detail),
            Is.EquivalentTo(new[] { "unknown setting 'Bogus'", "unknown setting 'AlsoBogus'" }));
    }

    [Test]
    public void GetLoadResult_SchemaKey_NotReported()
    {
        var appFolder = CreateAppFolder(
            """{"$schema": "https://raw.githubusercontent.com/ALCops/Analyzers/main/src/ALCops.Common/Settings/alcops.schema.json", "CyclomaticComplexityThreshold": 42}""");

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(42));
        Assert.That(result.Failures, Is.Empty);
    }

    [Test]
    public void GetLoadResult_CaseInsensitiveKnownKey_AppliedAndNotReported()
    {
        // Both serializers match properties case-insensitively; the unknown-key check must too.
        var appFolder = CreateAppFolder("""{"cyclomaticcomplexitythreshold": 42}""");

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(42));
        Assert.That(result.Failures, Is.Empty);
    }

    [Test]
    public void GetLoadResult_JsonWithCommentsAndTrailingComma_NoFailures()
    {
        // The unknown-key scan must tolerate everything the deserializer tolerates.
        var appFolder = CreateAppFolder(
            """
            {
                // comment
                "CyclomaticComplexityThreshold": 42,
            }
            """);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(42));
        Assert.That(result.Failures, Is.Empty);
    }

    [Test]
    public void GetLoadResult_ValidInheritedSettings_AppliesWithoutFailures()
    {
        var inheritedFile = Path.Combine(_tempRoot, "company.alcops.json");
        File.WriteAllText(inheritedFile, """{"CognitiveComplexityThreshold": 31}""");
        var appFolder = CreateAppFolder(
            $$"""
            {
                "Extends": { "Source": {{JsonSerializer.Serialize(inheritedFile)}} },
                "CyclomaticComplexityThreshold": 17
            }
            """);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.CognitiveComplexityThreshold, Is.EqualTo(31));
            Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(17));
            Assert.That(result.Failures, Is.Empty);
        });
    }

    [Test]
    public void GetLoadResult_MissingInheritedFile_AppliesLocalSettingsWithUnreadableFailure()
    {
        var inheritedFile = Path.Combine(_tempRoot, "missing.alcops.json");
        var appFolder = CreateAppFolder(
            $$"""
            {
                "Extends": { "Source": {{JsonSerializer.Serialize(inheritedFile)}} },
                "CyclomaticComplexityThreshold": 41
            }
            """);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(41));
            Assert.That(result.Failures, Has.Length.EqualTo(1));
            Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Unreadable));
            Assert.That(result.Failures[0].Source, Is.EqualTo(inheritedFile));
        });
    }

    [Test]
    public void GetLoadResult_MalformedInheritedJson_AppliesLocalSettingsWithInvalidFailure()
    {
        var inheritedFile = Path.Combine(_tempRoot, "malformed.alcops.json");
        File.WriteAllText(inheritedFile, "{ not valid JSON");
        var appFolder = CreateAppFolder(
            $$"""
            {
                "Extends": { "Source": {{JsonSerializer.Serialize(inheritedFile)}} },
                "MaintainabilityIndexThreshold": 44
            }
            """);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.MaintainabilityIndexThreshold, Is.EqualTo(44));
            Assert.That(result.Failures, Has.Length.EqualTo(1));
            Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Invalid));
            Assert.That(result.Failures[0].Source, Is.EqualTo(inheritedFile));
        });
    }

    [Test]
    public void GetLoadResult_InheritanceChain_AppliesLocalSettingsWithInvalidFailure()
    {
        var inheritedFile = Path.Combine(_tempRoot, "chained.alcops.json");
        File.WriteAllText(
            inheritedFile,
            """{"Extends":{"Source":"https://example.invalid/base.json"},"CognitiveComplexityThreshold":99}""");
        var appFolder = CreateAppFolder(
            $$"""
            {
                "Extends": { "Source": {{JsonSerializer.Serialize(inheritedFile)}} },
                "CyclomaticComplexityThreshold": 47
            }
            """);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.CognitiveComplexityThreshold, Is.EqualTo(15));
            Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(47));
            Assert.That(result.Failures, Has.Length.EqualTo(1));
            Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Invalid));
            Assert.That(result.Failures[0].Source, Is.EqualTo(inheritedFile));
            Assert.That(result.Failures[0].Detail, Does.Contain("inheritance chains"));
        });
    }

    [Test]
    public void GetLoadResult_CredentialBearingHttpSource_AppliesLocalSettingsWithInvalidFailure()
    {
        const string inheritedUrl = "https://user:pass@example.invalid/alcops.json";
        var appFolder = CreateAppFolder(
            $$"""
            {
                "Extends": { "Source": "{{inheritedUrl}}" },
                "MaintainabilityIndexThreshold": 39
            }
            """);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.MaintainabilityIndexThreshold, Is.EqualTo(39));
            Assert.That(result.Failures, Has.Length.EqualTo(1));
            Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Invalid));
            Assert.That(result.Failures[0].Source, Is.EqualTo(inheritedUrl));
            Assert.That(result.Failures[0].Detail, Does.Contain("credentials"));
        });
    }

    [Test]
    public void GetLoadResult_UnknownInheritedKey_AppliesRecognizedSettingsWithFailure()
    {
        var inheritedFile = Path.Combine(_tempRoot, "unknown-setting.alcops.json");
        File.WriteAllText(
            inheritedFile,
            """{"CognitiveComplexityThreshold":31,"InheritedTypo":true}""");
        var appFolder = CreateAppFolder(
            $$"""
            {
                "Extends": { "Source": {{JsonSerializer.Serialize(inheritedFile)}} },
                "CyclomaticComplexityThreshold": 17
            }
            """);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.CognitiveComplexityThreshold, Is.EqualTo(31));
            Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(17));
            Assert.That(result.Failures, Has.Length.EqualTo(1));
            Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.UnknownSetting));
            Assert.That(result.Failures[0].Source, Is.EqualTo(inheritedFile));
            Assert.That(result.Failures[0].Detail, Does.Contain("InheritedTypo"));
        });
    }

    [Test]
    public void GetLoadResult_NoSettingsFile_NoFailures()
    {
        var appFolder = Path.Combine(_tempRoot, "EmptyApp");
        Directory.CreateDirectory(appFolder);

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Failures, Is.Empty);
    }

    [Test]
    public void GetLoadResult_EmptyMemoryFileSystem_NoFailures()
    {
        var fileSystem = new MemoryFileSystem(new Dictionary<string, byte[]>());

        var result = ALCopsSettingsProvider.GetLoadResult(fileSystem);

        Assert.That(result.Failures, Is.Empty);
    }

    [Test]
    public void GetLoadResult_UnknownEnumValue_InvalidFailure()
    {
        var appFolder = CreateAppFolder(
            """{"StatementBlockSpacing": {"ScopeLeavingMode": "Bogus"}}""");

        var result = ALCopsSettingsProvider.GetLoadResult(new RelativeFileSystem(appFolder));

        Assert.That(result.Settings.StatementBlockSpacing.ScopeLeavingMode,
            Is.EqualTo(ScopeLeavingMode.ExitAndError), "defaults expected");
        Assert.That(result.Failures, Has.Length.EqualTo(1));
        Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Invalid));
    }

    [Test]
    public void GetLoadResult_VirtualFileUnreadable_ReturnsDefaultsWithUnreadableFailure()
    {
        var fileSystem = new ThrowingFileSystem();

        var result = ALCopsSettingsProvider.GetLoadResult(fileSystem);

        Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(8), "defaults expected");
        Assert.That(result.Failures, Has.Length.EqualTo(1));
        Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Unreadable));
        Assert.That(result.Failures[0].Source, Does.Contain("alcops.json"));
    }

    [Test]
    public void GetLoadResult_FailuresAreCachedAndRereported()
    {
        // The cache keeps failures so every compilation re-reports them via CM0001.
        var appFolder = CreateAppFolder("{ this is not json");
        var fileSystem = new RelativeFileSystem(appFolder);

        var first = ALCopsSettingsProvider.GetLoadResult(fileSystem);
        var second = ALCopsSettingsProvider.GetLoadResult(fileSystem);

        Assert.That(first.Failures, Has.Length.EqualTo(1));
        Assert.That(second.Failures, Has.Length.EqualTo(1));
    }

    [Test]
    public void GetLoadResult_MalformedJsonInMemoryFileSystem_InvalidFailure()
    {
        var files = new Dictionary<string, byte[]>
        {
            { "alcops.json", "{ this is not json"u8.ToArray() }
        };
        var fileSystem = new MemoryFileSystem(files);

        var result = ALCopsSettingsProvider.GetLoadResult(fileSystem);

        Assert.That(result.Failures, Has.Length.EqualTo(1));
        Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Invalid));
    }
}
