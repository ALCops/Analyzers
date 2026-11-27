using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test;

internal static class AppManifestRuleTestHelper
{
    public static string ReadFixture(string ruleName, string fixtureType)
    {
        string root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            root,
            "src",
            "ALCops.ApplicationCop.Test",
            "Rules",
            ruleName,
            fixtureType,
            "Manifest.al"));
    }

    public static string CreateManifest(
        string publisher = "Contoso Ltd.",
        string? contextSensitiveHelpUrl = null,
        Dictionary<string, bool>? resourceExposurePolicy = null,
        string[]? features = null,
        string[]? suppressWarnings = null,
        string version = "27.0.0.0",
        string application = "27.0.0.0")
    {
        var properties = new Dictionary<string, object?>
        {
            ["id"] = "00000000-0000-0000-0000-000000000001",
            ["name"] = "Manifest Rule Test",
            ["publisher"] = publisher,
            ["version"] = version,
            ["application"] = application
        };

        if (contextSensitiveHelpUrl is not null)
            properties["contextSensitiveHelpUrl"] = contextSensitiveHelpUrl;
        if (resourceExposurePolicy is not null)
            properties["resourceExposurePolicy"] = resourceExposurePolicy;
        if (features is not null)
            properties["features"] = features;
        if (suppressWarnings is not null)
            properties["suppressWarnings"] = suppressWarnings;

        return JsonSerializer.Serialize(properties);
    }

    public static Task<ImmutableArray<Diagnostic>> AnalyzeAsync<TAnalyzer>(
        string diagnosticId,
        string source,
        string appJson,
        string? alcopsJson = null,
        bool enableRule = true)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["app.json"] = Encoding.UTF8.GetBytes(appJson)
        };
        if (alcopsJson is not null)
            files["alcops.json"] = Encoding.UTF8.GetBytes(alcopsJson);

        var fileSystem = new MemoryFileSystem(files);
        var syntaxTree = SyntaxTree.ParseObjectText(source);
        Compilation compilation = Compilation.Create(
            "ManifestRuleTest",
            syntaxTrees: new[] { syntaxTree },
            fileSystem: fileSystem);

        if (enableRule)
        {
            compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(
                ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(diagnosticId, ReportDiagnostic.Info)));
        }

        var driver = new CompilationWithAnalyzers(
            compilation,
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()),
            options: null!,
            CancellationToken.None);

        return driver.GetAnalyzerDiagnosticsAsync();
    }

    public static void AssertNoAnalyzerException(ImmutableArray<Diagnostic> diagnostics)
    {
        Assert.That(
            diagnostics.Any(diagnostic => diagnostic.Id == "AD0001"),
            Is.False,
            "The analyzer must not throw during manifest analysis.");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ALCops.sln")))
                return current.FullName;

            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root (ALCops.sln).");
        return string.Empty;
    }
}
