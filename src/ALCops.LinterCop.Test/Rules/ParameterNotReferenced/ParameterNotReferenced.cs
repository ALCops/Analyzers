using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class ParameterNotReferenced : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.ParameterNotReferenced _analyzer = new();
        private string _testCasePath;

        private static void RequireSdkV13Support()
        {
            RequireMinimumVersion("13.0", "LC0095/LC0099 requires SDK v13+ for reliable IMethodSymbol.IsLocal behavior");
        }

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ParameterNotReferenced>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ParameterNotReferenced)));
        }

        [Test]
        [TestCase("InternalProcedure")]
        [TestCase("PublicProcedure")]
        [TestCase("MultipleParamsOneUnused")]
        [TestCase("VarParameterUnused")]
        [TestCase("ErrorInfoInPage")]
        [TestCase("ErrorInfoMultipleParams")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireSdkV13Support();

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        [Test]
        [TestCase("EventSubscriber")]
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
        public async Task NoDiagnostic(string testCase)
        {
            RequireSdkV13Support();

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ParameterNotReferenced);
        }

        [Test]
        [TestCase("RemoveSingleParameter")]
        [TestCase("RemoveMiddleParameter")]
        [TestCase("RemoveMiddleParameterMultiline")]
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
        [TestCase("RemoveSingleParameterEventSubscriber")]
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
    }
}
