using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class InternalProcedureNotReferenced : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        private static readonly byte[] AppJsonWithInternalsVisibleTo = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "id": "00000000-0000-0000-0000-000000000042",
                "name": "TestApp",
                "publisher": "TestPublisher",
                "version": "1.0.0.0",
                "idRanges": [ { "from": 50000, "to": 99999 } ],
                "internalsVisibleTo": [
                    {
                        "id": "00000000-0000-0000-0000-000000000043",
                        "name": "TestApp.Test",
                        "publisher": "TestPublisher"
                    }
                ]
            }
            """);

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.InternalProcedureNotReferenced>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(InternalProcedureNotReferenced));
        }

        private static AnalyzerTestFixture CreateFixtureWithInternalsVisibleTo()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "app.json", AppJsonWithInternalsVisibleTo }
            };

            return RoslynFixtureFactory.Create<Analyzers.InternalProcedureNotReferenced>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = new MemoryFileSystem(files)
                });
        }

        [Test]
        [TestCase("Codeunit_FieldNameCollision")]
        [TestCase("Codeunit_NameCollisionVariable")]
        [TestCase("Codeunit_SameNameDifferentObjects")]
        [TestCase("Codeunit_UnusedInternal")]
        [TestCase("Codeunit_UnusedInternalOverloads")]
        [TestCase("InternalCodeunit_UnusedInternal")]
        [TestCase("InternalCodeunit_UnusedPublic")]
        [TestCase("PublicTestCodeunit_UnusedInternal")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureNotReferenced);
        }

        [Test]
        [TestCase("ErrorInfoParameter")]
        [TestCase("ImplementsInterfaceMethod")]
        // [TestCase("IntegrationEvent")] // Disabled: the analyzer does not exclude event publishers, so it reports LC0052 here.
        [TestCase("InternalTestCodeunit")]
        [TestCase("MessageHandlerAttribute")]
        [TestCase("NotificationParameter")]
        [TestCase("Obsolete")]
        [TestCase("PublicInPublicObject")]
        [TestCase("TestAttribute")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooHigh(
                ["ImplementsInterfaceMethod"],
                testCase,
                "30.0",
                "From SDK 30 the default runtime is Spring2027, where an internal procedure implementing a public interface method is compile error AL0921 instead of a warning.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureNotReferenced);
        }

        [Test]
        [TestCase("InternalsVisibleTo")]
        public async Task NoDiagnosticWithInternalsVisibleTo(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateFixtureWithInternalsVisibleTo().NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureNotReferenced);
        }
    }
}