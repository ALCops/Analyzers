using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class UseValidateForFieldAssignment : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.UseValidateForFieldAssignment _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UseValidateForFieldAssignment>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UseValidateForFieldAssignment)));
        }

        [Test]
        [TestCase("SimpleAssignment")]
        [TestCase("CompoundAssignment")]
        [TestCase("AfterInit")]
        [TestCase("PrimaryKeyField")]
        [TestCase("OnValidateDifferentFieldOnRec")]
        [TestCase("OnValidateXRecSameField")]
        [TestCase("OnValidateOtherRecordSameField")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseValidateForFieldAssignment);
        }

        [Test]
        [TestCase("TemporaryVariable")]
        [TestCase("ValidateCall")]
        [TestCase("NonRecordVariable")]
        [TestCase("InsideOnValidateTrigger")]
        [TestCase("TableFieldOnValidateSameField")]
        [TestCase("TableExtensionFieldOnBeforeValidateSameField")]
        [TestCase("TableExtensionFieldOnAfterValidateSameField")]
        [TestCase("PageControlOnValidateSameField")]
        [TestCase("PageExtensionControlOnBeforeValidateSameField")]
        [TestCase("PageExtensionControlOnAfterValidateSameField")]
        [TestCase("OnValidateSameFieldThisReference")]
        [TestCase("PageControlOnValidateSameFieldBareReference")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                [
                    "TableExtensionFieldOnBeforeValidateSameField",
                    "TableExtensionFieldOnAfterValidateSameField",
                    "PageExtensionControlOnBeforeValidateSameField",
                    "PageExtensionControlOnAfterValidateSameField"
                ],
                testCase,
                "13.0",
                "No support for table/page extensions when target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ["OnValidateSameFieldThisReference"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseValidateForFieldAssignment);
        }

        [Test]
        [TestCase("SimpleAssignment")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UseValidateForFieldAssignmentCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UseValidateForFieldAssignment);
        }
    }
}
