using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class TableDataAccessRequiresPermissions : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TableDataAccessRequiresPermissions>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TableDataAccessRequiresPermissions)));
        }

        [Test]
        [TestCase("ProcedureCalls")]
        [TestCase("ProcedureCallsExtended")]
        [TestCase("GetBySystemId")]
        [TestCase("Count")]
        [TestCase("ImplicitSelfCallInTable")]
        [TestCase("XmlPorts")]
        [TestCase("Queries")]
        [TestCase("Reports")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataAccessRequiresPermissions);
        }

        [Test]
        [TestCase("ProcedureCallsPermissionsProperty")]
        [TestCase("XmlPortPermissionsProperty")]
        [TestCase("QueryPermissionsProperty")]
        [TestCase("XmlPortInherentPermissions")]
        [TestCase("QueryInherentPermissions")]
        [TestCase("ReportPermissionsProperty")]
        [TestCase("ReportInherentPermissions")]
        [TestCase("ProcedureCallsInherentPermissionsProperty")]
        [TestCase("ProcedureCallsInherentPermissionsAttribute")]
        [TestCase("PageSourceTable")]
        [TestCase("PageExtensionSourceTable")]
        [TestCase("ProcedureCallsPermissionsPropertyFullyQualified")]
        // [TestCase("IntegerTable")]
        [TestCase("XMLPortWithTableElementProps")]
        [TestCase("PermissionsAsObjectId")]
        [TestCase("PermissionPropertyWithPragma")]
        [TestCase("PermissionPropertyWithComment")]
        [TestCase("MultiplePermissionsDifferentType")]
        [TestCase("TestPermissionsDisabled")]
        [TestCase("GetBySystemIdWithPermissions")]
        [TestCase("CountWithPermissions")]
        [TestCase("ImplicitSelfCallWithInherentPermissions")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["PageExtensionSourceTable"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataAccessRequiresPermissions);
        }
    }
}