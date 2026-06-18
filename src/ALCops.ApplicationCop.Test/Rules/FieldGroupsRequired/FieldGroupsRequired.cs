using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class FieldGroupsRequired : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(FieldGroupsRequired)));

            _fixture = RoslynFixtureFactory.Create<Analyzers.FieldGroupsRequired>(
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(FieldGroupsRequired)}.ruleset.json")
                });
        }

        [Test]
        [TestCase("BrickIsMissing")]
        [TestCase("DropDownIsMissing")]
        [TestCase("TemporaryTable")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.FieldGroupsRequired);
        }

        [Test]
        [TestCase("HasBrickAndDropDown")]
        [TestCase("TemporaryTable")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.FieldGroupsRequired);
        }
    }
}