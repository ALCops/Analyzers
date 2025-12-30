using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class UseSecretTextForSensitiveText : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UseSecretTextForSensitiveText>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UseSecretTextForSensitiveText)));
        }

        [Test]
        [TestCase("IsolatedStorage")]
        [TestCase("HttpHeaders")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["IsolatedStorage"],
                testCase,
                "13.0",
                "No support for SecretText in IsolatedStorage in versions prior to 13.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseSecretTextForSensitiveText);
        }

        [Test]
        [TestCase("IsolatedStorage")]
        [TestCase("HttpHeaders")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["IsolatedStorage"],
                testCase,
                "13.0",
                "No support for SecretText in IsolatedStorage in versions prior to 13.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseSecretTextForSensitiveText);
        }
    }
}