using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class EnumValueHasEmptyCaption : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.EnumAccessibility>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(EnumValueHasEmptyCaption)));
        }

        [Test]
        [TestCase("EnumWithEmptyCaption")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EnumValueHasEmptyCaption);
        }

        [Test]
        [TestCase("EnumWithEmptyCaptionLocked")]
        [TestCase("EnumWithNonEmptyCaption")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EnumValueHasEmptyCaption);
        }
    }
}