using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class FilterStringSingleQuoteEscaping : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.FilterStringSingleQuoteEscaping _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.FilterStringSingleQuoteEscaping>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(FilterStringSingleQuoteEscaping)));
        }

        [Test]
        [TestCase("CalcFormulaFieldWhere")]
        [TestCase("CalcFormulaTableWhere")]
        [TestCase("SetFilter")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.FilterStringSingleQuoteEscaping);
        }

        [Test]
        [TestCase("CalcFormulaFieldWhere")]
        [TestCase("CalcFormulaTableWhere")]
        [TestCase("SetFilter")]
        [TestCase("SetFilterPlaceholder")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.FilterStringSingleQuoteEscaping);
        }

        [Test]
        [TestCase("CalcFormulaFieldWhere")]
        [TestCase("CalcFormulaTableWhere")]
        [TestCase("SetFilter")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<FilterStringSingleQuoteEscapingCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.FilterStringSingleQuoteEscaping);
        }
    }
}