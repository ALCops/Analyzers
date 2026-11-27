using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class PageStyleStringLiteral : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PageStyleStringLiteral>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(PageStyleStringLiteral));
        }

        [Test]
        [TestCase("ArrayElementAssignment")]
        [TestCase("CaseLineStatement")]
        [TestCase("ExitStatement")]
        [TestCase("Label")]
        [TestCase("Page")]
        [TestCase("ReportLabelLocked")]
        [TestCase("TernaryAssignment")]
        [TestCase("TernaryConditionComparison")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireMinimumVersion("14.0", "No support for PageStyle datatype in versions below 14.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PageStyleStringLiteral);
        }

        [Test]
        [TestCase("AssignToStyleExpr")]
        [TestCase("AssignToTableField")]
        [TestCase("AssignToTableFieldLocal")]
        [TestCase("AssignToTableFieldRec")]
        [TestCase("AttributeArgument")]
        [TestCase("CaseLabel")]
        [TestCase("ComparisonNestedTernary")]
        [TestCase("ComparisonOperators")]
        [TestCase("ComparisonTernaryOperand")]
        [TestCase("Enum")]
        [TestCase("IfStatement")]
        [TestCase("Label")]
        [TestCase("LockedLabelComment")]
        [TestCase("LockedLabelLowercase")]
        [TestCase("LockedLabelUppercase")]
        [TestCase("MethodArgument")]
        [TestCase("Page")]
        [TestCase("PropertyValue")]
        [TestCase("RecordMethodInvocation")]
        [TestCase("ReportLabelMultilanguage")]
        [TestCase("ReportLabelUnlocked")]
        [TestCase("SetRangeArgumentBareSelf")]
        [TestCase("SetRangeArgumentThisSelf")]
        [TestCase("TableFieldCaptions")]
        [TestCase("TableFieldWriteBareSelf")]
        [TestCase("TableFieldWriteThisSelf")]
        [TestCase("TextConst")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireMinimumVersion("14.0", "No support for PageStyle datatype in versions below 14.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PageStyleStringLiteral);
        }
    }
}