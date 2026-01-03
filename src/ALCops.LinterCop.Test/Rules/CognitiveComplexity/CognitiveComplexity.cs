using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class CognitiveComplexity : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CognitiveComplexity>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(CognitiveComplexity)));
        }

        [Test]
        [TestCase("ConditionalExpressionNested")] // ternary operator
        [TestCase("IfStatement")]
        [TestCase("IfStatementNested")]
        [TestCase("RecursionDirect")]
        [TestCase("RecursionIndirect")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CognitiveComplexityThresholdExceeded);
        }

        [Test]
        [TestCase("CurrReportGuardClause")]
        [TestCase("CurrXMLportGuardClause")]
        [TestCase("IfStatement")]
        [TestCase("DiscountConsecutiveAndOperator")]
        [TestCase("IfStatementElseIf")]
        [TestCase("IfStatementGuardClause")]
        [TestCase("IfStatementGuardClauseContinue")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CognitiveComplexityThresholdExceeded);
        }
    }
}