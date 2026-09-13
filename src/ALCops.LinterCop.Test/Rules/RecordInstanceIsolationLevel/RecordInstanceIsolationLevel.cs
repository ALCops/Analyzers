using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class RecordInstanceIsolationLevel : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.RecordInstanceIsolationLevel _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.RecordInstanceIsolationLevel>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(RecordInstanceIsolationLevel)));
        }

        [Test]
        [TestCase("LockTable")]
        [TestCase("NamedVariable")]
        [TestCase("BareSelfInProcedure")]
        [TestCase("BareSelfInTrigger")]
        [TestCase("RecSelfInTrigger")]
        [TestCase("ThisSelfInProcedure")]
        [TestCase("ThisSelfInTrigger")]
        [TestCase("BareSelfInTableExtension")]
        [TestCase("RecSelfInTableExtension")]
        [TestCase("ThisSelfInTableExtension")]
        [TestCase("PageRecSelf")]
        [TestCase("PageBareSelf")]
        [TestCase("OnRunRecSelf")]
        [TestCase("OnRunBareSelf")]
        [TestCase("NamespacedQualifiedRecordVariable")]
        [TestCase("RecordRefVariable")]
        [TestCase("LockTableWithArguments")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisSelfInProcedure", "ThisSelfInTrigger", "ThisSelfInTableExtension"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.RecordInstanceIsolationLevel);
        }

        [Test]
        [TestCase("ObsoleteProcedure")]
        [TestCase("ReadIsolationMethodForm")]
        [TestCase("ReadIsolationPropertyForm")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.RecordInstanceIsolationLevel);
        }

        [Test]
        [TestCase("ReplaceLockTableWithReadIsolation")]
        [TestCase("ReplaceLockTableWithReadIsolationUsingRec")]
        [TestCase("BareSelfInTrigger")]
        [TestCase("BareSelfInTableExtension")]
        [TestCase("ThisSelf")]
        [TestCase("RecordRefVariable")]
        public async Task HasFix(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["ThisSelf"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<RecordInstanceIsolationLevelCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.RecordInstanceIsolationLevel);
        }
    }
}
