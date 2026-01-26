using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class UseQueryOrFindWithNextInsteadOfCount : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.AnalyzeCountMethod>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UseQueryOrFindWithNextInsteadOfCount)));
        }

        [Test]
        [TestCase("RecordCountEqualsOne")]
        [TestCase("RecordCountGreaterThanOne")]
        [TestCase("RecordCountGreaterThanOrEqualOne")]
        [TestCase("RecordCountLessThanOrEqualZero")]
        [TestCase("RecordCountLessThanTwo")]
        [TestCase("RecordCountNotEqualsOne")]
        [TestCase("TwoGreaterThanRecordCount")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseQueryOrFindWithNextInsteadOfCount);
        }

        [Test]
        [TestCase("RecordCountEqualsTwo")]
        [TestCase("RecordTemporaryCountEqualsOne")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseQueryOrFindWithNextInsteadOfCount);
        }
    }
}