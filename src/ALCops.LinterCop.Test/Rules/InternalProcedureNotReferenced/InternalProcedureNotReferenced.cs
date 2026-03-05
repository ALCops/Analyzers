using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class InternalProcedureNotReferenced : NavCodeAnalysisBase
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
                    Path.Combine("Rules", nameof(InternalProcedureNotReferenced)));
        }
        //TODO: The ManifestHelper.GetManifest doesn't support NUnit at the moment is seems, so these tests are disabled for now.
        // [Test]
        // [TestCase("Codeunit_FieldNameCollision")]
        // [TestCase("Codeunit_NameCollisionVariable")]
        // [TestCase("Codeunit_SameNameDifferentObjects")]
        // [TestCase("Codeunit_UnusedInternal")]
        // [TestCase("Codeunit_UnusedInternalOverloads")]
        // [TestCase("InternalCodeunit_UnusedInternal")]
        // [TestCase("InternalCodeunit_UnusedPublic")]
        // [TestCase("PublicTestCodeunit_UnusedInternal")]
        // public async Task HasDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureNotReferenced);
        // }

        // [Test]
        // [TestCase("ErrorInfoParameter")]
        // [TestCase("ImplementsInterfaceMethod")]
        // [TestCase("IntegrationEvent")]
        // [TestCase("InternalTestCodeunit")]
        // [TestCase("MessageHandlerAttribute")]
        // [TestCase("NotificationParameter")]
        // [TestCase("Obsolete")]
        // [TestCase("PublicInPublicObject")]
        // [TestCase("TestAttribute")]
        // public async Task NoDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureNotReferenced);
        // }
    }
}