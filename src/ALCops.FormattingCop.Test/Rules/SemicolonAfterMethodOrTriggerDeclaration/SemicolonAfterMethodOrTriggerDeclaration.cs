using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class SemicolonAfterMethodOrTriggerDeclaration : NavCodeAnalysisBase
    {
        private const string DefaultInterfaceMethodGateReason =
            "Default interface method bodies need runtime 18.0; the harness compiles at the SDK's current release.";

        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.SemicolonAfterMethodOrTriggerDeclaration>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(SemicolonAfterMethodOrTriggerDeclaration));
        }

        [Test]
        [TestCase("ProcedureWithSemicolonAfterDeclaration")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.SemicolonAfterMethodOrTriggerDeclaration);
        }

        [Test]
        [TestCase("ObsoleteStatePending")]
        [TestCase("ProcedureWithoutBody")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.SemicolonAfterMethodOrTriggerDeclaration);
        }

        // An interface method with a default body (runtime 18.0) is checked like a codeunit method; an
        // abstract member keeps the semicolon that ends its declaration.
        [Test]
        [TestCase("InterfaceDefaultBodyWithSemicolon")]
        public async Task HasDiagnosticInterface(string testCase)
        {
            RequireMinimumVersion("18.0", DefaultInterfaceMethodGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.SemicolonAfterMethodOrTriggerDeclaration);
        }

        [Test]
        [TestCase("InterfaceAbstractMethod")]
        public async Task NoDiagnosticInterface(string testCase)
        {
            RequireMinimumVersion("18.0", DefaultInterfaceMethodGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.SemicolonAfterMethodOrTriggerDeclaration);
        }
    }
}