using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class UnnecessaryRecordParameterInMethodCall : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.UnnecessaryRecordParameterInMethodCall>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(UnnecessaryRecordParameterInMethodCall));
        }

        [Test]
        [TestCase("ExternalRecordMethodCall")]
        [TestCase("InternalTableMethodCall")]
        [TestCase("InternalPageMethodCall")]
        [TestCase("InternalTableExtensionMethodCall")]
        [TestCase("InternalPageExtensionMethodCall")]
        [TestCase("MultipleArguments")]
        [TestCase("XRecSelfArgument")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InternalPageExtensionMethodCall", "InternalTableExtensionMethodCall"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.UnnecessaryRecordParameterInMethodCall);
        }

        [Test]
        [TestCase("DifferentParameter")]
        [TestCase("EventPublisher")]
        [TestCase("BuiltInMethods")]
        [TestCase("PageRunModal")]
        [TestCase("FieldAccessExpression")]
        [TestCase("PublicPageMethodWithRec")]
        [TestCase("DatabaseObjectReference")]
        [TestCase("ExternalThisSelfMethodCall")]
        [TestCase("XRecArgumentInTable")]
        [TestCase("XRecArgumentOnPage")]
        [TestCase("RecordRefArgument")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ExternalThisSelfMethodCall"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UnnecessaryRecordParameterInMethodCall);
        }

        // The callee lives in a second workspace project that the test project references, so the
        // target method belongs to another module even though the call shape is MyTable.Proc(MyTable).
        // NoException catches the analyzer crashing on the foreign module, which
        // NoDiagnosticAtAllMarkers would hide behind the filtered AD0001.
        [Test]
        [TestCase("CrossModuleCall")]
        public async Task NoDiagnosticWithDependencyProject(string testCase)
        {
            var dependencyCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "DependencyProject", $"{testCase}.al"))
                .ConfigureAwait(false);
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<Analyzers.UnnecessaryRecordParameterInMethodCall>(
                new AnalyzerTestFixtureConfig
                {
                    DependencyProjects = [new DependencyProject("DependencyApp", dependencyCode)]
                });

            fixture.NoException(code.Replace("[|", "").Replace("|]", ""));
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UnnecessaryRecordParameterInMethodCall);
        }
    }
}
