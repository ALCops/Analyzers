using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class WriteToFlowFieldRequiresComment : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.WriteToFlowFieldRequiresComment>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(WriteToFlowFieldRequiresComment)));
        }

        [Test]
        [TestCase("Assignment")]
        [TestCase("Validate")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.WriteToFlowFieldRequiresComment);
        }

        [Test]
        [TestCase("AssignmentWithLeadingComment")]
        [TestCase("AssignmentWithTrailingComment")]
        [TestCase("ValidateWithLeadingComment")]
        [TestCase("ValidateWithTrailingComment")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.WriteToFlowFieldRequiresComment);
        }
    }
}