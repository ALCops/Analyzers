using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class RestClientRequiresHttpClientHandler : NavCodeAnalysisBase
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
                    Path.Combine("Rules", nameof(RestClientRequiresHttpClientHandler)));
        }

        [Test]
        [TestCase("GlobalVariable")]
        [TestCase("LocalVariable")]
        [TestCase("Parameter")]
        [TestCase("ReturnValue")]
        [TestCase("PageActionTriggerLocal")]
        [TestCase("PageFieldTriggerLocal")]
        [TestCase("TableTriggerLocal")]
        [TestCase("TableFieldTriggerLocal")]
        [TestCase("ReportDataItemTriggerLocal")]
        [TestCase("XmlPortElementTriggerLocal")]
        [TestCase("QueryTriggerLocal")]
        [TestCase("TableExtensionTriggerLocal")]
        [TestCase("PageExtensionActionTriggerLocal")]
        [TestCase("MultipleVariablesOneDiagnostic")]
        [TestCase("TwoObjectsTwoDiagnostics")]
        [TestCase("BothRulesOneObject")]
        [TestCase("NamespacedUsage")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExtensionTriggerLocal", "PageExtensionActionTriggerLocal"],
                testCase,
                "13.0",
                "AL 12 rejects extensions whose target is declared in the same module (AL0334).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.RestClientRequiresHttpClientHandler);
        }

        [Test]
        [TestCase("HandlerInOtherCodeunit")]
        [TestCase("HandlerInSameCodeunit")]
        [TestCase("NoUsage")]
        [TestCase("TestCodeunit")]
        [TestCase("TestRunnerCodeunit")]
        [TestCase("ObsoletePendingObject")]
        [TestCase("NameMatchIdMismatch")]
        [TestCase("IdMatchNameMismatch")]
        [TestCase("ObjectReferenceOnly")]
        [TestCase("NamespacedHandler")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.RestClientRequiresHttpClientHandler);
        }
    }
}
