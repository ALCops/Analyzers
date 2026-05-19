using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class UsePartialRecordsOnRead : NavCodeAnalysisBase
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
                    Path.Combine("Rules", nameof(UsePartialRecordsOnRead)));
        }

        [Test]
        [TestCase("LocalRecordGet")]
        [TestCase("LocalRecordGetBySystemId")]
        [TestCase("LocalRecordFindFirst")]
        [TestCase("LocalRecordFindSet")]
        [TestCase("LocalRecordFindLast")]
        [TestCase("LocalRecordFind")]
        [TestCase("LocalRecordMultipleReads")]
        [TestCase("LocalRecordRefFindFirst")]
        [TestCase("SetLoadFieldsAfterGet")]
        [TestCase("ClearBetweenSetLoadFieldsAndGet")]
        [TestCase("ResetBetweenSetLoadFieldsAndGet")]
        [TestCase("SetLoadFieldsNoArgsBetween")]
        [TestCase("CaseBranchWithoutSetLoadFields")]
        [TestCase("IfBranchWithoutSetLoadFields")]
        [TestCase("ClearResetsWriteOp")]
        [TestCase("ClearResetsPassedToFunction")]
        [TestCase("ClearResetsRecordAssignment")]
        [TestCase("LoopNoSetLoadFields")]
        [TestCase("SetupTableGetWithArgs")]
        [TestCase("SetupTableFindFirst")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UsePartialRecordsOnRead);
        }

        [Test]
        [TestCase("HasSetLoadFields")]
        [TestCase("HasSetLoadFieldsGetBySystemId")]
        [TestCase("HasAddLoadFields")]
        [TestCase("HasSetBaseLoadFields")]
        [TestCase("HasModify")]
        [TestCase("HasInsert")]
        [TestCase("HasDelete")]
        [TestCase("HasDeleteAll")]
        [TestCase("HasModifyAll")]
        [TestCase("HasRename")]
        [TestCase("PassedToFunction")]
        [TestCase("PassedToEvent")]
        [TestCase("PassedToPageRun")]
        [TestCase("TemporaryTable")]
        [TestCase("GlobalVariable")]
        [TestCase("ParameterVariable")]
        [TestCase("IsEmptyOnly")]
        [TestCase("HasTransferFields")]
        [TestCase("HasInit")]
        [TestCase("HasCopy")]
        [TestCase("CDSTable")]
        [TestCase("RecordRefSetTableWithModify")]
        [TestCase("RecordRefSetTablePassedToFunction")]
        [TestCase("DatabaseObjectReference")]
        [TestCase("IfBothBranchesSetLoadFields")]
        [TestCase("LoopSetLoadFieldsBefore")]
        [TestCase("FindSetWithModifyInLoop")]
        [TestCase("FindSetWithPassedToFunctionInLoop")]
        [TestCase("GetWithConditionalModify")]
        [TestCase("RecordAssignedToOther")]
        [TestCase("RecordAssignedToOtherQuotedName")]
        [TestCase("RecordAssignedBeforeRead")]
        [TestCase("SetupTableGet")]
        [TestCase("SetupTableGetNoSpace")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UsePartialRecordsOnRead);
        }

        [Test]
        [TestCase("SingleField")]
        [TestCase("MultipleFields")]
        [TestCase("QuotedFieldName")]
        [TestCase("NoFieldAccess")]
        [TestCase("SetRangeFieldExcluded")]
        [TestCase("SetFilterFieldExcluded")]
        [TestCase("SetRangeValueArgIncluded")]
        [TestCase("AllFieldsInFilters")]
        [TestCase("TestFieldIncluded")]
        [TestCase("SetCurrentKeyExcluded")]
        [TestCase("MixedFilterAndConsume")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UsePartialRecordsOnReadCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UsePartialRecordsOnRead);
        }
    }
}
