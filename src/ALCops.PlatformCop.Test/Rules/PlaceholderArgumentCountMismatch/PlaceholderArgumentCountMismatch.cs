using RoslynTestKit;

namespace ALCops.PlatformCop.Test;

public class PlaceholderArgumentCountMismatch : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _fixture = RoslynFixtureFactory.Create<Analyzers.PlaceholderArgumentCountMismatch>();

        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                Path.Combine("Rules", nameof(PlaceholderArgumentCountMismatch)));
    }

    [Test]
    [TestCase("StrSubstNoMissingArgs")]
    [TestCase("ErrorMissingArgs")]
    [TestCase("MessageMissingArgs")]
    [TestCase("ConfirmMissingArgs")]
    [TestCase("ConfirmTooManyArgs")]
    [TestCase("MultiplePlaceholdersMissingArgs")]
    [TestCase("StringLiteralMissingArgs")]
    public async Task HasDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PlaceholderArgumentCountMismatch);
    }

    [Test]
    [TestCase("StrSubstNoCorrectArgs")]
    [TestCase("ErrorNoPlaceholders")]
    [TestCase("ConfirmCorrectArgs")]
    [TestCase("TextVariable")]
    [TestCase("EmptyLabel")]
    [TestCase("StrSubstNoTooManyArgs")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PlaceholderArgumentCountMismatch);
    }
}
