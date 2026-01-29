using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class OptionTypeShouldBeEnum : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.OptionTypeShouldBeEnum>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(OptionTypeShouldBeEnum)));
        }

        [Test]
        [TestCase("OptionField")]
        [TestCase("OptionParameterGlobalVar")]
        [TestCase("OptionParameterLocalVar")]
        [TestCase("OptionReturnValue")]
        [TestCase("OptionVariable")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.OptionTypeShouldBeEnum);
        }

        [Test]
        [TestCase("CDSDocument")]
        [TestCase("EventSubscriberOption")]
        [TestCase("FlowField")]
        [TestCase("ObsoleteFieldOption")]
        // [TestCase("OptionParameter")] //TODO: See remarks in the test file
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.OptionTypeShouldBeEnum);
        }
    }
}