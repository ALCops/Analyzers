using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test;

internal static class TestHelper
{
    private static readonly byte[] AnalysisViewDefinitionContent = System.Text.Encoding.UTF8.GetBytes(
        """
        {
            "Id": "00000000-0000-0000-0000-000000000001",
            "Name": "MyAnalysisView",
            "TargetObjectId": 50100,
            "TargetObjectType": "Page"
        }
        """);

    internal static MemoryFileSystem CreateAnalysisViewFileSystem()
    {
        return new MemoryFileSystem(new Dictionary<string, byte[]>
        {
            { "MyAnalysisView.analysis.json", AnalysisViewDefinitionContent }
        });
    }

    internal static AnalyzerTestFixtureConfig CreateAnalysisViewConfig()
    {
        return new AnalyzerTestFixtureConfig
        {
            FileSystem = CreateAnalysisViewFileSystem(),
            ThrowsWhenInputDocumentContainsError = false
        };
    }

    internal static AnalyzerTestFixtureConfig CreateConfigWithSettings(string settingsJson)
    {
        return new AnalyzerTestFixtureConfig
        {
            FileSystem = new MemoryFileSystem(new Dictionary<string, byte[]>
            {
                { "alcops.json", System.Text.Encoding.UTF8.GetBytes(settingsJson) }
            }),
            ThrowsWhenInputDocumentContainsError = false
        };
    }
}
