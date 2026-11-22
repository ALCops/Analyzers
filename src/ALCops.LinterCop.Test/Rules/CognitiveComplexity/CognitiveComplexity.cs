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
                    "Rules", nameof(CognitiveComplexity));
        }

        [Test]
        [TestCase("ConditionalExpressionNested")] // ternary operator
        [TestCase("IfStatement")]
        [TestCase("IfStatementNested")]
        [TestCase("RecursionDirect")]
        [TestCase("RecursionIndirect")]
        [TestCase("RecursionDirectWithoutParentheses")]
        [TestCase("RecursionIndirectWithoutParentheses")]
        [TestCase("RecursionDirectPropertySyntax")]
        [TestCase("RecursionDirectPropertySyntaxThis")]
        [TestCase("RecursionIndirectPropertySyntax")]
        [TestCase("UserDefinedErrorMethodsAreNotGuardClauses")]
        [TestCase("InterfaceDefaultBody")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InterfaceDefaultBody"],
                testCase,
                "18.0",
                "Default interface method bodies need runtime 18.0; the harness compiles at the SDK's current release.");

            SkipTestIfVersionIsTooLow(
                ["ConditionalExpressionNested"],
                testCase,
                "14.0",
                "This test requires .NET 8 or higher due to the use of Conditional Expressions.");

            SkipTestIfVersionIsTooLow(
                ["RecursionDirect", "RecursionIndirect", "RecursionDirectWithoutParentheses", "RecursionIndirectWithoutParentheses", "RecursionDirectPropertySyntaxThis"],
                testCase,
                "14.0",
                "The this keyword is not supported in versions prior to 14.0.");

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
        [TestCase("BuiltInFieldErrorGuardClauses")]
        [TestCase("NonRecursivePropertySyntax")]
        [TestCase("InterfaceDefaultBody")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InterfaceDefaultBody"],
                testCase,
                "18.0",
                "Default interface method bodies need runtime 18.0; the harness compiles at the SDK's current release.");

            SkipTestIfVersionIsTooLow(
                ["IfStatementGuardClauseContinue"],
                testCase,
                "15.0",
                "The continue statement is not supported in versions prior to 15.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CognitiveComplexityThresholdExceeded);
        }
    }
}