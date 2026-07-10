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
    [TestCase("UnnamedGuardClauseErrorFirst")]
    [TestCase("NamedInitializedByVarArgument")]
    [TestCase("NamedInitializedByReceiverCall")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NotAllCodePathsReturnValue);
    }
}