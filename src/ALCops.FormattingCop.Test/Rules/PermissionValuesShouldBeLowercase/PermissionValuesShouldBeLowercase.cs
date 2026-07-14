using ALCops.FormattingCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class PermissionValuesShouldBeLowercase : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private AnalyzerTestFixture _errorTolerantFixture;
        private static readonly Analyzers.PermissionValuesShouldBeLowercase _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PermissionValuesShouldBeLowercase>();

            // The compiler rejects execute permission values (X/x) in the object-level
            // Permissions property (AL0195), so fixtures containing them cannot compile cleanly.
            _errorTolerantFixture = RoslynFixtureFactory.Create<Analyzers.PermissionValuesShouldBeLowercase>(
                new AnalyzerTestFixtureConfig
                {
                    ThrowsWhenInputDocumentContainsError = false
                });

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PermissionValuesShouldBeLowercase)));
        }

        [Test]
        [TestCase("CodeunitUppercase")]
        [TestCase("CodeunitMixedCase")]
        [TestCase("TableUppercase")]
        [TestCase("PageUppercase")]
        [TestCase("ReportUppercase")]
        [TestCase("XmlPortUppercase")]
        [TestCase("QueryUppercase")]
        [TestCase("RequestPageUppercase")]
        [TestCase("MultipleEntriesOneUppercase")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionValuesShouldBeLowercase);
        }

        [Test]
        [TestCase("ExecuteUppercase")]
        public async Task HasDiagnosticInDocumentWithErrors(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _errorTolerantFixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionValuesShouldBeLowercase);
        }

        [Test]
        [TestCase("LowercaseCodeunit")]
        [TestCase("PermissionSetUppercase")]
        [TestCase("InherentPermissionsUppercase")]
        [TestCase("NoPermissionsProperty")]
        [TestCase("ObsoleteCodeunit")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionValuesShouldBeLowercase);
        }

        [Test]
        [TestCase("PermissionSetExtensionUppercase")]
        public async Task NoDiagnosticOnPermissionSetExtension(string testCase)
        {
            RequireMinimumVersion(
                "13.0",
                "Older SDKs reject the permission set extension fixture with AL0334 (extension target already declared in this module)");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionValuesShouldBeLowercase);
        }

        [Test]
        [TestCase("LowercaseExecute")]
        public async Task NoDiagnosticInDocumentWithErrors(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _errorTolerantFixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionValuesShouldBeLowercase);
        }

        [Test]
        [TestCase("LowercaseAllValues")]
        [TestCase("MixedCaseValue")]
        [TestCase("MultipleEntries")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PermissionValuesShouldBeLowercaseCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PermissionValuesShouldBeLowercase);
        }

        [Test]
        [TestCase("ExecuteValue")]
        public async Task HasFixInDocumentWithErrors(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PermissionValuesShouldBeLowercaseCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer],
                    ThrowsWhenInputDocumentContainsError = false
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PermissionValuesShouldBeLowercase);
        }
    }
}
