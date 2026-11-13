using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TryFunctionDatabaseWrite : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private AnalyzerTestFixture _potentialFixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                "Rules", nameof(TryFunctionDatabaseWrite));

            _fixture = RoslynFixtureFactory.Create<Analyzers.TryFunctionDatabaseWrite>();
            _potentialFixture = RoslynFixtureFactory.Create<Analyzers.TryFunctionDatabaseWrite>(
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(TryFunctionDatabaseWrite)}.ruleset.json")
                });
        }

        [Test]
        [TestCase("DirectDatabaseModifications")]
        [TestCase("WithoutParentheses")]
        [TestCase("ImplicitRec")]
        public async Task HasDatabaseWrite(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["DirectDatabaseModifications"],
                testCase,
                "16.0",
                "Record.Truncate is not available before AL version 16.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasDiagnostic", $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionDatabaseWrite);
        }

        [Test]
        [TestCase("PotentialDatabaseModifications")]
        public async Task HasPotentialModification(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasDiagnostic", $"{testCase}.al"))
                .ConfigureAwait(false);

            _potentialFixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionPotentialDatabaseWrite);
        }

        [Test]
        [TestCase("PotentialDatabaseModifications")]
        public async Task PotentialModificationIsDisabledByDefault(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "HasDiagnostic", $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionPotentialDatabaseWrite);
        }

        [Test]
        [TestCase("SafeMethods")]
        [TestCase("OutsideTryFunction")]
        [TestCase("ObsoleteTryFunction")]
        [TestCase("TemporaryRecord")]
        [TestCase("PageTemporarySourceTable")]
        [TestCase("NonRecordBuiltIns")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionDatabaseWrite);
            _potentialFixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TryFunctionPotentialDatabaseWrite);
        }
    }
}
