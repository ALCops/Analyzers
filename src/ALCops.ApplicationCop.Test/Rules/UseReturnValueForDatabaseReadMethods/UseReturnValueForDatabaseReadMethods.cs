using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class UseReturnValueForDatabaseReadMethods : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UseReturnValueForDatabaseReadMethods>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(UseReturnValueForDatabaseReadMethods)));
        }

        [Test]
        [TestCase("GetMethod")]
        [TestCase("GetBySystemIdMethod")]
        [TestCase("GetMethodWithoutParentheses")]
        [TestCase("FindFirstMethodWithoutParentheses")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseReturnValueForDatabaseReadMethods);
        }

        [Test]
        [TestCase("GetMethod")]
        [TestCase("GetBySystemIdMethod")]
        [TestCase("GetMethodWithoutParentheses")]
        [TestCase("FindFirstMethodWithoutParentheses")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseReturnValueForDatabaseReadMethods);
        }
    }
}