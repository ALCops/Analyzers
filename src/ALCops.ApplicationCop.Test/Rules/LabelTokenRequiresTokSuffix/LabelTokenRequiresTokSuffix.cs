using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class LabelTokenRequiresTokSuffix : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.LabelTokenRequiresTokSuffix>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(LabelTokenRequiresTokSuffix)));
        }

        [Test]
        [TestCase("NameEqualsText_LblSuffix_CaseInsensitiveMatch")]
        [TestCase("NameEqualsText_LblSuffix_LocalVar")]
        [TestCase("NameEqualsText_LblSuffix")]
        [TestCase("NameEqualsText_NoSuffix")]
        [TestCase("NameEqualsText_TxtSuffix_AlphanumericMatch")]
        [TestCase("NameEqualsText_TxtSuffix")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.LabelTokenRequiresTokSuffix);
        }

        [Test]
        [TestCase("AlreadyTokSuffix")]
        [TestCase("EmptyText")]
        [TestCase("NoLockedProperty")]
        [TestCase("NonLabelVariable")]
        [TestCase("NotLocked")]
        [TestCase("TextDoesNotMatch")]
        [TestCase("TextDoesNotMatchAlphanumeric")]
        [TestCase("TextDoesNotMatchStrippedName")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.LabelTokenRequiresTokSuffix);
        }
    }
}