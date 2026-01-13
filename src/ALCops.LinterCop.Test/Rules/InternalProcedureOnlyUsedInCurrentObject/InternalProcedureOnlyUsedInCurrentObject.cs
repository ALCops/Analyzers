using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class InternalProcedureOnlyUsedInCurrentObject : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.InternalProcedureNotReferenced>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(InternalProcedureOnlyUsedInCurrentObject)));
        }

        //TODO: The ManifestHelper.GetManifest doesn't support NUnit at the moment is seems, so these tests are disabled for now.
        // [Test]
        // [TestCase("OnlyUsedInCurrentObject")]
        // [TestCase("OnlyUsedInCurrentObjectInternalToInternal")]
        // [TestCase("OnlyUsedInCurrentObjectMultipleCalls")]
        // [TestCase("OnlyUsedInCurrentObjectSelfVariable")]
        // [TestCase("OnlyUsedInCurrentObjectTrigger")]
        // public async Task HasDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureOnlyUsedInCurrentObject);
        // }

        // [Test]
        // [TestCase("PublicInInternalObject")]
        // [TestCase("TestAttribute")]
        // [TestCase("UsedInBoth")]
        // [TestCase("UsedInOtherObjectOnly")]
        // public async Task NoDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureOnlyUsedInCurrentObject);
        // }
    }
}