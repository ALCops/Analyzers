using RoslynTestKit;

namespace ALCops.PlatformCop.Test;

public class NotAllCodePathsReturnValue : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private AnalyzerTestFixture _errorTolerantFixture;
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _fixture = RoslynFixtureFactory.Create<Analyzers.NotAllCodePathsReturnValue>();

        _errorTolerantFixture = RoslynFixtureFactory.Create<Analyzers.NotAllCodePathsReturnValue>(
            new AnalyzerTestFixtureConfig
            {
                ThrowsWhenInputDocumentContainsError = false
            });

        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
            "Rules", nameof(NotAllCodePathsReturnValue));
    }

    [Test]
    [TestCase("UnnamedNoExit")]
    [TestCase("UnnamedIfWithoutElse")]
    [TestCase("NamedAssignedOnlyInIf")]
    [TestCase("NamedLoopMayNotAssign")]
    [TestCase("UnnamedCaseWithoutElse")]
    [TestCase("UnnamedIfElseIfElseMissingReturn")]
    [TestCase("NamedNestedIfElseIfMissingAssignment")]
    [TestCase("NamedPassedAsByValueArgument")]
    [TestCase("NamedNotAssignedFieldSameName")]
    [TestCase("NamedIfConditionShortCircuit")]
    [TestCase("NamedWhileConditionShortCircuit")]
    [TestCase("NamedAssignedInIncompleteEnumCase")]
    [TestCase("NamedAssignedInExtensibleEnumCase")]
    [TestCase("NamedRepeatUntilBreakSkipsCondition")]
    [TestCase("UnnamedCaseTrueWithoutElse")]
    [TestCase("UnnamedUserDefinedFieldErrorNotTerminating")]
    [TestCase("NamedRecordEmptyBody")]
    [TestCase("NamedRecordFieldAssignedOnlyInIf")]
    [TestCase("NamedRecordOtherFieldAssigned")]
    [TestCase("NamedRecordFieldReadOnly")]
    [TestCase("NamedRecordFieldInvocationOnlyInIf")]
    [TestCase("NamedRecordOtherFieldInvocations")]
    [TestCase("NamedRecordFieldPassedByValue")]
    public async Task HasDiagnostic(string testCase)
    {
        SkipTestIfVersionIsTooLow(
            ["NamedAssignedInExtensibleEnumCase"],
            testCase,
            "13.0",
            "Extending an enum declared in the same module requires runtime version 13.0.");

        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NotAllCodePathsReturnValue);
    }

    [Test]
    [TestCase("UnnamedImmediateExit")]
    [TestCase("UnnamedIfElseBothExit")]
    [TestCase("NamedDirectAssignment")]
    [TestCase("NamedAssignmentInBothBranches")]
    [TestCase("NamedAssignedBeforeConditional")]
    [TestCase("TryFunctionExcluded")]
    [TestCase("NamedCaseAllBranchesAssigned")]
    [TestCase("UnnamedIfElseIfElseAllReturn")]
    [TestCase("NamedNestedIfElseIfAssigned")]
    [TestCase("TriggerCases")]
    [TestCase("UnnamedIfElseErrorTerminates")]
    [TestCase("UnnamedIfElseFieldErrorTerminates")]
    [TestCase("NamedIfElseErrorTerminates")]
    [TestCase("NamedIfElseFieldErrorTerminates")]
    [TestCase("UnnamedCaseElseErrorTerminates")]
    [TestCase("UnnamedCaseElseFieldErrorTerminates")]
    [TestCase("UnnamedCaseElseExitTerminates")]
    [TestCase("UnnamedCaseTrueElseExitTerminates")]
    [TestCase("UnnamedGuardClauseErrorFirst")]
    [TestCase("UnnamedGuardClauseFieldErrorFirst")]
    [TestCase("UnnamedIfElseFieldRefFieldErrorTerminates")]
    [TestCase("NamedInitializedByVarArgument")]
    [TestCase("NamedInitializedByVarArgumentInCondition")]
    [TestCase("NamedIfConditionGuaranteedLeft")]
    [TestCase("NamedInitializedByReceiverCall")]
    [TestCase("NamedInitializedByJsonObjectGet")]
    [TestCase("NamedInitializedByIsolatedStorage")]
    [TestCase("NamedInitializedByCaseSelector")]
    [TestCase("NamedInitializedByWhileCondition")]
    [TestCase("NamedWhileConditionGuaranteedLeft")]
    [TestCase("NamedInitializedByRepeatUntilCondition")]
    [TestCase("NamedInitializedByForBounds")]
    [TestCase("NamedInitializedByForEachCollection")]
    [TestCase("NamedAssignedInExhaustiveTextEncodingCase")]
    [TestCase("NamedIfConditionParenthesizedGuaranteedLeft")]
    [TestCase("NamedInitializedByTernaryCondition")]
    [TestCase("NamedRecordFieldAssignment")]
    [TestCase("NamedRecordFieldAssignedAndInserted")]
    [TestCase("NamedRecordFieldAssignedInBothBranches")]
    [TestCase("NamedTemporaryRecordPassedAsVarArgument")]
    [TestCase("NamedTemporaryRecordPassedToNonTemporaryVarArgument")]
    [TestCase("NamedRecordFilteredOnly")]
    [TestCase("NamedRecordClearedThenFieldAssigned")]
    [TestCase("NamedRecordClearedOnly")]
    [TestCase("NamedRecordInitOnly")]
    [TestCase("NamedRecordViewConfiguredOnly")]
    [TestCase("NamedRecordReadIsolationAssignment")]
    [TestCase("NamedRecordFieldInitializedByVarArgument")]
    [TestCase("NamedRecordFieldInitializedByReceiver")]
    public async Task NoDiagnostic(string testCase)
    {
        SkipTestIfVersionIsTooLow(
            ["NamedInitializedByIsolatedStorage"],
            testCase,
            "14.0",
            "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

        SkipTestIfVersionIsTooLow(
            ["NamedInitializedByTernaryCondition"],
            testCase,
            "14.0",
            "The ternary conditional expression requires runtime version 14.0.");

        SkipTestIfVersionIsTooLow(
            ["NamedRecordReadIsolationAssignment"],
            testCase,
            "11.0",
            "The ReadIsolation record property requires runtime version 11.0.");

        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NotAllCodePathsReturnValue);
        _fixture.NoException(code.Replace("[|", "").Replace("|]", ""));
    }

    [Test]
    [TestCase("UnnamedIfElseErrorUnboundArgumentTerminates")]
    [TestCase("UnnamedIfElseFieldErrorUnboundArgumentTerminates")]
    [TestCase("UnnamedIfElseFieldRefFieldErrorUnboundArgumentTerminates")]
    public async Task NoDiagnosticInDocumentWithErrors(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _errorTolerantFixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NotAllCodePathsReturnValue);
    }

    [Test]
    [TestCase("UnnamedUserDefinedErrorUnboundArgumentNotTerminating")]
    public async Task HasDiagnosticInDocumentWithErrors(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _errorTolerantFixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NotAllCodePathsReturnValue);
    }
}