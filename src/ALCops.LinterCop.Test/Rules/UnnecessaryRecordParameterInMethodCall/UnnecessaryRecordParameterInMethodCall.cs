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
                    Path.Combine("Rules", nameof(UnnecessaryRecordParameterInMethodCall)));
        }

        [Test]
        [TestCase("ExternalRecordMethodCall")]
        [TestCase("InternalTableMethodCall")]
        [TestCase("InternalPageMethodCall")]
        [TestCase("InternalTableExtensionMethodCall")]
        [TestCase("InternalPageExtensionMethodCall")]
        [TestCase("MultipleArguments")]
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
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.UnnecessaryRecordParameterInMethodCall);
        }
    }
}
