using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class UnusedParameter : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.UnusedParameter _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UnusedParameter>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UnusedParameter)));
        }

        [Test]
        [TestCase("InternalProcedure")]
        [TestCase("PublicProcedure")]
        [TestCase("EventSubscriber")]
        [TestCase("MultipleParamsOneUnused")]
        [TestCase("VarParameterUnused")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UnusedParameter);
        }

        [Test]
        [TestCase("LocalProcedure")]
        [TestCase("TriggerUnusedParam")]
        [TestCase("InterfaceImplementation")]
        [TestCase("EventDeclaration")]
        [TestCase("ObsoleteProcedure")]
        [TestCase("AllParametersUsed")]
        [TestCase("ParameterUsedInExpression")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UnusedParameter);
        }

        [Test]
        [TestCase("RemoveSingleParameter")]
        [TestCase("RemoveMiddleParameter")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UnusedParameterCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UnusedParameter);
        }
    }
}
