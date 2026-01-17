using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TemporaryRecordTriggerInvocation : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.TemporaryRecordTriggerInvocation _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TemporaryRecordTriggerInvocation>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TemporaryRecordTriggerInvocation)));
        }

        [Test]
        [TestCase("TempVar")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TemporaryRecordTriggerInvocation);
        }

        [Test]
        [TestCase("TempVarImplicit")]
        [TestCase("TempVarExplicit")]
        [TestCase("TempTable")]
        [TestCase("TempTableExplicitTemp")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TemporaryRecordTriggerInvocation);
        }
    }
}