using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class PragmaDisableRequiresComment : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PragmaDisableRequiresComment>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PragmaDisableRequiresComment)));
        }

        [Test]
        [TestCase("NoComment")]
        [TestCase("EmptyComment")]
        [TestCase("CommentNotOnLineAbove")]
        [TestCase("CommentAboveBelongsToCode")]
        [TestCase("StackedPragmas")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PragmaDisableRequiresComment);
        }

        [Test]
        [TestCase("TrailingComment")]
        [TestCase("CommentOnLineAbove")]
        [TestCase("RestorePragma")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PragmaDisableRequiresComment);
        }
    }
}
