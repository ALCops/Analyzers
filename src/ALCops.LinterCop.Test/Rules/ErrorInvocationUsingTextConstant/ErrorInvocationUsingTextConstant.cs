using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class ErrorInvocationUsingTextConstant : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ErrorInvocationUsingTextConstant>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ErrorInvocationUsingTextConstant)));
        }

        [Test]
        [TestCase("ErrorWithLiteralExpression")]
        [TestCase("ErrorWithStrSubstNo")]
        [TestCase("ErrorWithTextVariable")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                 .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ErrorInvocationUsingTextConstant);
        }

        [Test]
        [TestCase("ErrorWithErrorInfo")]
        [TestCase("ErrorWithLabel")]
        [TestCase("ErrorWiththisLabel")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ErrorWiththisLabel"],
                testCase,
                "14.0",
                "The this keyword is not supported in versions prior to 14.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ErrorInvocationUsingTextConstant);
        }
    }
}