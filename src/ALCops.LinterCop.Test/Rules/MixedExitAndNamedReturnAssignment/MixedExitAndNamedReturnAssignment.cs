using RoslynTestKit;

namespace ALCops.LinterCop.Test;

public class MixedExitAndNamedReturnAssignment : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
            Path.Combine("Rules", nameof(MixedExitAndNamedReturnAssignment)));

        _fixture = RoslynFixtureFactory.Create<Analyzers.MixedExitAndNamedReturnAssignment>(
            new AnalyzerTestFixtureConfig
            {
                RuleSetPath = Path.Combine(_testCasePath, $"{nameof(MixedExitAndNamedReturnAssignment)}.ruleset.json")
            });
    }

    [Test]
    [TestCase("NamedSimpleAssignmentAndExitCases")]
    [TestCase("NamedBranchAssignmentAndExitCases")]
    public async Task HasDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.MixedExitAndNamedReturnAssignment);
    }

    [Test]
    [TestCase("NamedOnlyAssignment")]
    [TestCase("NamedOnlyExit")]
    [TestCase("UnnamedExitAndLocalAssignment")]
    [TestCase("TryFunctionExcluded")]
    [TestCase("NamedCaseOnlyAssignments")]
    [TestCase("NamedIfElseIfElseOnlyExit")]
    [TestCase("NamedNestedIfElseIfOnlyAssignments")]
    [TestCase("TriggerOnlyExitCases")]
    [TestCase("NamedFieldSameNameOnlyExit")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.MixedExitAndNamedReturnAssignment);
    }
}