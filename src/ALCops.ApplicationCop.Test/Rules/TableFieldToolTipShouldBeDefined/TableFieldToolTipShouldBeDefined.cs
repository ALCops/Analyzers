using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class TableFieldToolTipShouldBeDefined : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TableFieldToolTip>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                        Path.Combine("Rules", nameof(TableFieldToolTipShouldBeDefined)));
        }

        [Test]
        [TestCase("PageExtensionWithToolTip")]
        [TestCase("PageWithToolTip")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireMinimumVersion(
                "14.0",
                "No support for ToolTip property on table objects before AL version 14");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TableFieldToolTipShouldBeDefined);
        }

        [Test]
        [TestCase("PageExtensionWithToolTip")]
        [TestCase("PageWithToolTip")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireMinimumVersion(
                "14.0",
                "No support for ToolTip property on table objects before AL version 14");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TableFieldToolTipShouldBeDefined);
        }
    }
}