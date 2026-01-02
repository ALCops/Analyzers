using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class LineSeparatorShouldUseTypeHelper : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.LineSeparatorShouldUseTypeHelper>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(LineSeparatorShouldUseTypeHelper)));
        }

        [Test]
        [TestCase("LFSeparatorChar")]
        [TestCase("LFSeparatorCode")]
        [TestCase("LFSeparatorText")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.LineSeparatorShouldUseTypeHelper);
        }

        [Test]
        [TestCase("LFSeparatorCodeElementAccess3")]
        [TestCase("LFSeparatorTextElementAccess3")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.LineSeparatorShouldUseTypeHelper);
        }
    }
}