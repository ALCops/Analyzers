using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class EmptyStatementRequiresComment : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.EmptyStatementRequiresComment>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(EmptyStatementRequiresComment)));
        }

        [Test]
        [TestCase("EmpyStatement")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EmptyStatementRequiresComment);
        }

        [Test]
        [TestCase("CaseStatement")]
        [TestCase("IfStatement")]
        [TestCase("NormalAssignment")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EmptyStatementRequiresComment);
        }
    }
}