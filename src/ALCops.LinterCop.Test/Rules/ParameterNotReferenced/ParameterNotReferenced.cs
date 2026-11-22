using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class ParameterNotReferenced : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private AnalyzerTestFixture _errorTolerantFixture;
        private static readonly Analyzers.ParameterNotReferenced _analyzer = new();
        private string _testCasePath;
        private const string DefaultInterfaceMethodGateReason =
            "Default interface method bodies need runtime 18.0; the harness compiles at the SDK's current release.";

        private static void RequireSdkV13Support()
        {
            RequireMinimumVersion("13.0", "LC0095/LC0099 requires SDK v13+ for reliable IMethodSymbol.IsLocal behavior");
        }

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ParameterNotReferenced>();

            // A reference under a binding error needs a fixture that does not compile.
            _errorTolerantFixture = RoslynFixtureFactory.Create<Analyzers.ParameterNotReferenced>(
                new AnalyzerTestFixtureConfig
                {
                    ThrowsWhenInputDocumentContainsError = false
                });

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(ParameterNotReferenced));
        }

        [Test]
        [TestCase("InternalProcedure")]
        [TestCase("PublicProcedure")]
        [TestCase("MultipleParamsOneUnused")]
        [TestCase("VarParameterUnused")]
        [TestCase("ErrorInfoInPage")]
        [TestCase("ErrorInfoMultipleParams")]
        [TestCase("ParameterNamedLikeAccessedField")]
        [TestCase("ParameterUsedAsSetRangeField")]
        [TestCase("ParameterNamedLikeQueryColumn")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireSdkV13Support();

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        [Test]
        [TestCase("EventSubscriber")]
        [TestCase("EventSubscriberParameterNamedLikeField")]
        public async Task HasDiagnosticEventSubscriber(string testCase)
        {
            RequireSdkV13Support();

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberParameterNotReferenced);
        }

        [Test]
        [TestCase("LocalProcedure")]
        [TestCase("TriggerUnusedParam")]
        [TestCase("InterfaceImplementation")]
        [TestCase("InterfaceImplementationWrongCasing")]
        [TestCase("EventDeclaration")]
        [TestCase("ObsoleteProcedure")]
        [TestCase("AllParametersUsed")]
        [TestCase("ParameterUsedInExpression")]
        [TestCase("ErrorInfoCallbackInCodeunit")]
        [TestCase("NotificationCallbackInCodeunit")]
        [TestCase("MessageHandlerInCodeunit")]
        [TestCase("ConfirmHandlerInCodeunit")]
        [TestCase("ParameterUsedWithDifferentCasing")]
        [TestCase("ParameterPassedAsVarArgument")]
        [TestCase("ParameterAssignedOnly")]
        [TestCase("ParameterUsedAsRecordReceiver")]
        [TestCase("ParameterUsedAsFilterValue")]
        [TestCase("ParameterUsedInReportProcedure")]
        [TestCase("ParameterUsedInIsExpression")]
        [TestCase("ParameterUsedInAsExpression")]
        [TestCase("ParameterUsedInConditionalExpression")]
        [TestCase("ParameterUsedAsTestPageReceiver")]
        [TestCase("ParameterUsedAsQueryReceiver")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireSdkV13Support();
            SkipTestIfVersionIsTooLow(
                ["ParameterUsedInIsExpression", "ParameterUsedInAsExpression", "ParameterUsedInConditionalExpression"],
                testCase,
                "14.0",
                "The 'is'/'as' interface operators and the conditional expression require runtime version 14.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        // A codeunit that implements an interface with default methods still reports its own procedures.
        [Test]
        [TestCase("DefaultInterfaceMethodImplementerOwnProcedure")]
        public async Task HasDiagnosticDefaultInterfaceMethod(string testCase)
        {
            RequireMinimumVersion("18.0", DefaultInterfaceMethodGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        // An interface method with a default body (runtime 18.0) is reached by the code block action like
        // any other method body; its parameters are part of the contract every implementer shares.
        [Test]
        [TestCase("DefaultInterfaceMethodUnusedParameter")]
        [TestCase("DefaultInterfaceMethodEmptyBody")]
        [TestCase("DefaultInterfaceMethodOverloadBridge")]
        [TestCase("DefaultInterfaceMethodOverriddenInCodeunit")]
        [TestCase("DefaultInterfaceMethodNamespaced")]
        public async Task NoDiagnosticDefaultInterfaceMethod(string testCase)
        {
            RequireMinimumVersion("18.0", DefaultInterfaceMethodGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoException(code.Replace("[|", "").Replace("|]", ""));
            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        // The interface with the default method lives in a second workspace project, so the overriding
        // codeunit sees it through the referenced module rather than through its own declarations.
        [Test]
        [TestCase("DefaultInterfaceMethodOverriddenFromDependency")]
        public async Task NoDiagnosticWithDependencyProject(string testCase)
        {
            RequireMinimumVersion("18.0", DefaultInterfaceMethodGateReason);

            var fixture = await CreateFixtureWithDependencyProject(testCase).ConfigureAwait(false);
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            fixture.NoException(code.Replace("[|", "").Replace("|]", ""));
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        [Test]
        [TestCase("ParameterUsedUnderInvalidNode")]
        public async Task NoDiagnosticInDocumentWithErrors(string testCase)
        {
            RequireSdkV13Support();

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _errorTolerantFixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        [Test]
        [TestCase("RemoveSingleParameter")]
        [TestCase("RemoveMiddleParameter")]
        [TestCase("RemoveMiddleParameterMultiline")]
        [TestCase("RemoveSingleParameterWithPragma")]
        public async Task HasFix(string testCase)
        {
            RequireSdkV13Support();

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ParameterNotReferencedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.ParameterNotReferenced);
        }

        [Test]
        [TestCase("ConditionalParameter")]
        public async Task NoFix(string testCase)
        {
            RequireSdkV13Support();

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoFix), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ParameterNotReferencedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.NoCodeFix(code, DiagnosticDescriptors.ParameterNotReferenced);
        }

        [Test]
        [TestCase("ConditionalEventSubscriberParameter")]
        public async Task NoFixEventSubscriber(string testCase)
        {
            RequireSdkV13Support();

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoFix), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ParameterNotReferencedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.NoCodeFix(code, DiagnosticDescriptors.EventSubscriberParameterNotReferenced);
        }

        [Test]
        [TestCase("RemoveSingleParameterEventSubscriber")]
        [TestCase("RemoveSingleParameterEventSubscriberWithPragma")]
        public async Task HasFixEventSubscriber(string testCase)
        {
            RequireSdkV13Support();

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ParameterNotReferencedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.EventSubscriberParameterNotReferenced);
        }

        [Test]
        [TestCase("RemoveTwoParametersSingleMethod")]
        [TestCase("RemoveUnusedFromMultipleMethods")]
        [TestCase("RemoveParametersWithComments")]
        [TestCase("RemoveParametersWithPragmas")]
        public async Task HasFixAll(string testCase)
        {
            RequireSdkV13Support();

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ParameterNotReferencedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestFixAll(
                currentCode,
                expectedCode,
                DiagnosticIds.ParameterNotReferenced,
                codeFixIndex: 0,
                equivalenceKey: $"{nameof(ParameterNotReferencedCodeFixProvider)}.RegularProcedure");
        }

        [Test]
        public async Task HasFixAllMixedProcedureKinds()
        {
            RequireSdkV13Support();

            string testCasePath = Path.Combine(_testCasePath, nameof(HasFixAll), "RemoveMixedProcedureKinds");
            var currentCode = await File.ReadAllTextAsync(Path.Combine(testCasePath, "current.al"))
                .ConfigureAwait(false);
            var regularExpectedCode = await File.ReadAllTextAsync(Path.Combine(testCasePath, "expected-regular.al"))
                .ConfigureAwait(false);
            var subscriberExpectedCode = await File.ReadAllTextAsync(Path.Combine(testCasePath, "expected-subscriber.al"))
                .ConfigureAwait(false);
            string regularCurrentCode = currentCode.Replace(
                "[|SubscriberUnused: Boolean|]",
                "SubscriberUnused: Boolean",
                StringComparison.Ordinal);
            string subscriberCurrentCode = currentCode.Replace(
                "[|RegularUnused: Text|]",
                "RegularUnused: Text",
                StringComparison.Ordinal);

            var fixture = RoslynFixtureFactory.Create<ParameterNotReferencedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestFixAll(
                regularCurrentCode,
                regularExpectedCode,
                DiagnosticIds.ParameterNotReferenced,
                codeFixIndex: 0,
                equivalenceKey: $"{nameof(ParameterNotReferencedCodeFixProvider)}.RegularProcedure");
            fixture.TestFixAll(
                subscriberCurrentCode,
                subscriberExpectedCode,
                DiagnosticIds.EventSubscriberParameterNotReferenced,
                codeFixIndex: 0,
                equivalenceKey: $"{nameof(ParameterNotReferencedCodeFixProvider)}.EventSubscriber");
        }

        [Test]
        [TestCase("RemoveTwoParametersEventSubscriber")]
        public async Task HasFixAllEventSubscriber(string testCase)
        {
            RequireSdkV13Support();

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ParameterNotReferencedCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestFixAll(
                currentCode,
                expectedCode,
                DiagnosticIds.EventSubscriberParameterNotReferenced,
                codeFixIndex: 0,
                equivalenceKey: $"{nameof(ParameterNotReferencedCodeFixProvider)}.EventSubscriber");
        }

        private async Task<AnalyzerTestFixture> CreateFixtureWithDependencyProject(string testCase)
        {
            var dependencyCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "DependencyProject", $"{testCase}.al"))
                .ConfigureAwait(false);

            return RoslynFixtureFactory.Create<Analyzers.ParameterNotReferenced>(
                new AnalyzerTestFixtureConfig
                {
                    DependencyProjects = [new DependencyProject("DependencyApp", dependencyCode)]
                });
        }
    }
}
