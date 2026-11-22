using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class UsePartialRecordsForLoops : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.UsePartialRecordsForLoops _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UsePartialRecordsForLoops>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(UsePartialRecordsForLoops));
        }

        [Test]
        [TestCase("CaseBranchWithoutSetLoadFields")]
        [TestCase("ClearBetweenSetLoadFieldsAndFindSet")]
        [TestCase("ClearResetsPassedToFunction")]
        [TestCase("ClearResetsRecordAssignment")]
        [TestCase("ClearResetsWriteOp")]
        [TestCase("CompoundKeyOutsideSetupNamespace")]
        [TestCase("EarlierReadReplaced")]
        [TestCase("ExitFieldAccess")]
        [TestCase("ExitGuardThenRead")]
        [TestCase("ExitInOtherBranchOnly")]
        [TestCase("ExitOtherVariable")]
        [TestCase("FindFirstThenNextLoop")]
        [TestCase("FindLastThenNextBackLoop")]
        [TestCase("GetThenNextLoop")]
        [TestCase("IfBranchWithoutSetLoadFields")]
        [TestCase("IfFindSetThenBeginRepeat")]
        [TestCase("IfNotFindSetExitThenRepeat")]
        [TestCase("IntegerCodePrimaryKey")]
        [TestCase("LocalRecordFindMinus")]
        [TestCase("LocalRecordFindPlus")]
        [TestCase("LocalRecordFindSet")]
        [TestCase("LocalRecordRefFindSetLoop")]
        [TestCase("NamespacedTableReference")]
        [TestCase("NestedLoopsTwoVariables")]
        [TestCase("NextAsStatementInWhileBody")]
        [TestCase("NextLoopInCaseLine")]
        [TestCase("NextWithoutParentheses")]
        [TestCase("ReadInBothBranchesOneLoop")]
        [TestCase("RecordRefNextLoopAfterOpen")]
        [TestCase("ResetBetweenSetLoadFieldsAndFindSet")]
        [TestCase("SetLoadFieldsAfterLoop")]
        [TestCase("SetLoadFieldsBetweenReadAndLoop")]
        [TestCase("SetLoadFieldsNoArgsBetween")]
        [TestCase("SetupNamespaceAutoIncrementKey")]
        [TestCase("SetupNamespaceAutoIncrementLaterKeyField")]
        [TestCase("SetupNamespaceSegmentNotLast")]
        [TestCase("SetupTableGetRecordOnceDifferentName")]
        [TestCase("SetupTableGetRecordOnceWithParameter")]
        [TestCase("SetupTableGetRecordOnceWithReturn")]
        [TestCase("TableNoOnRunRecRead")]
        [TestCase("ThisQualifiedGlobalReadBesideLocalRead")]
        [TestCase("ThisQualifiedGlobalWriteDoesNotSuppressLocal")]
        [TestCase("TwoLoopsSameVariable")]
        [TestCase("UntilNextOrStop")]
        [TestCase("WhileNextCondition")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisQualifiedGlobalWriteDoesNotSuppressLocal", "ThisQualifiedGlobalReadBesideLocalRead"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UsePartialRecordsForLoops);
        }

        [Test]
        [TestCase("CDSTable")]
        [TestCase("ClearBetweenFindSetAndLoop")]
        [TestCase("CodePrimaryKeyFindSet")]
        [TestCase("CodePrimaryKeyGet")]
        [TestCase("DatabaseObjectReference")]
        [TestCase("EarlierReadReplacedNotReported")]
        [TestCase("ExitRecord")]
        [TestCase("ExitRecordInCaseBranch")]
        [TestCase("ExitRecordInIfBranch")]
        [TestCase("ExitRecordInLoop")]
        [TestCase("FindSetWithModifyInLoop")]
        [TestCase("FindSetWithPassedToFunctionInLoop")]
        [TestCase("FindSetWithoutNext")]
        [TestCase("ForEachGetInBody")]
        [TestCase("GetWithConditionalModify")]
        [TestCase("GlobalVariable")]
        [TestCase("HasAddLoadFields")]
        [TestCase("HasCopy")]
        [TestCase("HasDelete")]
        [TestCase("HasDeleteAll")]
        [TestCase("HasInit")]
        [TestCase("HasInsert")]
        [TestCase("HasModify")]
        [TestCase("HasModifyAll")]
        [TestCase("HasRename")]
        [TestCase("HasSetBaseLoadFields")]
        [TestCase("HasSetLoadFields")]
        [TestCase("HasTransferFields")]
        [TestCase("IfBothBranchesSetLoadFields")]
        [TestCase("IsEmptyOnly")]
        [TestCase("LoopSetLoadFieldsBefore")]
        [TestCase("NamePrimaryKeyGet")]
        [TestCase("NextOutsideLoop")]
        [TestCase("OtherRecordReadInsideLoop")]
        [TestCase("ParameterVariable")]
        [TestCase("PassedToEvent")]
        [TestCase("PassedToFunction")]
        [TestCase("PassedToPageRun")]
        [TestCase("ReadInsideOwnLoopBody")]
        [TestCase("RecordAssignedBeforeRead")]
        [TestCase("RecordAssignedToOther")]
        [TestCase("RecordAssignedToOtherQuotedName")]
        [TestCase("RecordRefSetTableBeforeLoop")]
        [TestCase("RecordRefSetTableInLoop")]
        [TestCase("SetupNamespaceCompoundKeyGet")]
        [TestCase("SetupNamespaceFindSetLoop")]
        [TestCase("SetupTableFindFirst")]
        [TestCase("SetupTableGet")]
        [TestCase("SetupTableGetNoSpace")]
        [TestCase("SetupTableGetRecordOnce")]
        [TestCase("SetupTableGetRecordOnceLowercase")]
        [TestCase("SingleReadFindEquals")]
        [TestCase("SingleReadFindFirst")]
        [TestCase("SingleReadFindLast")]
        [TestCase("SingleReadGet")]
        [TestCase("SingleReadGetBySystemId")]
        [TestCase("TemporaryTable")]
        [TestCase("ThisQualifiedDataItemRead")]
        [TestCase("ThisQualifiedGlobalRead")]
        [TestCase("ThisSelfNextToLocalNamedLikeTable")]
        [TestCase("WhileFindSetNoNext")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisQualifiedGlobalRead", "ThisQualifiedDataItemRead", "ThisSelfNextToLocalNamedLikeTable"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UsePartialRecordsForLoops);
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
        [TestCase("QuotedLocalReceiver")]
        [TestCase("ThisQualifiedGlobalFieldExcluded")]
        [TestCase("IfThenWithoutBlock")]
        [TestCase("IfThenElseWithoutBlock")]
        [TestCase("EmbeddedStatementTrailingComment")]
        [TestCase("ThenBranchTrailingCommentBeforeElse")]
        [TestCase("ReadInIfConditionEmbedded")]
        [TestCase("ElseIfChain")]
        [TestCase("CaseLineWithoutBlock")]
        [TestCase("CaseElseList")]
        [TestCase("RepeatBodyDirect")]
        [TestCase("WhileBodyWithoutBlock")]
        [TestCase("ForBodyWithoutBlock")]
        [TestCase("ForEachBodyWithoutBlock")]
        [TestCase("WithBodyWithoutBlock")]
        [TestCase("SameLineElse")]
        [TestCase("AssertErrorStatement")]
        [TestCase("ElseBranchOnly")]
        [TestCase("AssertErrorEmbeddedIf")]
        [TestCase("ElseAfterConditionalDirective")]
        [TestCase("RegionAroundEmbeddedStatement")]
        public async Task HasFix(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisQualifiedGlobalFieldExcluded"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UsePartialRecordsForLoopsCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UsePartialRecordsForLoops);
        }

        [Test]
        [TestCase("NestedEmbedded")]
        [TestCase("SameVariableInTwoStatements")]
        [TestCase("WrapInsideWrap")]
        [TestCase("InsertInsideWrap")]
        [TestCase("WrapInsideWrapWithElse")]
        [TestCase("BothBranchesWrapped")]
        public async Task HasFixAll(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UsePartialRecordsForLoopsCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestFixAll(
                currentCode,
                expectedCode,
                DiagnosticIds.UsePartialRecordsForLoops,
                codeFixIndex: 0,
                equivalenceKey: nameof(UsePartialRecordsForLoopsCodeFixProvider));
        }

        [Test]
        [TestCase("WrapAroundConditionalDirective")]
        [TestCase("WrapAroundInactiveConditionalDirective")]
        [TestCase("WrapAroundPragma")]
        [TestCase("InsertAfterConditionalDirective")]
        public async Task NoFix(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoFix), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UsePartialRecordsForLoopsCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.NoCodeFix(code, DiagnosticDescriptors.UsePartialRecordsForLoops);
        }
    }
}
