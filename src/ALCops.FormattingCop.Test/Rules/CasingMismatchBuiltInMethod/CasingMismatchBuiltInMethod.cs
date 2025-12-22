using ALCops.FormattingCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class CasingMismatchBuiltInMethod : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.CasingMismatchBuiltInMethod _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.CasingMismatchBuiltInMethod>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(CasingMismatchBuiltInMethod)));
        }

        [Test]
        [TestCase("FieldAccess")]
        [TestCase("GlobalReferenceExpression")]
        [TestCase("InvocationExpression")]
        [TestCase("LocalReferenceExpression")]
        [TestCase("ParameterReferenceExpression")]
        [TestCase("ReturnValueReferenceExpression")]
        [TestCase("XmlPortDataItemAccess")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.CasingMismatch);
        }

        [Test]
        [TestCase("FieldAccess")]
        [TestCase("GlobalReferenceExpression")]
        [TestCase("InvocationExpression")]
        [TestCase("LocalReferenceExpression")]
        [TestCase("ParameterReferenceExpression")]
        [TestCase("ReturnValueReferenceExpression")]
        [TestCase("XmlPortDataItemAccess")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.CasingMismatch);
        }
    }
}