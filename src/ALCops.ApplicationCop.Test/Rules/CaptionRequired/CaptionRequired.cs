using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class CaptionRequired : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private AnalyzerTestFixture _analysisViewFixture;
        private string _testCasePath;

        private static readonly string[] AnalysisViewTestCases = ["PageAnalysisView"];

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CaptionRequired>();
            _analysisViewFixture = RoslynFixtureFactory.Create<Analyzers.CaptionRequired>(
                TestHelper.CreateAnalysisViewConfig());

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(CaptionRequired)));
        }

        [Test]
        [TestCase("EnumObject")]
        [TestCase("PageObject")]
        [TestCase("PageAnalysisView")]
        [TestCase("TableObject")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CaptionRequired);
        }

        [Test]
        [TestCase("ApiPage")]
        [TestCase("EnumObject")]
        [TestCase("PageObject")]
        [TestCase("PageAnalysisView")]
        [TestCase("TableObject")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CaptionRequired);
        }
    }
}