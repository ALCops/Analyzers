using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class ODataKeyFieldsShouldUseSystemId : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.ODataKeyFieldsShouldUseSystemId _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ODataKeyFieldsShouldUseSystemId>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ODataKeyFieldsShouldUseSystemId)));
        }

        [Test]
        [TestCase("ODataKeyFields")]
        [TestCase("ODataKeyFieldsIsSystemRowVersion")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ODataKeyFieldsShouldUseSystemId);
        }

        [Test]
        [TestCase("ODataKeyFieldsIsSystemId")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ODataKeyFieldsShouldUseSystemId);
        }
    }
}