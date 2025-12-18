using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class SetRangeWithFilterOperators : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.SetRangeWithFilterOperators _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.SetRangeWithFilterOperators>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(SetRangeWithFilterOperators)));
        }

        [Test]
        [TestCase("SetRangeWithFilterOperator")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.SetRangeWithFilterOperators);
        }

        [Test]
        [TestCase("SetFilterWithFilterOperator")]
        [TestCase("SetRangeWithoutFilterOperator")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.SetRangeWithFilterOperators);
        }

        [Test]
        [TestCase("ReplaceSetRangeWithSetFilter")]
        [TestCase("ReplaceSetRangeWithSetFilterUsingRec")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<SetRangeWithFilterOperatorsCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.SetRangeWithFilterOperators);
        }
    }
}