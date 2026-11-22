using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class UseSequentialGuid : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.UseSequentialGuid _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UseSequentialGuid>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(UseSequentialGuid));
        }

        [Test]
        [TestCase("DirectAssignmentToPrimaryKey")]
        [TestCase("ValidatePrimaryKeyField")]
        [TestCase("DirectAssignmentToSecondaryKeyField")]
        [TestCase("VariableAssignedToKeyField")]
        [TestCase("CrossProcedureIntraModule")]
        [TestCase("OnInsertTrigger")]
        [TestCase("QualifiedCreateGuid")]
        [TestCase("MultiLevelTracing")]
        [TestCase("ValidateSecondaryKeyField")]
        [TestCase("DatabaseObjectReference")]
        [TestCase("DirectAssignmentBareSelf")]
        [TestCase("DirectAssignmentThisSelf")]
        [TestCase("ValidateBareSelf")]
        [TestCase("ValidateThisSelf")]
        [TestCase("GlobalVariableAssignedInSiblingProcedure")]
        [TestCase("GlobalVariableValidateInSiblingProcedure")]
        [TestCase("GlobalVariableBareSelfInTable")]
        [TestCase("GlobalVariableSameProcedure")]
        [TestCase("ParenthesizedCreateGuid")]
        [TestCase("AssignmentBareSelfInTrigger")]
        [TestCase("ValidateBareSelfInTrigger")]
        [TestCase("ImplicitPrimaryKeyNamedVariable")]
        [TestCase("ImplicitPrimaryKeyRecSelf")]
        [TestCase("ImplicitPrimaryKeyValidate")]
        [TestCase("NamespacedQualifiedRecordVariable")]
        [TestCase("NamespacedExplicitKeys")]
        [TestCase("OnRunRecSelf")]
        [TestCase("OnRunRecSelfExplicitKeys")]
        [TestCase("OnRunBareSelf")]
        [TestCase("OnRunValidateRecSelf")]
        [TestCase("AssignmentBareSelfInTableExtension")]
        [TestCase("AssignmentRecSelfInTableExtension")]
        [TestCase("AssignmentThisSelfInTableExtension")]
        [TestCase("ValidateBareSelfInTableExtension")]
        [TestCase("PageRecSelf")]
        [TestCase("PageBareSelf")]
        [TestCase("PageExtensionRecSelf")]
        [TestCase("RequestPageRecSelf")]
        [TestCase("ReportRecSelfViaRequestPage")]
        [TestCase("XmlPortRecSelfViaRequestPage")]
        [TestCase("ReportDataItemNamed")]
        [TestCase("ReportDataItemBareSelf")]
        [TestCase("TableExtensionKeyOnBaseGuidField")]
        public async Task HasDiagnostic(string testCase)
        {
            // https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/guid/guid-createsequentialguid-method
            RequireMinimumVersion("16.0", "Available with runtime version 16.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseSequentialGuid);
        }

        // A tableextension key may combine a base-table field with an extension field.
        [Test]
        [TestCase("TableExtensionMixedOriginKey")]
        public async Task HasDiagnosticMixedOriginKey(string testCase)
        {
            RequireMinimumVersion("18.0", "Mixed-origin keys in tableextensions require runtime 18.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseSequentialGuid);
        }

        // The base table lives in a second workspace project; the tableextension that declares the key
        // and the CreateGuid() call live in the test project.
        [Test]
        [TestCase("TableExtensionKeyOnReferencedTable")]
        public async Task HasDiagnosticWithDependencyProject(string testCase)
        {
            RequireMinimumVersion("16.0", "Available with runtime version 16.0.");

            await AssertHasDiagnosticWithDependencyProject(testCase).ConfigureAwait(false);
        }

        [Test]
        [TestCase("TableExtensionMixedOriginKeyOnReferencedTable")]
        public async Task HasDiagnosticWithDependencyProjectMixedOriginKey(string testCase)
        {
            RequireMinimumVersion("18.0", "Mixed-origin keys in tableextensions require runtime 18.0.");

            await AssertHasDiagnosticWithDependencyProject(testCase).ConfigureAwait(false);
        }

        private async Task AssertHasDiagnosticWithDependencyProject(string testCase)
        {
            var dependencyCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "DependencyProject", $"{testCase}.al"))
                .ConfigureAwait(false);
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<Analyzers.UseSequentialGuid>(
                new AnalyzerTestFixtureConfig
                {
                    DependencyProjects = [new DependencyProject("DependencyApp", dependencyCode)]
                });

            fixture.NoException(code.Replace("[|", "").Replace("|]", ""));
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UseSequentialGuid);
        }

        [Test]
        [TestCase("NonKeyGuidField")]
        [TestCase("TemporaryTable")]
        [TestCase("GuidVariableNotInKey")]
        [TestCase("NonGuidKeyField")]
        [TestCase("AssignedToGuidVariableUsedElsewhere")]
        [TestCase("AlreadySequentialGuid")]
        [TestCase("GlobalVariableNotInKey")]
        [TestCase("PageTemporarySourceTable")]
        [TestCase("ImplicitPrimaryKeyNonKeyField")]
        [TestCase("TableExtensionKeyOnNonGuidField")]
        [TestCase("TableExtensionKeyOnOtherTable")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireMinimumVersion("16.0", "Available with runtime version 16.0.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseSequentialGuid);
        }

        // The callee lives in a second workspace project that the test project references, so its
        // declaring syntax belongs to another compilation. NoException catches the analyzer crashing
        // on that syntax, which NoDiagnosticAtAllMarkers would hide behind the filtered AD0001.
        [Test]
        [TestCase("CrossProjectCallee")]
        [TestCase("CrossProjectCalleeDirectArgument")]
        public async Task NoDiagnosticWithDependencyProject(string testCase)
        {
            RequireMinimumVersion("16.0", "Available with runtime version 16.0.");

            var dependencyCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "DependencyProject", $"{testCase}.al"))
                .ConfigureAwait(false);
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<Analyzers.UseSequentialGuid>(
                new AnalyzerTestFixtureConfig
                {
                    DependencyProjects = [new DependencyProject("DependencyApp", dependencyCode)]
                });

            fixture.NoException(code.Replace("[|", "").Replace("|]", ""));
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UseSequentialGuid);
        }

        [Test]
        [TestCase("SimpleCreateGuid")]
        [TestCase("QualifiedCreateGuid")]
        public async Task HasFix(string testCase)
        {
            RequireMinimumVersion("16.0", "Available with runtime version 16.0.");

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<UseSequentialGuidCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.UseSequentialGuid);
        }
    }
}
