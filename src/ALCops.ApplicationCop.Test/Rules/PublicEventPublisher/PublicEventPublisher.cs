using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class PublicEventPublisher : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.PermissionSetCaptionLength _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PublicEventPublisher>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PublicEventPublisher)));
        }

        [Test]
        [TestCase("PublicEvent")]
        [TestCase("PublicExternalBusinessEvent")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["PublicExternalBusinessEvent"],
                testCase,
                "13.0",
                "No support for External Business Events before version 13.0");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PublicEventPublisher);
        }

        [Test]
        [TestCase("InternalEvent")]
        [TestCase("InternalExternalBusinessEvent")]
        [TestCase("LocalEvent")]
        [TestCase("LocalExternalBusinessEvent")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InternalExternalBusinessEvent", "LocalExternalBusinessEvent"],
                testCase,
                "13.0",
                "No support for External Business Events before version 13.0");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PublicEventPublisher);
        }

        [Test]
        [TestCase("BusinessEvent")]
        [TestCase("ExternalBusinessEvent")]
        [TestCase("IntegrationEvent")]
        [TestCase("InternalEvent")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PublicEventPublisherCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PublicEventPublisher);
        }
    }
}