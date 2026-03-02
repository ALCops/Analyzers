using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class AccessPropertyExplicitlySet : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.AccessPropertyExplicitlySet>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(AccessPropertyExplicitlySet)));
        }

        //TODO: Expose .WithRuleSetPath in RoslynTestKit, so we can enable/disable diagnostics in tests
        // [Test]
        // [TestCase("CodeunitObject")]
        // public async Task HasDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AccessPropertyExplicitlySet);
        // }

        // [Test]
        // [TestCase("CodeunitObject")]
        // [TestCase("TableFieldWithoutAccess")]
        // public async Task NoDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.AccessPropertyExplicitlySet);
        // }
    }
}