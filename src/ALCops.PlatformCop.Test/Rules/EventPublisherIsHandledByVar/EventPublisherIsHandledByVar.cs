using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class EventPublisherIsHandledByVar : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.EventPublisherIsHandledByVar _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.EventPublisherIsHandledByVar>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(EventPublisherIsHandledByVar)));
        }

        [Test]
        [TestCase("BusinessEvent")]
        [TestCase("IntegrationEvent")]
        [TestCase("InternalEvent")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventPublisherIsHandledByVar);
        }

        [Test]
        [TestCase("BusinessEvent")]
        [TestCase("IntegrationEvent")]
        [TestCase("InternalEvent")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventPublisherIsHandledByVar);
        }

        [Test]
        [TestCase("IntegrationEvent")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<EventPublisherIsHandledByVarCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.EditableFlowField);
        }
    }
}