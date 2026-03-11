using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class AllowInCustomizationsRedundancy : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.AllowInCustomizationsRedundancy _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.AllowInCustomizationsRedundancy>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(AllowInCustomizationsRedundancy)));
        }

        [Test]
        [TestCase("RedundantAllowInCustomizations")]
        [TestCase("RedundantAllowInCustomizationsTableExtension")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["RedundantAllowInCustomizations", "RedundantAllowInCustomizationsTableExtension"],
                testCase,
                "16.0",
                "AllowInCustomizations property is not supported in versions prior to 16.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AllowInCustomizationsRedundancy);
        }

        [Test]
        [TestCase("NonRedundantAllowInCustomizations")]
        [TestCase("NoAllowInCustomizationsOnTable")]
        [TestCase("NoAllowInCustomizationsOnField")]
        [TestCase("ObsoleteField")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["NoAllowInCustomizationsOnField", "NonRedundantAllowInCustomizations", "ObsoleteField"],
                testCase,
                "16.0",
                "AllowInCustomizations property is not supported in versions prior to 16.0."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.AllowInCustomizationsRedundancy);
        }

        [Test]
        [TestCase("AllowInCustomizations")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<AllowInCustomizationsRedundancyCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.AllowInCustomizationsRedundancy);
        }
    }
}
