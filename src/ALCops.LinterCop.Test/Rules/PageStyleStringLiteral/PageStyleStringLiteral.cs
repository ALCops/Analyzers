using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class PageStyleStringLiteral : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PageStyleStringLiteral>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PageStyleStringLiteral)));
        }

        [Test]
        [TestCase("Label")]
        [TestCase("Page")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Label", "Page"],
                testCase,
                "14.0",
                "No support for PageStyle datatype in versions below 14.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PageStyleStringLiteral);
        }

        [Test]
        [TestCase("AssignToStyleExpr")]
        [TestCase("AssignToTableField")]
        [TestCase("Enum")]
        [TestCase("Label")]
        [TestCase("Page")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["AssignToStyleExpr", "AssignToTableField", "Enum", "Label", "Page"],
                testCase,
                "14.0",
                "No support for PageStyle datatype in versions below 14.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PageStyleStringLiteral);
        }
    }
}