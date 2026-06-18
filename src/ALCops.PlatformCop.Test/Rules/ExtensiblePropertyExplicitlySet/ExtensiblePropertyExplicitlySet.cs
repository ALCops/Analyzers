using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class ExtensiblePropertyExplicitlySet : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.ExtensiblePropertyExplicitlySet _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ExtensiblePropertyExplicitlySet)));

            _fixture = RoslynFixtureFactory.Create<Analyzers.ExtensiblePropertyExplicitlySet>(
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(ExtensiblePropertyExplicitlySet)}.ruleset.json")
                });
        }

        [Test]
        [TestCase("PageObject")]
        [TestCase("ReportObject")]
        [TestCase("TableObject")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ExtensiblePropertyExplicitlySet);
        }

        [Test]
        [TestCase("PageObject")]
        [TestCase("TableObject")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ExtensiblePropertyExplicitlySet);
        }

        [Test]
        [TestCase("PageObject")]
        [TestCase("ReportObject")]
        [TestCase("TableObject")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ExtensiblePropertyExplicitlySetCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer],
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(ExtensiblePropertyExplicitlySet)}.ruleset.json")
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.ExtensiblePropertyExplicitlySet);
        }
    }
}