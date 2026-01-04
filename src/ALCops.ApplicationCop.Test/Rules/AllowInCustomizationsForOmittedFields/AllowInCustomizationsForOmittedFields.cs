using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class AllowInCustomizationsForOmittedFields : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.AllowInCustomizationsForOmittedFields>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(AllowInCustomizationsForOmittedFields)));
        }

        [Test]
        [TestCase("FieldOmittedPage")]
        [TestCase("ObsoleteStateNo")]
        [TestCase("TableExtension")]
        [TestCase("TableExtensionBaseDrillDownPageId")]
        [TestCase("TableExtensionBaseLookupPageId")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExtension", "TableExtensionBaseDrillDownPageId", "TableExtensionBaseLookupPageId"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AllowInCustomizationsForOmittedFields);
        }

        [Test]
        [TestCase("AllowInCustomizationsIsSet")]
        [TestCase("DisabledField")]
        [TestCase("FieldOnPage")]
        [TestCase("FieldTypeNotSupported")]
        [TestCase("FlowFilterField")]
        [TestCase("ObsoleteStatePending")]
        [TestCase("TableExtension")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExtension"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.AllowInCustomizationsForOmittedFields);
        }
    }
}