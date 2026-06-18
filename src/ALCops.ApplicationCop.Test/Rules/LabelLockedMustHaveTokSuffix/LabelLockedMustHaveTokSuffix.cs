using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class LabelLockedMustHaveTokSuffix : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(LabelLockedMustHaveTokSuffix)));

            _fixture = RoslynFixtureFactory.Create<Analyzers.LabelTokLockedConvention>(
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(LabelLockedMustHaveTokSuffix)}.ruleset.json")
                });
        }

        [Test]
        [TestCase("Label")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.LabelLockedMustHaveTokSuffix);
        }

        [Test]
        [TestCase("Label")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.LabelLockedMustHaveTokSuffix);
        }
    }
}