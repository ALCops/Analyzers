using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test;

public class UseSetAutoCalcFieldsForLoops : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private static readonly Analyzers.UseSetAutoCalcFieldsForLoops _analyzer = new();
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _fixture = RoslynFixtureFactory.Create<Analyzers.UseSetAutoCalcFieldsForLoops>();

        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                Path.Combine("Rules", nameof(UseSetAutoCalcFieldsForLoops)));
    }

    [Test]
    [TestCase("FindSetRepeatUntil")]
    [TestCase("FindRepeatUntil")]
    [TestCase("WhileLoop")]
    [TestCase("ReportOnAfterGetRecord")]
    [TestCase("MultipleCalcFields")]
    [TestCase("NestedLoop")]
    [TestCase("NestedLoopInConditional")]
    public async Task HasDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseSetAutoCalcFieldsForLoops);
    }

    [Test]
    [TestCase("DifferentVariable")]
    [TestCase("CalcFieldsOutsideLoop")]
    [TestCase("CrossMethodCall")]
    [TestCase("SingleRecord")]
    [TestCase("CalcFieldsInIfBlock")]
    [TestCase("CalcFieldsInCaseBlock")]
    [TestCase("CalcFieldsInIfElseBlock")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseSetAutoCalcFieldsForLoops);
    }

    [Test]
    [TestCase("FindSetRepeatUntil")]
    [TestCase("MultipleFields")]
    [TestCase("IfFindSetRepeatUntil")]
    [TestCase("IfFindSetBeginRepeatUntil")]
    public async Task HasFix(string testCase)
    {
        var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
            .ConfigureAwait(false);

        var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
            .ConfigureAwait(false);

        var fixture = RoslynFixtureFactory.Create<UseSetAutoCalcFieldsForLoopsCodeFixProvider>(
            new CodeFixTestFixtureConfig
            {
                AdditionalAnalyzers = [_analyzer]
            });

        fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UseSetAutoCalcFieldsForLoops);
    }
}
