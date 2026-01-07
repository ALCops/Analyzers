using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class PageRecordMethodRequiresSourceTable : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PageRecordMethodRequiresSourceTable>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PageRecordMethodRequiresSourceTable)));
        }

        [Test]
        [TestCase("PageGetRecord")]
        [TestCase("PageSetRecord")]
        [TestCase("PageSetSelectionFilter")]
        [TestCase("PageSetTableView")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExtensionDrillDownPageId", "TableExtensionLookupPageId"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PageRecordMethodRequiresSourceTable);
        }

        [Test]
        [TestCase("PageGetRecord")]
        [TestCase("PageSetRecord")]
        [TestCase("PageSetSelectionFilter")]
        [TestCase("PageSetTableView")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PageRecordMethodRequiresSourceTable);
        }
    }
}