using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class PossibleOverflowAssigning : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.PossibleOverflowAssigning _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PossibleOverflowAssigning>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PossibleOverflowAssigning)));
        }

        [Test]
        [TestCase("AssignLabel")]
        [TestCase("ExitStatementLabel")]
        [TestCase("GetMethodStringLiteral")]
        [TestCase("GetMethodStrSubstNo")]
        [TestCase("GetMethodXmlPortTextElement")]
        [TestCase("SetFilterFieldCode")]
        [TestCase("SetFilterFieldCodeXmlPortTextElement")]
        [TestCase("ValidateFieldCode")]
        [TestCase("ValidateFieldCodeXmlPortTextElement")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PossibleOverflowAssigning);
        }

        [Test]
        [TestCase("AssignLabel")]
        [TestCase("ExitStatementLabel")]
        [TestCase("ExitStatementLabelWithLocked")]
        [TestCase("ExitStatementLabelWithMaxLength")]
        [TestCase("GetMethodCompanyName")]
        [TestCase("GetMethodStringLiteral")]
        [TestCase("GetMethodStrSubstNo")]
        [TestCase("GetMethodUserId")]
        [TestCase("SetFilterFieldRef")]
        [TestCase("ValidateFieldCode")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PossibleOverflowAssigning);
        }

        [Test]
        [TestCase("AssignmentStatement")]
        [TestCase("ExitStatement")]
        [TestCase("ValidateStatement")]
        public async Task HasFixCopyStr(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixCopyStr), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixCopyStr), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PossibleOverflowAssigningApplyCopyStrCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PossibleOverflowAssigning);
        }

        [Test]
        [TestCase("AssignmentStatement")]
        [TestCase("ExitStatement")]
        [TestCase("ExitStatementWithComment")]
        [TestCase("ValidateStatement")]
        public async Task HasFixMaxLength(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixMaxLength), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixMaxLength), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PossibleOverflowAssigningAppendMaxLengthToLabelCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PossibleOverflowAssigning);
        }
    }
}