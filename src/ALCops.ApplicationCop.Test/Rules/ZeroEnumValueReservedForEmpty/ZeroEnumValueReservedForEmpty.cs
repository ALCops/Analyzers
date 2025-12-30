using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class ZeroEnumValueReservedForEmpty : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ZeroEnumValueReservedForEmpty>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ZeroEnumValueReservedForEmpty)));
        }

        [Test]
        [TestCase("Enum")]
        [TestCase("EnumCaption")]
        [TestCase("EnumWithCaption")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ZeroEnumValueReservedForEmpty);
        }

        [Test]
        [TestCase("Enum")]
        [TestCase("EnumCaption")]
        [TestCase("Interface")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ZeroEnumValueReservedForEmpty);
        }
    }
}