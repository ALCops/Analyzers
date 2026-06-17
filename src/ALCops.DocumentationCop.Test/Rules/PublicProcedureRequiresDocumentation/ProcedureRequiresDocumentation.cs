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
            _fixture = RoslynFixtureFactory.Create<Analyzers.ProcedureRequiresDocumentation>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PublicProcedureRequiresDocumentation)));
        }

        [Test]
        [TestCase("Procedure")]
        [TestCase("ProcedureWithAttribute")]
        [TestCase("ProcedureWithComment")]
        public async Task PublicHasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicHasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PublicProcedureRequiresDocumentation);
        }

        [Test]
        [TestCase("CodeunitAccessInternal")]
        [TestCase("ProcedureDocumentationComment")]
        [TestCase("ProcedureDocumentationCommentWithAttribute")]
        [TestCase("ProcedureDocumentationCommentWithMultipleAttributes")]
        [TestCase("ProcedureInternal")]
        [TestCase("ProcedureLocal")]
        [TestCase("TestCodeunit")]
        [TestCase("TestCodeunitHandlerMethod")]
        public async Task PublicNoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicNoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PublicProcedureRequiresDocumentation);
        }
 
         [Test]
        [TestCase("CodeunitAccessInternal")]
		[TestCase("CodeunitAccessInternalProcedureWithAttribute")]
		[TestCase("CodeunitAccessInternalProcedureWithComment")]
		[TestCase("InternalProcedure")]
        [TestCase("InternalProcedureWithAttribute")]
        [TestCase("InternalProcedureWithComment")]
        public async Task InternalHasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalHasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureRequiresDocumentation);
        }

         [Test]
        [TestCase("CodeunitAccessInternalProcedureDocumentationComment")]
        [TestCase("CodeunitAccessInternalProcedureDocumentationCommentWithAttribute")]
        [TestCase("CodeunitAccessInternalProcedureDocumentationCommentWithMultipleAttributes")]
        [TestCase("Procedure")]
        [TestCase("ProcedureLocal")]
        [TestCase("TestCodeunit")]
        [TestCase("TestCodeunitHandlerMethod")]
        public async Task InternalNoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalNoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureRequiresDocumentation);
        }
  }
}
