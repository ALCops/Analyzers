using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class ReportLayoutPropertyLength : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ReportLayoutPropertyLength>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ReportLayoutPropertyLength)));
        }

        [Test]
        [TestCase("CaptionExceeds250")]
        [TestCase("SummaryExceeds250")]
        [TestCase("ReportExtensionCaptionExceeds250")]
        [TestCase("BothExceed250")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ReportExtensionCaptionExceeds250"],
                 testCase,
                 "13.0", "The extension object 'MyReportExt' cannot be declared. Another extension for target '' or the target itself is already declared in this module.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ReportLayoutPropertyLength);
        }

        [Test]
        [TestCase("CaptionExactly250")]
        [TestCase("CaptionUnder250")]
        [TestCase("SummaryUnder250")]
        [TestCase("NoCaptionOrSummary")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ReportLayoutPropertyLength);
        }
    }
}
