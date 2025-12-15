using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class SemicolonAfterMethodOrTriggerDeclaration : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzer.SemicolonAfterMethodOrTriggerDeclaration>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(SemicolonAfterMethodOrTriggerDeclaration)));
        }

        [Test]
        [TestCase("ProcedureWithSemicolonAfterDeclaration")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.SemicolonAfterMethodOrTriggerDeclaration);
        }

        [Test]
        [TestCase("ObsoleteStatePending")]
        [TestCase("ProcedureWithoutBody")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.SemicolonAfterMethodOrTriggerDeclaration);
        }
    }
}