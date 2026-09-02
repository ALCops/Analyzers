using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using RoslynTestKit;

namespace ALCops.LinterCop.Test;

public class GlobalVariableCouldBeLocal : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
            Path.Combine("Rules", nameof(GlobalVariableCouldBeLocal)));

        _fixture = RoslynFixtureFactory.Create<Analyzers.GlobalVariableCouldBeLocal>(
            new AnalyzerTestFixtureConfig
            {
                RuleSetPath = Path.Combine(_testCasePath, $"{nameof(GlobalVariableCouldBeLocal)}.ruleset.json")
            });
    }

    [Test]
    [TestCase("Case01UnconditionalOverwrite")]
    [TestCase("Case02AllBranchesInitialize")]
    [TestCase("Case03RecordGetBeforeFieldRead")]
    [TestCase("Case04ClearBeforeConditionalRecordGet")]
    [TestCase("Case05ImmutableLabel")]
    [TestCase("Case06GuardExitThenInitialize")]
    [TestCase("ScalarClearBeforeRead")]
    [TestCase("CaseEveryBranchInitializes")]
    [TestCase("ErrorBranchDoesNotContinue")]
    [TestCase("ConditionalReadInsideInitializedBranch")]
    [TestCase("ClearThenCompoundAssignment")]
    [TestCase("RecordGetFailureGuard")]
    [TestCase("ByValueArgumentAfterAssignment")]
    [TestCase("CodeunitOnRunTrigger")]
    [TestCase("RepeatAssignsBeforeRead")]
    [TestCase("UnrelatedRecursionDoesNotSuppress")]
    [TestCase("ConditionalRecordGetReadOnSuccess")]
    [TestCase("RecordAssignmentInitializesFields")]
    [TestCase("ThisQualifiedInitialization")]
    [TestCase("RecursiveThenReinitialize")]
    public async Task HasDiagnostic(string testCase)
    {
        SkipTestIfVersionIsTooLow(
            ["ThisQualifiedInitialization"],
            testCase,
            "14.0",
            "Explicit this-qualified global access requires AL runtime 14.0 or higher.");

        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.GlobalVariableCouldBeLocal);
    }

    [Test]
    [TestCase("Case07CompoundAssignmentReadsPreviousState")]
    [TestCase("Case08ConditionalInitialization")]
    [TestCase("Case09PassedByReference")]
    [TestCase("Case10RecursiveProcedure")]
    [TestCase("UsedByMultipleProcedures")]
    [TestCase("ReadBeforeAssignment")]
    [TestCase("AssignmentReadsPreviousValue")]
    [TestCase("InitializedButPassedByVar")]
    [TestCase("RecordGetThenGetFilters")]
    [TestCase("RecordGetThenGetView")]
    [TestCase("RecordGetArgumentReadsPreviousField")]
    [TestCase("RecordGetThenFlowFilter")]
    [TestCase("RecordGetThenWholeRecordArgument")]
    [TestCase("PartialRecordFieldAssignment")]
    [TestCase("CaseWithoutElse")]
    [TestCase("WhileMayNotExecuteBeforeRead")]
    [TestCase("IndirectRecursion")]
    [TestCase("SingleInstanceCodeunit")]
    [TestCase("ManualEventSubscriberCodeunit")]
    [TestCase("ProtectedPageVariable")]
    [TestCase("PagePropertyReference")]
    [TestCase("ShadowedLocalDoesNotReferenceGlobal")]
    [TestCase("UnknownExternalCallMayReenter")]
    [TestCase("LabelUsedInMultipleProcedures")]
    [TestCase("NestedRecordGetResultIsObserved")]
    [TestCase("RecordGetWithoutKeyReadsState")]
    [TestCase("BreakMaySkipInitialization")]
    [TestCase("TemporaryRecordStatePersists")]
    [TestCase("ReferenceTypeStateMayEscape")]
    [TestCase("RecordContextChangesAfterRead")]
    [TestCase("RecordAssignmentCanAffectNextCall")]
    [TestCase("ArrayElementStateIsNotModeled")]
    [TestCase("IntegrationEventExposesGlobalVariables")]
    [TestCase("ClearAllTargetsObjectState")]
    [TestCase("CollectibleErrorDoesNotTerminateFlow")]
    [TestCase("RenameTriggerMayReenter")]
    [TestCase("TemporaryTableStatePersists")]
    [TestCase("TableResetCanClearGlobals")]
    [TestCase("TableExtensionObjectIsExcluded")]
    [TestCase("PageObjectIsExcluded")]
    [TestCase("TestCodeunitSubtypeIsExcluded")]
    [TestCase("IntegrationEventIncludesSender")]
    [TestCase("BusinessEventIncludesSender")]
    [TestCase("InternalEventIncludesSender")]
    [TestCase("BuiltInInvocationMayReenter")]
    [TestCase("RecordAssignmentDoesNotInitializeWholeRecord")]
    [TestCase("ThisQualifiedReadBeforeAssignment")]
    [TestCase("AssignmentTargetTraversal")]
    [TestCase("VarArgumentTraversal")]
    [TestCase("ContinueMaySkipInitialization")]
    [TestCase("AssignmentTargetBeforeValue")]
    public async Task NoDiagnostic(string testCase)
    {
        SkipTestIfVersionIsTooLow(
            ["TableExtensionObjectIsExcluded"],
            testCase,
            "13.0",
            "AL versions prior to 13.0 cannot extend a table declared in the same test module.");

        SkipTestIfVersionIsTooLow(
            ["ThisQualifiedReadBeforeAssignment"],
            testCase,
            "14.0",
            "Explicit this-qualified global access requires AL runtime 14.0 or higher.");

        SkipTestIfVersionIsTooLow(
            ["ContinueMaySkipInitialization"],
            testCase,
            "15.0",
            "The continue statement requires AL runtime 15.0 or higher.");

        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.GlobalVariableCouldBeLocal);
    }

    [Test]
    public void DiagnosticMessageIncludesVariableAndScopeNames()
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.GlobalVariableCouldBeLocal,
            Location.None,
            "MyGlobalVariable",
            "MyDummyProcedure");

        Assert.That(
            diagnostic.GetMessage(),
            Is.EqualTo(
                "Global variable 'MyGlobalVariable' is only used in 'MyDummyProcedure' and appears to be reinitialized before every read. Consider moving it to local scope."));
    }
}
