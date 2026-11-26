using ALCops.FormattingCop.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class UseParenthesisForFunctionCall : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.UseParenthesisForFunctionCall _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UseParenthesisForFunctionCall>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(UseParenthesisForFunctionCall));
        }

        [Test]
        [TestCase("NoBrackets")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseParenthesisForFunctionCall);
        }

        [Test]
        [TestCase("Brackets")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                 .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseParenthesisForFunctionCall);
        }

        [Test]
        [TestCase("CurrentDateTime")]
        [TestCase("CompanyName")]
        [TestCase("TableCaption")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UseParenthesisForFunctionCallCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UseParenthesisForFunctionCall);
        }

        [Test]
        [TestCase("CustomerVendorRecordCalls")]
        public async Task HasFixAll(string testCase)
        {
            var (currentCode, expectedCode) = await ReadFixAllFixture(testCase).ConfigureAwait(false);

            CreateCodeFixFixture().TestFixAll(
                currentCode,
                expectedCode,
                DiagnosticIds.UseParenthesisForFunctionCall,
                equivalenceKey: nameof(UseParenthesisForFunctionCallCodeFix));
        }

        [Test]
        [TestCase("CustomerVendorAcrossDocuments")]
        public async Task HasFixAllInProject(string testCase)
        {
            var (currentCode, expectedCode) = await ReadFixAllFixture(testCase).ConfigureAwait(false);

            CreateCodeFixFixture().TestFixAll(
                currentCode,
                expectedCode,
                DiagnosticIds.UseParenthesisForFunctionCall,
                equivalenceKey: nameof(UseParenthesisForFunctionCallCodeFix),
                scope: FixAllScope.Project);
        }

        [Test]
        [TestCase("CustomerVendorAcrossDocuments")]
        public async Task HasFixAllInProjectOnReparsedText(string testCase)
        {
            var (currentCode, expectedCode) = await ReadFixAllFixture(testCase).ConfigureAwait(false);

            CreateCodeFixFixture().TestFixAll(
                currentCode,
                expectedCode,
                DiagnosticIds.UseParenthesisForFunctionCall,
                equivalenceKey: nameof(UseParenthesisForFunctionCallCodeFix),
                scope: FixAllScope.Project,
                hostVerification: FixAllHostVerification.TextEditsOnReparsedText);
        }

        private static CodeFixTestFixture CreateCodeFixFixture() =>
            RoslynFixtureFactory.Create<UseParenthesisForFunctionCallCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

        private async Task<(string CurrentCode, string ExpectedCode)> ReadFixAllFixture(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "expected.al"))
                .ConfigureAwait(false);

            return (currentCode, expectedCode);
        }
    }
}