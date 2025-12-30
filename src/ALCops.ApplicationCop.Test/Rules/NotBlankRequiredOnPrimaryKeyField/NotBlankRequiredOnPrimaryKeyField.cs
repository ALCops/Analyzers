using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class NotBlankRequiredOnPrimaryKeyField : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.NotBlankOnPrimaryKeyField _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.NotBlankOnPrimaryKeyField>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(NotBlankRequiredOnPrimaryKeyField)));
        }

        [Test]
        [TestCase("PrimaryKeyCodeField")]
        [TestCase("PrimaryKeyCodeFieldWithoutExplicitKeysSet")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NotBlankRequiredOnPrimaryKeyField);
        }

        [Test]
        [TestCase("PrimaryKeyCodeFieldNotBlankFalse")]
        [TestCase("PrimaryKeyCodeFieldNotBlankTrue")]
        [TestCase("PrimaryKeyIntegerField")]
        [TestCase("PrimaryKeyMultipleFields")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NotBlankRequiredOnPrimaryKeyField);
        }

        [Test]
        [TestCase("PrimaryKeyCodeField")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<NotBlankRequiredOnPrimaryKeyFieldCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.NotBlankRequiredOnPrimaryKeyField);
        }
    }
}