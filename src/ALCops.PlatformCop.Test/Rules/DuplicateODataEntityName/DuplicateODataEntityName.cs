using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class DuplicateODataEntityName : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.DuplicateODataEntityName>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(DuplicateODataEntityName)));
        }

        [Test]
        [TestCase("DotRemoval")]
        [TestCase("PercentSign")]
        [TestCase("ParenthesisRemoval")]
        [TestCase("SlashToUnderscore")]
        [TestCase("PageExtensionCollision")]
        [TestCase("PrimaryKeyCollision")]
        [TestCase("ThreeWayCollision")]
        [TestCase("MultiplePageExtensionCollision")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["PageExtensionCollision", "MultiplePageExtensionCollision"],
                testCase,
                "13.0",
                "No support for pageextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.DuplicateODataEntityName);
        }

        [Test]
        [TestCase("UniqueNames")]
        [TestCase("ApiPage")]
        [TestCase("RoleCenterPage")]
        [TestCase("ObsoletePage")]
        [TestCase("PageExtensionUniqueNames")]
        [TestCase("PrimaryKeyFieldOnPage")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["PageExtensionUniqueNames"],
                testCase,
                "13.0",
                "No support for pageextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.DuplicateODataEntityName);
        }
    }
}
