using ALCops.ApplicationCop.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis;
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

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.EmptyCaptionLocked>();
            _analysisViewFixture = RoslynFixtureFactory.Create<Analyzers.EmptyCaptionLocked>(
                TestHelper.CreateAnalysisViewConfig());

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(EmptyCaptionLocked)));
        }

        [Test]
        [TestCase("Enum")]
        [TestCase("LockedIsFalse")]
        [TestCase("Page")]
        [TestCase("PageAnalysisView")]
        [TestCase("PermissionSet")]
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

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EmptyCaptionLocked);
        }

        [Test]
        [TestCase("Enum")]
        [TestCase("PageAnalysisView")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EmptyCaptionLocked);
        }

        [Test]
        [TestCase("Enum")]
        public async Task HasFix(string testCase)
        {
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