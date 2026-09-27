using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class TelemetryRequiresTelemetryLogger : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.RequiredInterfaceImplementation>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TelemetryRequiresTelemetryLogger)));
        }

        [Test]
        [TestCase("GlobalVariableTelemetry")]
        [TestCase("GlobalVariableFeatureTelemetry")]
        [TestCase("LocalVariable")]
        [TestCase("Parameter")]
        [TestCase("ReturnValue")]
        [TestCase("PageActionTriggerLocal")]
        [TestCase("MultipleVariablesOneDiagnostic")]
        [TestCase("TwoObjectsTwoDiagnostics")]
        [TestCase("BothRulesOneObject")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TelemetryRequiresTelemetryLogger);
        }

        [Test]
        [TestCase("LoggerInOtherCodeunit")]
        [TestCase("LoggerInSameCodeunit")]
        [TestCase("NoUsage")]
        [TestCase("TestCodeunit")]
        [TestCase("TestRunnerCodeunit")]
        [TestCase("ObsoletePendingObject")]
        [TestCase("NameMatchIdMismatch")]
        [TestCase("IdMatchNameMismatch")]
        [TestCase("ObjectReferenceOnly")]
        [TestCase("NamespacedLogger")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TelemetryRequiresTelemetryLogger);
        }
    }
}
