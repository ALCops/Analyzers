using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class ToolTipShouldStartWithSpecifies : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private AnalyzerTestFixture _analysisViewFixture;
        private string _testCasePath;

        private static readonly string[] AnalysisViewTestCases = ["PageAnalysisView"];

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>();
            _analysisViewFixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>(
                TestHelper.CreateAnalysisViewConfig());

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ToolTipShouldStartWithSpecifies)));
        }

        [Test]
        [TestCase("PageField")]
        [TestCase("TableField")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableField"],
                testCase,
                "13.0",
                "ToolTips on fields in a table object are not supported in versions lower than 13.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipShouldStartWithSpecifies);
        }

        [Test]
        [TestCase("PageAction")]
        [TestCase("PageAnalysisView")]
        [TestCase("PageField")]
        [TestCase("TableField")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableField"],
                testCase,
                "13.0",
                "ToolTips on fields in a table object are not supported in versions lower than 13.0."
            );
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipShouldStartWithSpecifies);
        }
    }
}