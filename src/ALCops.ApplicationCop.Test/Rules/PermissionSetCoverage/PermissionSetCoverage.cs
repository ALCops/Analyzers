using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class PermissionSetCoverage : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PermissionSetCoverage>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PermissionSetCoverage)));
        }

        //TODO: Unknown issue with test case
        // [Test]
        // [TestCase("Codeunit")]
        // public async Task HasDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionSetCoverage);
        // }

        [Test]
        [TestCase("Codeunit")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionSetCoverage);
        }
    }
}