using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.Common.Test;

/// <summary>
/// Tests for the CM0001 analyzer. Uses a manual Compilation + CompilationWithAnalyzers harness:
/// the diagnostic is reported at Location.None (alcops.json is not part of the compilation),
/// which RoslynTestKit's marker-based assertions cannot match. All file systems used here
/// return an empty directory path, bypassing the settings cache so tests stay isolated.
/// </summary>
public class ConfigurationCouldNotBeLoadedTests
{
    internal static ImmutableArray<Diagnostic> GetDiagnostics(IFileSystem fileSystem)
    {
        var tree = SyntaxTree.ParseObjectText("codeunit 50100 MyCodeunit { }");
        var compilation = Compilation.Create("Test", syntaxTrees: new[] { tree }, fileSystem: fileSystem);
        var withAnalyzers = new CompilationWithAnalyzers(
            compilation,
            ImmutableArray.Create<DiagnosticAnalyzer>(new Common.Analyzers.ConfigurationCouldNotBeLoaded()),
            options: null!,
            CancellationToken.None);
        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    private static MemoryFileSystem CreateFileSystem(string settingsJson) =>
        new(new Dictionary<string, byte[]>
        {
            { "alcops.json", System.Text.Encoding.UTF8.GetBytes(settingsJson) }
        });

    [Test]
    public void MalformedJson_ReportsSingleCm0001()
    {
        var diagnostics = GetDiagnostics(CreateFileSystem("{ this is not json"));

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.ConfigurationCouldNotBeLoaded));
        Assert.That(diagnostics[0].Location, Is.EqualTo(Location.None));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("alcops.json"));
    }

    [Test]
    public void UnknownKeys_ReportsOneCm0001PerKey()
    {
        var diagnostics = GetDiagnostics(CreateFileSystem("""{"Bogus": 1, "AlsoBogus": true}"""));

        Assert.That(diagnostics, Has.Length.EqualTo(2));
        Assert.That(diagnostics.Select(d => d.Id),
            Is.All.EqualTo(DiagnosticIds.ConfigurationCouldNotBeLoaded));
        Assert.That(diagnostics.Select(d => d.GetMessage()),
            Has.One.Contains("unknown setting 'Bogus'").And.One.Contains("unknown setting 'AlsoBogus'"));
    }

    [Test]
    public void ValidJson_NoDiagnostic()
    {
        var diagnostics = GetDiagnostics(CreateFileSystem("""{"CyclomaticComplexityThreshold": 42}"""));

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void NoAlcopsJson_NoDiagnostic()
    {
        var diagnostics = GetDiagnostics(new MemoryFileSystem(new Dictionary<string, byte[]>()));

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void SchemaKeyOnly_NoDiagnostic()
    {
        var diagnostics = GetDiagnostics(CreateFileSystem("""{"$schema": "https://example.invalid/alcops.schema.json"}"""));

        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("https://review-user:review-secret@example.invalid/alcops.json")]
    [TestCase("https://review-user:review%2Dsecret@example.invalid/alcops.json")]
    [TestCase("http://review-user:review-secret@example.invalid:8080/alcops.json")]
    public void CredentialBearingInheritedSource_ReportsCm0001WithoutCredentials(string source)
    {
        var diagnostics = GetDiagnostics(CreateFileSystem(
            System.Text.Json.JsonSerializer.Serialize(new { Extends = new { Source = source } })));

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.ConfigurationCouldNotBeLoaded));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("credentials"));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("example.invalid"));
        Assert.That(diagnostics[0].GetMessage(), Does.Not.Contain("review-user"));
        Assert.That(diagnostics[0].GetMessage(), Does.Not.Contain("review-secret"));
        Assert.That(diagnostics[0].GetMessage(), Does.Not.Contain("review%2Dsecret"));
    }

    [Test]
    public void UnreadableVirtualFile_ReportsCm0001()
    {
        var diagnostics = GetDiagnostics(new ThrowingFileSystem());

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.ConfigurationCouldNotBeLoaded));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("alcops.json"));
    }
}
