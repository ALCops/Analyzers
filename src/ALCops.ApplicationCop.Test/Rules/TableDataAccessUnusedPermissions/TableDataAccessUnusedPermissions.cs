using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class TableDataAccessUnusedPermissions : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.TableDataAccessUnusedPermissions _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TableDataAccessUnusedPermissions>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TableDataAccessUnusedPermissions)));
        }

        [Test]
        [TestCase("EntireEntryUnused")]
        [TestCase("PartialCharsUnused")]
        [TestCase("MultipleUnusedEntries")]
        [TestCase("NoCodeInCodeunit")]
        [TestCase("UnusedOnReport")]
        [TestCase("UnusedOnQuery")]
        [TestCase("UnusedOnXmlPort")]
        [TestCase("TemporaryRecord")]
        [TestCase("ParameterPartialUnused")]
        [TestCase("ReportDataItemPartialUnused")]
        [TestCase("ThisKeywordPartialUnused")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisKeywordPartialUnused"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataAccessUnusedPermissions);
        }

        [Test]
        [TestCase("AllPermissionsUsed")]
        [TestCase("PageSourceTable")]
        [TestCase("TestCodeunitDisabled")]
        [TestCase("ReadUsed")]
        [TestCase("ReportDataItemRead")]
        [TestCase("QueryDataItemRead")]
        [TestCase("PermissionSet")]
        [TestCase("PermissionSetExtension")]
        [TestCase("SystemTable")]
        [TestCase("ParameterOperations")]
        [TestCase("UppercasePermissions")]
        [TestCase("ParameterAllOperations")]
        [TestCase("LocalVarSpacedTable")]
        [TestCase("GlobalVarSpacedTable")]
        [TestCase("ReportDataItemModify")]
        [TestCase("ReportDataItemAliasModify")]
        [TestCase("XmlPortTableElementModify")]
        [TestCase("XmlPortNestedTableElementModify")]
        [TestCase("ReturnParameterRead")]
        [TestCase("ReportNestedDataItemRead")]
        [TestCase("QueryNestedDataItemRead")]
        [TestCase("MethodWithoutParenthesesCount")]
        [TestCase("MethodWithoutParenthesesFindFirst")]
        [TestCase("MethodWithoutParenthesesIsEmpty")]
        [TestCase("MethodWithoutParenthesesChained")]
        [TestCase("ThisKeywordSelfAccess")]
        [TestCase("ImplicitSelfBareCall")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["PermissionSetExtension"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ["ThisKeywordSelfAccess"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");


            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TableDataAccessUnusedPermissions);
        }

        [Test]
        [TestCase("RemoveEntireEntry")]
        [TestCase("RemoveEntireProperty")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<TableDataAccessUnusedPermissionsCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.TableDataAccessUnusedPermissionsEntireEntry);
        }

        [Test]
        [TestCase("ReduceChars")]
        [TestCase("ReplaceChars")]
        public async Task HasFixPartialChars(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<TableDataAccessUnusedPermissionsCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.TableDataAccessUnusedPermissionsPartialChars);
        }
    }
}
