using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class ConfirmImplementConfirmManagement : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.BuiltInMethodImplementThroughCodeunit _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.BuiltInMethodImplementThroughCodeunit>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ConfirmImplementConfirmManagement)));
        }

        [Test]
        [TestCase("Confirm")]
        [TestCase("DialogConfirm")]
        [TestCase("PageOfTypeAPI")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ConfirmImplementConfirmManagement);
        }

        [Test]
        [TestCase("ObsoletePending")]
        [TestCase("Page")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ConfirmImplementConfirmManagement);
        }
    }
}