using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class PageRecordArgumentMismatch : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PageRecordArgumentMismatch>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PageRecordArgumentMismatch)));
        }

        [Test]
        [TestCase("PageGetRecord")]
        [TestCase("PageSetRecord")]
        [TestCase("PageSetSelectionFilter")]
        [TestCase("PageSetTableView")]
        [TestCase("RunPage")]
        [TestCase("RunPageMethodAsTable")]
        [TestCase("RunPageModal")]
        [TestCase("TableDrillDownPageId")]
        [TestCase("TableExtensionDrillDownPageId")]
        [TestCase("TableExtensionLookupPageId")]
        [TestCase("TableLookupPageId")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExtensionDrillDownPageId", "TableExtensionLookupPageId"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PageRecordArgumentMismatch);
        }

        [Test]
        [TestCase("RunPageTargetPageHasNoRelatedTable")]
        [TestCase("RunPageWithMatchingSourceTable")]
        [TestCase("RunPageWithoutRecordArg")]
        [TestCase("TableDrillDownPageId")]
        [TestCase("TableExtensionDrillDownPageId")]
        [TestCase("TableExtensionLookupPageId")]
        [TestCase("TableLookupPageId")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PageRecordArgumentMismatch);
        }
    }
}