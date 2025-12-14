using ALCops.ApplicationCop.Analyzer;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class LookupPageIdAndDrillDownPageId : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<LookupPageIdAndDrillDownPageIdAnalyzer>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(LookupPageIdAndDrillDownPageId)));
        }

        [Test]
        [TestCase("NoDrillDownPageId")]
        [TestCase("NoLookupPageId")]
        [TestCase("NoLookupPageIdAndNoDrillDownPageId")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.LookupPageIdAndDrillDownPageId);
        }

        [Test]
        [TestCase("LookupPageIdAndDrillDownPageId")]
        [TestCase("SourceTableTemporary")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.LookupPageIdAndDrillDownPageId);
        }
    }
}