using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class PublicProcedureRequiresDocumentation : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PublicProcedureRequiresDocumentation>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PublicProcedureRequiresDocumentation)));
        }

        [Test]
        [TestCase("Procedure")]
        [TestCase("ProcedureWithComment")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PublicProcedureRequiresDocumentation);
        }

        [Test]
        [TestCase("CodeunitAccessInternal")]
        [TestCase("ProcedureDocumentationComment")]
        [TestCase("ProcedureInternal")]
        [TestCase("ProcedureLocal")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PublicProcedureRequiresDocumentation);
        }
    }
}