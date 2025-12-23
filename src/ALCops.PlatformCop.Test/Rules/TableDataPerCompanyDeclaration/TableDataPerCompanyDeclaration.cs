using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TableDataPerCompanyDeclaration : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TableDataPerCompanyDeclaration>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                        Path.Combine("Rules", nameof(TableDataPerCompanyDeclaration)));
        }

        //TODO: Expose .WithRuleSetPath in RoslynTestKit, so we can enable/disable diagnostics in tests
        // [Test]
        // [TestCase("DataPerCompanyPropertyMissing")]
        // public async Task HasDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataPerCompanyDeclaration);
        // }

        // [Test]
        // [TestCase("DataPerCompanyFalse")]
        // [TestCase("DataPerCompanyTrue")]
        // public async Task NoDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataPerCompanyDeclaration);
        // }
    }
}