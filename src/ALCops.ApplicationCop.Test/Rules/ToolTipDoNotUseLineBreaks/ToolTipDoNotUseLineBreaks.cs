using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class ToolTipDoNotUseLineBreaks : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ToolTipDoNotUseLineBreaks)));
        }

        [Test]
        [TestCase("PageAction")]
        [TestCase("PageField")]
        [TestCase("TableField")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableField"],
                testCase,
                "13.0",
                "ToolTips on fields in a table object are not supported in versions lower than 13.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipDoNotUseLineBreaks);
        }

        [Test]
        [TestCase("PageAction")]
        [TestCase("PageField")]
        [TestCase("TableField")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableField"],
                testCase,
                "13.0",
                "ToolTips on fields in a table object are not supported in versions lower than 13.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipDoNotUseLineBreaks);
        }
    }
}