using ALCops.FormattingCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class CasingMismatchDeclaration : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.CasingMismatchIdentifier _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CasingMismatchIdentifier>();

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
        [TestCase("MemberAccessField")]
        [TestCase("OptionDataType")]
        [TestCase("Property")]
        [TestCase("TextConstDataType")]
        [TestCase("TestType")]
        [TestCase("TriggerDeclaration")]
        [TestCase("ObjectTypeOptionAccess")]
        [TestCase("CrossScopeIdentifierGrouping")]
        [TestCase("GlobalVarAndParam")]
        [TestCase("GlobalVarAndParamThisPrefix")]
        [TestCase("GlobalVarAndReturnValue")]
        [TestCase("GlobalVarAndLocalVar")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Property", "DataType", "TriggerDeclaration"],
                testCase,
                "14.0"
            );

            SkipTestIfVersionIsTooLow(
                ["TestType"],
                testCase,
                "16.0"
            );

            SkipTestIfVersionIsTooLow(
                ["GlobalVarAndParamThisPrefix"],
                testCase,
                "14.0"
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
        [TestCase("MemberAccessField")]
        [TestCase("OptionDataType")]
        [TestCase("Property")]
        [TestCase("TextConstDataType")]
        [TestCase("TestType")]
        [TestCase("TriggerDeclaration")]
        [TestCase("VariableNamedAfterKeyword")]
        [TestCase("ObjectTypeOptionAccess")]
        [TestCase("DeeplyNestedExpression")]
        [TestCase("CrossScopeIdentifierGrouping")]
        [TestCase("CrossScopeQualifiedNameGrouping")]
        [TestCase("GlobalVarAndParam")]
        [TestCase("GlobalVarAndParamThisPrefix")]
        [TestCase("GlobalVarAndReturnValue")]
        [TestCase("GlobalVarAndLocalVar")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Property", "DataType", "TriggerDeclaration", "TestType"],
                testCase,
                "14.0"
            );

            SkipTestIfVersionIsTooLow(
                ["TestType"],
                testCase,
                "16.0"
            );

            SkipTestIfVersionIsTooLow(
                ["GlobalVarAndParamThisPrefix"],
                testCase,
                "14.0"
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CasingMismatch);
        }
    }
}