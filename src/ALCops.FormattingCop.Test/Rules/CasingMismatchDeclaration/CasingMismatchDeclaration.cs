using ALCops.FormattingCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class CasingMismatchDeclaration : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.CasingMismatchDeclaration _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CasingMismatchDeclaration>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(CasingMismatchDeclaration)));
        }

        [Test]
        [TestCase("DataType")]
        [TestCase("EnumDataType")]
        [TestCase("FieldGroup")]
        [TestCase("LabelDataType")]
        [TestCase("LabelProperties")]
        [TestCase("LengthDataType")]
        [TestCase("OptionDataType")]
        [TestCase("Property")]
        [TestCase("TextConstDataType")]
        [TestCase("TriggerDeclaration")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Property"],
                testCase,
                "14.0",
                "error AL0124: The property 'SCOPE' cannot be used in this context"
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CasingMismatch);
        }

        [Test]
        [TestCase("AccessByPermission")]
        [TestCase("DataType")]
        [TestCase("EnumDataType")]
        [TestCase("FieldGroup")]
        [TestCase("IdentifierNameSyntaxGrouping")]
        [TestCase("LabelDataType")]
        [TestCase("LabelProperties")]
        [TestCase("LengthDataType")]
        [TestCase("OptionDataType")]
        [TestCase("Property")]
        [TestCase("TextConstDataType")]
        [TestCase("TriggerDeclaration")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Property"],
                testCase,
                "14.0",
                "error AL0124: The property 'SCOPE' cannot be used in this context"
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CasingMismatch);
        }

        // [TestCase("ObjectKeyword")]
        // public async Task HasFix(string testCase)
        // {
        //     var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
        //         .ConfigureAwait(false);

        //     var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
        //         .ConfigureAwait(false);

        //     var fixture = RoslynFixtureFactory.Create<CasingMismatchCodeFix>(
        //         new CodeFixTestFixtureConfig
        //         {
        //             AdditionalAnalyzers = [_analyzer]
        //         });

        //     fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.CasingMismatch);
        // }
    }
}