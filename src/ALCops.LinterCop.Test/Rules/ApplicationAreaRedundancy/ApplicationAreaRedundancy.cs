using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class ApplicationAreaRedundancy : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.ApplicationAreaRedundancy _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ApplicationAreaRedundancy>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ApplicationAreaRedundancy)));
        }

        [Test]
        [TestCase("RedundantApplicationArea")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ApplicationAreaRedundancy);
        }

        [Test]
        [TestCase("NonRedundantApplicationArea")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ApplicationAreaRedundancy);
        }

        [Test]
        [TestCase("ApplicationArea")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ApplicationAreaRedundancyCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.ApplicationAreaRedundancy);
        }
    }
}