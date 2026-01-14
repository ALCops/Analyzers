using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class ApiPageCanonicalFieldNameGuide : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.ApiPageCanonicalFieldNameGuide _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ApiPageCanonicalFieldNameGuide>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ApiPageCanonicalFieldNameGuide)));
        }

        [Test]
        [TestCase("FieldWithNo")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ApiPageCanonicalFieldNameGuide);
        }

        [Test]
        [TestCase("FieldWithNo")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ApiPageCanonicalFieldNameGuide);
        }
    }
}