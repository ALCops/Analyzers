using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class UseSequentialGuid : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.UseSequentialGuid _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UseSequentialGuid>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UseSequentialGuid)));
        }

        [Test]
        [TestCase("DirectAssignmentToPrimaryKey")]
        [TestCase("ValidatePrimaryKeyField")]
        [TestCase("DirectAssignmentToSecondaryKeyField")]
        [TestCase("VariableAssignedToKeyField")]
        [TestCase("CrossProcedureIntraModule")]
        [TestCase("OnInsertTrigger")]
        [TestCase("QualifiedCreateGuid")]
        [TestCase("MultiLevelTracing")]
        [TestCase("ValidateSecondaryKeyField")]
        public async Task HasDiagnostic(string testCase)
        {
            // https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/guid/guid-createsequentialguid-method
            RequireMinimumVersion("16.0", "Available with runtime version 16.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseSequentialGuid);
        }

        [Test]
        [TestCase("NonKeyGuidField")]
        [TestCase("TemporaryTable")]
        [TestCase("GuidVariableNotInKey")]
        [TestCase("NonGuidKeyField")]
        [TestCase("AssignedToGuidVariableUsedElsewhere")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseSequentialGuid);
        }

        [Test]
        [TestCase("SimpleCreateGuid")]
        [TestCase("QualifiedCreateGuid")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UseSequentialGuidCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UseSequentialGuid);
        }
    }
}
