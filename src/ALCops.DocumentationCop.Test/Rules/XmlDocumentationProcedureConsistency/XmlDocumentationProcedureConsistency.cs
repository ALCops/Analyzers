using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class XmlDocumentationProcedureConsistency : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.XmlDocumentationProcedureConsistency>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(XmlDocumentationProcedureConsistency)));
        }

        [Test]
        [TestCase("Return")]
        [TestCase("Parameter")]
        [TestCase("DuplicateParameter")]
        [TestCase("DuplicateReturns")]
        [TestCase("TryFunction")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.XmlDocumentationProcedureConsistency);
        }

        [Test]
        [TestCase("Return")]
        [TestCase("Parameter")]
        [TestCase("NoDocumentationComment")]
        [TestCase("TryFunction")]
        [TestCase("TryFunctionEmptyReturns")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.XmlDocumentationProcedureConsistency);
        }
    }
}