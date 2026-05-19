using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class PageVariableSetRecordTemporaryRecord : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.PageVariableSetRecordTemporaryRecord>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(PageVariableSetRecordTemporaryRecord)));
        }

        [Test]
        [TestCase("TempVarSetRecord")]
        [TestCase("TempTableTypeSetRecord")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PageVariableSetRecordTemporaryRecord);
        }

        [Test]
        [TestCase("NonTempVarSetRecord")]
        [TestCase("TempVarGetRecord")]
        [TestCase("TempTableTypeWithoutKeyword")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PageVariableSetRecordTemporaryRecord);
        }
    }
}
