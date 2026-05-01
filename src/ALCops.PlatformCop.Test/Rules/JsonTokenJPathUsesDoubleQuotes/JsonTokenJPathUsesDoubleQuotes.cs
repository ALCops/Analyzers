using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class JsonTokenJPathUsesDoubleQuotes : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.JsonTokenJPathUsesDoubleQuotes _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.JsonTokenJPathUsesDoubleQuotes>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(JsonTokenJPathUsesDoubleQuotes)));
        }

        [Test]
        [TestCase("SelectToken")]
        [TestCase("SelectTokens")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["SelectTokens"],
                testCase,
                "17.0",
                "SelectTokens is available with runtime version 17.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.JsonTokenJPathUsesDoubleQuotes);
        }

        [Test]
        [TestCase("SelectToken")]
        [TestCase("SelectTokens")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["SelectTokens"],
                testCase,
                "17.0",
                "SelectTokens is available with runtime version 17.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.JsonTokenJPathUsesDoubleQuotes);
        }

        [Test]
        [TestCase("SelectToken")]
        [TestCase("SelectTokens")]
        public async Task HasFix(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["SelectTokens"],
                testCase,
                "17.0",
                "SelectTokens is available with runtime version 17.0."
            );

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<JsonTokenJPathUsesDoubleQuotesCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.EditableFlowField);
        }
    }
}