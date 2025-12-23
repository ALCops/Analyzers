using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class MaintainabilityIndexMetric : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CyclomaticComplexityAndMaintainabilityIndex>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(MaintainabilityIndexMetric)));
        }

        [Test]
        [TestCase("MaintainabilityIndexThreshold")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                 .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.MaintainabilityIndexThresholdExceeded);
        }

        [Test]
        [TestCase("MaintainabilityIndexThreshold")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.MaintainabilityIndexThresholdExceeded);
        }
    }
}