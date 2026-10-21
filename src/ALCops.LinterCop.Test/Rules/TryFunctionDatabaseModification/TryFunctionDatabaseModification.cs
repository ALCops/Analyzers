using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class TryFunctionDatabaseModification : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TryFunctionDatabaseModification>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                Path.Combine("Rules", nameof(TryFunctionDatabaseModification)));
        }

        [Test]
        [TestCase("DirectDatabaseModifications")]
        public async Task HasDisallowedModification(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasDiagnostic", $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionDatabaseModificationDisallowed);
        }

        [Test]
        [TestCase("PotentialDatabaseModifications")]
        public async Task HasPotentialModification(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasDiagnostic", $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionPotentialDatabaseModification);
        }

        [Test]
        [TestCase("SafeMethods")]
        [TestCase("OutsideTryFunction")]
        [TestCase("ObsoleteTryFunction")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionDatabaseModificationDisallowed);
            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionPotentialDatabaseModification);
        }
    }
}
