using ALCops.FormattingCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class PermissionValuesShouldBeLowercase : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.PermissionValuesShouldBeLowercase _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PermissionValuesShouldBeLowercase>();

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
        [TestCase("LowercaseCodeunit")]
        [TestCase("PermissionSetUppercase")]
        [TestCase("PermissionSetExtensionUppercase")]
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
    }
}
