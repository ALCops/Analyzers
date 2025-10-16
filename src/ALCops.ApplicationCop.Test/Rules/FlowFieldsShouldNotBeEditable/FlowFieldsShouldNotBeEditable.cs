using ALCops.ApplicationCop.CodeFixer;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{

    public class FlowFieldsShouldNotBeEditable : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzer.FlowFieldsShouldNotBeEditable _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzer.FlowFieldsShouldNotBeEditable>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(FlowFieldsShouldNotBeEditable)));
        }

        [Test]
        [TestCase("FlowFieldEditable")]
        [TestCase("FlowFieldEditableWithoutComment")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.FlowFieldsShouldNotBeEditable);
        }

        [Test]
        [TestCase("FlowFieldEditableFalse")]
        [TestCase("FlowFieldObsoletePending")]
        [TestCase("FlowFieldObsoleteRemoved")]
        [TestCase("FlowFieldTableObsoleteMoved")]
        [TestCase("FlowFieldTableObsoletePending")]
        [TestCase("FlowFieldTableObsoletePendingMove")]
        [TestCase("FlowFieldTableObsoleteRemoved")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["FlowFieldTableObsoleteMoved",
                "FlowFieldTableObsoletePending",
                "FlowFieldTableObsoletePendingMove"],
                testCase,
                "15.0.20"
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.FlowFieldsShouldNotBeEditable);
        }

        [Test]
        [TestCase("SingleFlowFieldIsEditable")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<FlowFieldsShouldNotBeEditableCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.FlowFieldsShouldNotBeEditable);
        }
    }
}