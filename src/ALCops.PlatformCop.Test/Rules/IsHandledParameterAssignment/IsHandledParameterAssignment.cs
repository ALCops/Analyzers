using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class IsHandledParameterAssignment : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.IsHandledParameterAssignment>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(IsHandledParameterAssignment)));
        }

        [Test]
        [TestCase("Assignment")]
        [TestCase("Invocation")]
        [TestCase("Handled")]
        [TestCase("PrecedingExitOnAssignment")]
        [TestCase("PrecedingExitOnInvocation")]
        [TestCase("SelfGuardedOrAssignment")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.IsHandledParameterAssignment);
        }

        [Test]
        [TestCase("Assignment")]
        [TestCase("Invocation")]
        [TestCase("NoEventSubscriberParameterReference")]
        [TestCase("PrecedingExitOnAssignment")]
        [TestCase("PrecedingExitOnInvocation")]
        [TestCase("SelfGuardedOrAssignment")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.IsHandledParameterAssignment);
        }
    }
}