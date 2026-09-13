using RoslynTestKit;

namespace ALCops.TestAutomationCop.Test;

public class InvokeActionOnPartTestPage : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _fixture = RoslynFixtureFactory.Create<Analyzers.InvokeActionOnPartTestPage>();

        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                Path.Combine("Rules", nameof(InvokeActionOnPartTestPage)));
    }

    [Test]
    [TestCase("ListPartLocalVariable")]
    [TestCase("CardPartLocalVariable")]
    [TestCase("ListPartGlobalVariable")]
    [TestCase("ListPartVarParameter")]
    [TestCase("ListPartActionFromPageExtension")]
    [TestCase("ListPartEnabledAndVisible")]
    [TestCase("ListPartInvokeWithoutParentheses")]
    [TestCase("ObsoletePartPage")]
    [TestCase("NamespacedListPart")]
    [TestCase("ObsoleteTestMethod")]
    public async Task HasDiagnostic(string testCase)
    {
        SkipTestIfVersionIsTooLow(
            ["ListPartActionFromPageExtension"],
            testCase,
            "13.0",
            "No support for pageextensions when target itself is already declared in the same module");

        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InvokeActionOnPartTestPage);
    }

    [Test]
    [TestCase("ListPage")]
    [TestCase("CardPage")]
    [TestCase("PageWithoutPageTypeProperty")]
    [TestCase("HeadlinePart")]
    [TestCase("ActionThroughPartControl")]
    [TestCase("FieldAccessOnPart")]
    [TestCase("OpenViewOnPart")]
    [TestCase("BuiltInOkOnPart")]
    [TestCase("TestRequestPage")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InvokeActionOnPartTestPage);
    }
}
