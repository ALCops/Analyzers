using ALCops.FormattingCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class PermissionDeclarationOrder : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.PermissionDeclarationOrder _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PermissionDeclarationOrder>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PermissionDeclarationOrder)));
        }

        [Test]
        [TestCase("UnsortedTabledata")]
        [TestCase("UnsortedMixedTypes")]
        [TestCase("UnsortedSingleType")]
        [TestCase("UnsortedCodeunit")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionDeclarationOrder);
        }

        [Test]
        [TestCase("AlreadySorted")]
        [TestCase("SingleEntry")]
        [TestCase("NoPermissionsProperty")]
        [TestCase("SortedTabledata")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PermissionDeclarationOrder);
        }

        [Test]
        [TestCase("ReorderTabledata")]
        [TestCase("ReorderMixedTypes")]
        [TestCase("SingleLineToMultiLine")]
        [TestCase("PreserveCasing")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PermissionDeclarationOrderCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PermissionDeclarationOrder);
        }
    }
}
