using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class PartialRecordsBeforeWriteOperation : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.PartialRecordOperations _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PartialRecordOperations>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PartialRecordsBeforeWriteOperation)));
        }

        [Test]
        [TestCase("SetLoadFieldsThenModify")]
        [TestCase("SetLoadFieldsThenDelete")]
        [TestCase("SetLoadFieldsThenRename")]
        [TestCase("SetLoadFieldsThenTransferFields")]
        [TestCase("SetLoadFieldsThenCopy")]
        [TestCase("AddLoadFieldsThenModify")]
        [TestCase("SetBaseLoadFieldsThenModify")]
        [TestCase("BranchWithWrite")]
        [TestCase("QualifiedSetLoadFields")]
        [TestCase("SetLoadFieldsThenGetBySystemIdThenModify")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PartialRecordsBeforeWriteOperation);
        }

        [Test]
        [TestCase("SetLoadFieldsReadOnly")]
        [TestCase("NoSetLoadFieldsModify")]
        [TestCase("ModifyAll")]
        [TestCase("DeleteAll")]
        [TestCase("Init")]
        [TestCase("TemporaryTable")]
        [TestCase("ClearBetweenSetLoadFieldsAndModify")]
        [TestCase("WriteThenSetLoadFields")]
        [TestCase("WriteAfterSetLoadFieldsBeforePartialRead")]
        [TestCase("SetLoadFieldsNoArgsResetsPartialRead")]
        [TestCase("SetLoadFieldsThenInsert")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PartialRecordsBeforeWriteOperation);
        }

        [Test]
        [TestCase("RemoveSetLoadFields")]
        [TestCase("RemoveAddLoadFields")]
        [TestCase("RemoveSetBaseLoadFields")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<PartialRecordsBeforeWriteOperationCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.PartialRecordsBeforeWriteOperation);
        }
    }
}
