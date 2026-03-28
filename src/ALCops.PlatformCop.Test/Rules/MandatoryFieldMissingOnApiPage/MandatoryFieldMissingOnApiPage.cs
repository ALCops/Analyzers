using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class MandatoryFieldMissingOnApiPage : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.MandatoryFieldMissingOnApiPage _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.MandatoryFieldMissingOnApiPage>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(MandatoryFieldMissingOnApiPage)));
        }

        [Test]
        [TestCase("MissingExposedAsId")]
        [TestCase("MissingExposedAsLastModifiedDateTime")]
        [TestCase("MissingFields")]
        [TestCase("MissingSystemId")]
        [TestCase("MissingSystemModifiedAt")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.MandatoryFieldMissingOnApiPage);
        }

        [Test]
        [TestCase("FieldsIdAndLastModifiedDateTimeDeclared")]
        [TestCase("NoFieldsExposed")]
        [TestCase("NoSourceTable")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.MandatoryFieldMissingOnApiPage);
        }

        [Test]
        [TestCase("MissingFields")]
        [TestCase("MissingSystemId")]
        [TestCase("MissingSystemModifiedAt")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<MandatoryFieldMissingOnApiPageCodeFix>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.MandatoryFieldMissingOnApiPage);
        }
    }
}