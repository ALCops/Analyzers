using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class TableDataPerCompanyDeclaration : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                        Path.Combine("Rules", nameof(TableDataPerCompanyDeclaration)));

            _fixture = RoslynFixtureFactory.Create<Analyzers.TableDataPerCompanyDeclaration>(
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(TableDataPerCompanyDeclaration)}.ruleset.json")
                });
        }

        [Test]
        [TestCase("DataPerCompanyPropertyMissing")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataPerCompanyDeclaration);
        }

        [Test]
        [TestCase("DataPerCompanyFalse")]
        [TestCase("DataPerCompanyTrue")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataPerCompanyDeclaration);
        }
    }
}