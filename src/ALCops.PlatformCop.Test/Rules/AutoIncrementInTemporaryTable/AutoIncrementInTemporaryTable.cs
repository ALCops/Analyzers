using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class AutoIncrementInTemporaryTable : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.AutoIncrementInTemporaryTable>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(AutoIncrementInTemporaryTable)));
        }

        [Test]
        [TestCase("AutoIncrementFieldInTemporaryTable")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AutoIncrementInTemporaryTable);
        }

        [Test]
        [TestCase("AutoIncrementFieldInTable")]
        [TestCase("RegularFieldInTemporaryTable")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.AutoIncrementInTemporaryTable);
        }
    }
}