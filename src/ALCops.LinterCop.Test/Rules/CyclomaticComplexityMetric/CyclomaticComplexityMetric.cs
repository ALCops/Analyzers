using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class CyclomaticComplexityMetric : NavCodeAnalysisBase
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
                    "Rules", nameof(CyclomaticComplexityMetric));
        }

        [Test]
        [TestCase("CyclomaticComplexityThreshold")]
        [TestCase("InterfaceDefaultBody")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InterfaceDefaultBody"],
                testCase,
                "18.0",
                "Default interface method bodies need runtime 18.0; the harness compiles at the SDK's current release.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                 .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CyclomaticComplexityThresholdExceeded);
        }

        [Test]
        [TestCase("CyclomaticComplexityThreshold")]
        [TestCase("InterfaceDefaultBody")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InterfaceDefaultBody"],
                testCase,
                "18.0",
                "Default interface method bodies need runtime 18.0; the harness compiles at the SDK's current release.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CyclomaticComplexityThresholdExceeded);
        }
    }
}