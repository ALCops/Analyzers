using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class GlobalLanguageImplementTranslationHelper : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.BuiltInMethodImplementThroughCodeunit _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.BuiltInMethodImplementThroughCodeunit>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(GlobalLanguageImplementTranslationHelper)));
        }

        [Test]
        [TestCase("GlobalLanguage")]
        [TestCase("SystemGlobalLanguage")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.GlobalLanguageImplementTranslationHelper);
        }

        [Test]
        [TestCase("GlobalLanguageWithoutParameters")]
        [TestCase("ObsoletePending")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.GlobalLanguageImplementTranslationHelper);
        }

        [TestCase("GlobalLanguage")]
        [TestCase("GlobalLanguageAssignment")]
        [TestCase("GlobalLanguageCommentLeading")]
        [TestCase("GlobalLanguageCommentTrailing")]
        [TestCase("GlobalLanguageDefault")]
        [TestCase("SystemGlobalLanguage")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<GlobalLanguageImplementTranslationHelperCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.NotBlankRequiredOnPrimaryKeyField);
        }
    }
}