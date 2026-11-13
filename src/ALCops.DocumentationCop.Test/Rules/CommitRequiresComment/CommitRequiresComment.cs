using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class CommitRequiresComment : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CommitRequiresComment>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(CommitRequiresComment));
        }

        [Test]
        [TestCase("Commit")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CommitRequiresComment);
        }

        [Test]
        [TestCase("CommitLeading")]
        [TestCase("CommitLeadingWithSpace")]
        [TestCase("CommitTrailing")]
        [TestCase("ObsoleteMethod")]
        [TestCase("ObsoleteObject")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CommitRequiresComment);
        }
    }
}