using RoslynTestKit;

namespace ALCops.TestAutomationCop.Test
{

    public class MethodRequiresTestAttribute : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzer.GlobalMethodRequiresTestAttributeAnalyzer _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzer.GlobalMethodRequiresTestAttributeAnalyzer>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(MethodRequiresTestAttribute)));
        }

        [Test]
        [TestCase("GlobalTestMethodWithoutTestAttribute")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.GlobalMethodRequiresTestAttribute);
        }

        [Test]
        [TestCase("StandardCodeunit")]
        [TestCase("ConfirmHandler")]
        [TestCase("GlobalTestProcedure")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.GlobalMethodRequiresTestAttribute);
        }
    }
}