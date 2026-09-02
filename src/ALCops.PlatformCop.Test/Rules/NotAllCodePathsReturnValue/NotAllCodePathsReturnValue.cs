using RoslynTestKit;

namespace ALCops.PlatformCop.Test;

public class NotAllCodePathsReturnValue : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _fixture = RoslynFixtureFactory.Create<Analyzers.NotAllCodePathsReturnValue>();

        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
            Path.Combine("Rules", nameof(NotAllCodePathsReturnValue)));
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
    [TestCase("UnnamedCaseTrueWithoutElse")]
    public async Task HasDiagnostic(string testCase)
    {
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
    [TestCase("NamedIfElseErrorTerminates")]
    [TestCase("UnnamedCaseElseErrorTerminates")]
    [TestCase("UnnamedCaseElseExitTerminates")]
    [TestCase("UnnamedCaseTrueElseExitTerminates")]
    [TestCase("UnnamedGuardClauseErrorFirst")]
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
    public async Task NoDiagnostic(string testCase)
    {
        SkipTestIfVersionIsTooLow(
            ["NamedInitializedByIsolatedStorage"],
            testCase,
            "14.0",
            "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NotAllCodePathsReturnValue);
    }
}