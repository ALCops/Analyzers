using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
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
    [TestCase("BooleanVariable")]
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
    [TestCase(
        "BooleanVariable",
        "Global 'Boolean' variable 'MyTestVariable' is only used in 'MyTestProcedure' and appears to be reinitialized before every read. Consider moving it to local scope.")]
    [TestCase(
        "Case05ImmutableLabel",
        "Global 'Label' variable 'MyProcedureOnlyLabel' is only used in 'ShowCustomerName'. Consider moving it to local scope.")]
    [TestCase(
        "Case03RecordGetBeforeFieldRead",
        "Global 'Record Customer' variable 'MyCustomerFromGet' is only used in 'ShowCustomerFromOriginalPost' and appears to be reinitialized before every read. Consider moving it to local scope.")]
    public async Task DiagnosticMessageIncludesTypeAndUsesStateAppropriateWording(
        string testCase,
        string expectedMessage)
    {
        var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        var fixture = new DiagnosticMessageFixture(
            Path.Combine(_testCasePath, $"{nameof(GlobalVariableCouldBeLocal)}.ruleset.json"));

        Assert.That(
            fixture.GetDiagnosticMessages(code, DiagnosticIds.GlobalVariableCouldBeLocal),
            Is.EqualTo(new[] { expectedMessage }));
    }

    private sealed class DiagnosticMessageFixture(string ruleSetPath) : AnalyzerTestFixture
    {
        protected override string LanguageName => LanguageNames.AL;
        protected override string? RuleSetPath => ruleSetPath;

        protected override DiagnosticAnalyzer CreateAnalyzer() =>
            new Analyzers.GlobalVariableCouldBeLocal();

        public string[] GetDiagnosticMessages(string markupCode, string diagnosticId)
        {
            var markup = new CodeMarkup(markupCode);
            var document = CreateDocumentFromCode(markup.Code);
            var compilation = document.Project
                .GetCompilationAsync(CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            if (compilation is null)
            {
                return [];
            }

            return compilation
                .WithAnalyzers(
                    ImmutableArray.Create(CreateAnalyzer()),
                    cancellationToken: CancellationToken.None)
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult()
                .Where(diagnostic => diagnostic.Id == diagnosticId)
                .Select(diagnostic => diagnostic.GetMessage())
                .ToArray();
        }
    }
}
