using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class EmptyCaptionLocked : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private AnalyzerTestFixture _analysisViewFixture;
        private static readonly Analyzers.PermissionSetCaptionLength _analyzer = new();
        private string _testCasePath;

        private static readonly string[] AnalysisViewTestCases = ["PageAnalysisView"];
        private static readonly string[] ProfileExtensionTestCases = ["ProfileExtension"];

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.EmptyCaptionLocked>();
            _analysisViewFixture = RoslynFixtureFactory.Create<Analyzers.EmptyCaptionLocked>(
                TestHelper.CreateAnalysisViewConfig());

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(EmptyCaptionLocked));
        }

        [Test]
        [TestCase("Enum")]
        [TestCase("LockedIsFalse")]
        [TestCase("Page")]
        [TestCase("PageAnalysisView")]
        [TestCase("PermissionSet")]
        [TestCase("ProfileExtension")]
        [TestCase("Query")]
        [TestCase("Report")]
        [TestCase("Table")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            SkipTestIfVersionIsTooLow(
                ProfileExtensionTestCases,
                testCase,
                "13.0",
                "No support for extension objects when the target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ProfileExtensionTestCases,
                testCase,
                "14.0",
                "Profile extensions are not supported before AL 14.0");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EmptyCaptionLocked);
        }

        [Test]
        [TestCase("Enum")]
        [TestCase("PageAnalysisView")]
        [TestCase("ProfileExtension")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            SkipTestIfVersionIsTooLow(
                ProfileExtensionTestCases,
                testCase,
                "13.0",
                "No support for extension objects when the target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ProfileExtensionTestCases,
                testCase,
                "14.0",
                "Profile extensions are not supported before AL 14.0");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EmptyCaptionLocked);
        }

        [Test]
        [TestCase("Enum")]
        [TestCase("ProfileExtension")]
        public async Task HasFix(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ProfileExtensionTestCases,
                testCase,
                "13.0",
                "No support for extension objects when the target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ProfileExtensionTestCases,
                testCase,
                "14.0",
                "Profile extensions are not supported before AL 14.0");

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<EmptyCaptionLockedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.EmptyCaptionLocked);
        }
    }
}