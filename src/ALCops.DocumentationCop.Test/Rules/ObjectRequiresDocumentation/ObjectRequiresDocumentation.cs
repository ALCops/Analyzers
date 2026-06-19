using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class ObjectRequiresDocumentation : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ObjectRequiresDocumentation)));

		    _fixture = RoslynFixtureFactory.Create<Analyzers.ObjectRequiresDocumentation>(
				// Inject a ruleset to enable testing for rules, that are not enabled by default (isEnabledByDefault: false).
				new AnalyzerTestFixtureConfig
				{
					RuleSetPath = Path.Combine(_testCasePath, $"{nameof(ObjectRequiresDocumentation)}.ruleset.json")
				});
        }

        [Test]
        [TestCase("PublicCodeunit")]
        public async Task PublicHasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicHasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PublicObjectRequiresDocumentation);
        }

        [Test]
        [TestCase("PublicCodeunit")]
        public async Task PublicNoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicNoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PublicObjectRequiresDocumentation);
        }
 
         [Test]
        [TestCase("InternalCodeunit")]
        public async Task InternalHasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalHasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalObjectRequiresDocumentation);
        }

         [Test]
        [TestCase("InternalCodeunit")]
        public async Task InternalNoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalNoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalObjectRequiresDocumentation);
        }
  }
}
