using ALCops.FormattingCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class CasingMismatchKeyword : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.CasingMismatchKeyword _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CasingMismatchKeyword>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(CasingMismatchKeyword));
        }

        [Test]
        [TestCase("Codeunit")]
        [TestCase("Enum")]
        [TestCase("PageCustomization")]
        [TestCase("Table")]
        [TestCase("XmlPort")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Codeunit"],
                testCase,
                "13.0"
            );

            SkipTestIfVersionIsTooLow(
                ["PageCustomization"],
                testCase,
                "13.0",
                "No support for pagecustomizations when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CasingMismatch);
        }

        [Test]
        [TestCase("Codeunit")]
        [TestCase("Enum")]
        [TestCase("PageCustomization")]
        [TestCase("Table")]
        [TestCase("VariableNamedAfterKeyword")]
        [TestCase("XmlPort")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Codeunit"],
                testCase,
                "13.0"
            );

            SkipTestIfVersionIsTooLow(
                ["PageCustomization"],
                testCase,
                "13.0",
                "No support for pagecustomizations when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CasingMismatch);
        }

        [Test]
        [TestCase("ObjectKeyword")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<CasingMismatchCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.CasingMismatch);
        }
    }
}