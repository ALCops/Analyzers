using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class ExplicitlySetRunTrigger : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ExplicitlySetRunTrigger>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ExplicitlySetRunTrigger)));
        }

        [Test]
        [TestCase("Delete")]
        [TestCase("DeleteAll")]
        [TestCase("Insert")]
        [TestCase("Modify")]
        [TestCase("ModifyAll")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ExplicitlySetRunTrigger);
        }

        [Test]
        [TestCase("DeleteAllRunTriggerFalse")]
        [TestCase("DeleteAllRunTriggerTrue")]
        [TestCase("DeleteRunTriggerFalse")]
        [TestCase("DeleteRunTriggerTrue")]
        [TestCase("InsertRunTriggerFalse")]
        [TestCase("InsertRunTriggerTrue")]
        [TestCase("ModifyAllRunTriggerFalse")]
        [TestCase("ModifyAllRunTriggerTrue")]
        [TestCase("ModifyRunTriggerFalse")]
        [TestCase("ModifyRunTriggerTrue")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ExplicitlySetRunTrigger);
        }
    }
}