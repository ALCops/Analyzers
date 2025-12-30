using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class PermissionSetCaptionLength : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.PermissionSetCaptionLength _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PermissionSetCaptionLength>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PermissionSetCaptionLength)));
        }

        [Test]
        [TestCase("Caption")]
        [TestCase("CaptionLocked")]
        [TestCase("CaptionLong")]
        [TestCase("CaptionMaxLength")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionSetCaptionLength);
        }

        [Test]
        [TestCase("CaptionLocked")]
        [TestCase("CaptionMaxLength")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionSetCaptionLength);
        }

        [Test]
        [TestCase("Caption")]
        [TestCase("CaptionLocked")]
        [TestCase("CaptionMaxLength")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PermissionSetCaptionLengthCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PermissionSetCaptionLength);
        }
    }
}