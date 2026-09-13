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
    public async Task HasDiagnostic(string testCase)
    {
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
    [TestCase("ObsoleteTestMethod")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InvokeActionOnPartTestPage);
    }
}
