using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class UseIsEmptyMethodInsteadOfCount : NavCodeAnalysisBase
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
                    Path.Combine("Rules", nameof(UseIsEmptyMethodInsteadOfCount)));
        }

        [Test]
        [TestCase("OneGreaterThanRecordCount")]
        [TestCase("RecordCountEqualsZero")]
        [TestCase("RecordCountGreaterThanOrEqualZero")]
        [TestCase("RecordCountGreaterThanZero")]
        [TestCase("RecordCountLessThanOne")]
        [TestCase("RecordCountLessThanOrEqualZero")]
        [TestCase("RecordCountLessThanZero")]
        [TestCase("RecordCountNotEqualsZero")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseIsEmptyMethodInsteadOfCount);
        }

        [Test]
        [TestCase("RecordCountEqualsOne")]
        [TestCase("RecordTemporaryCountEqualsZero")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseIsEmptyMethodInsteadOfCount);
        }
    }
}