using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class RestClientInitializeWithHttpClientHandler : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.RestClientInitializeWithHttpClientHandler>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(RestClientInitializeWithHttpClientHandler)));
        }

        [Test]
        [TestCase("LocalNoInitialization")]
        [TestCase("LocalBareInitialize")]
        [TestCase("LocalInitializeWithAuthenticationOnly")]
        [TestCase("LocalInitializeWithDefaultHandler")]
        [TestCase("LocalInitializeWithDefaultHandlerAndAuthentication")]
        [TestCase("LocalCreateWithoutHandlerAssigned")]
        [TestCase("LocalCreateWithAuthenticationAssigned")]
        [TestCase("LocalGoodThenBareInitialize")]
        [TestCase("LocalInitializeWithoutParentheses")]
        [TestCase("GlobalUsedWithoutInitialization")]
        [TestCase("GlobalBareInitializeInOneProcedureUsedInAnother")]
        [TestCase("LocalPassedToSameObjectProcedureNeverInitialized")]
        [TestCase("LocalPassedToSameObjectProcedureBareInitialize")]
        [TestCase("ThisReceiverProcedureCall")]
        [TestCase("PageActionTriggerLocal")]
        [TestCase("TableTriggerLocal")]
        [TestCase("NamespacedFullyQualifiedType")]
        [TestCase("TwoLocalsTwoDiagnostics")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisReceiverProcedureCall"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.RestClientInitializeWithHttpClientHandler);
        }

        [Test]
        [TestCase("OwnHandlerCodeunit")]
        [TestCase("OwnHandlerAndAuthentication")]
        [TestCase("InterfaceTypedHandlerLocal")]
        [TestCase("FactoryReturnedHandler")]
        [TestCase("HandlerReceivedAsParameter")]
        [TestCase("CreateWithHandlerAssigned")]
        [TestCase("CreateResultPassedAsArgument")]
        [TestCase("OwnCodeunitNamedHttpClientHandler")]
        [TestCase("PassedToOtherObjectProcedure")]
        [TestCase("PassedToInterfaceMethod")]
        [TestCase("PassedToEventPublisher")]
        [TestCase("TwoLevelFollowingEndsInExternalCall")]
        [TestCase("GlobalPassedToOtherCodeunit")]
        [TestCase("AssignedFromProcedureReturn")]
        [TestCase("AssignedToGlobal")]
        [TestCase("ExitWithRestClient")]
        [TestCase("ClearThenGet")]
        [TestCase("PassedToSameObjectProcedureInitializedWithHandler")]
        [TestCase("GlobalInitializedInSetupProcedure")]
        [TestCase("RecursiveSameObjectProcedures")]
        [TestCase("VarParameterOnly")]
        [TestCase("DeclaredButUnused")]
        [TestCase("TestCodeunit")]
        [TestCase("TestRunnerCodeunit")]
        [TestCase("ObsoletePendingObject")]
        [TestCase("RestClientIdMatchNameMismatch")]
        [TestCase("RestClientNameMatchIdMismatch")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.RestClientInitializeWithHttpClientHandler);
        }
    }
}
