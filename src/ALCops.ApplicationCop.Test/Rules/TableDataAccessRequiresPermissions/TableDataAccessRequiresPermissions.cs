using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class TableDataAccessRequiresPermissions : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.TableDataAccessRequiresPermissions _analyzer = new();
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
        [TestCase("ThisKeywordSelfCallInTable")]
        [TestCase("XmlPorts")]
        [TestCase("Queries")]
        [TestCase("Reports")]
        [TestCase("DottedTableName")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisKeywordSelfCallInTable"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

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
        [TestCase("DottedTableNameWithPermissions")]
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

        [Test]
        [TestCase("AddNewPermissionsProperty")]
        [TestCase("AddNewTableEntry")]
        [TestCase("MergePermissionChar")]
        [TestCase("MergeCanonicalOrder")]
        [TestCase("AddEntryMultiLine")]
        [TestCase("AddEntrySingleLine")]
        [TestCase("AddEntryAlphabetical")]
        [TestCase("AddEntryAlphabeticalFirst")]
        [TestCase("AddEntryAppend")]
        [TestCase("AddNewPermissionsPropertyDottedName")]
        [TestCase("MergePermissionCharDottedName")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<TableDataAccessRequiresPermissionsCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.TableDataAccessRequiresPermissions);
        }
    }
}