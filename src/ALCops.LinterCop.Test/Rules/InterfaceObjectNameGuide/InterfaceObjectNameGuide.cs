using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class InterfaceObjectNameGuide : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        private static readonly byte[] AppSourceCopWithAffixes = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "mandatoryPrefix": "ABC ",
                "mandatorySuffix": "XYZ ",
                "mandatoryAffixes": ["FOO "]
            }
            """);

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(InterfaceObjectNameGuide)));

            // LC0054 is isEnabledByDefault: false; the ruleset enables it for tests
            _fixture = RoslynFixtureFactory.Create<Analyzers.InterfaceObjectNameGuide>(
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(InterfaceObjectNameGuide)}.ruleset.json")
                });
        }

        private AnalyzerTestFixture CreateFixtureWithAffixes()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "AppSourceCop.json", AppSourceCopWithAffixes }
            };

            return RoslynFixtureFactory.Create<Analyzers.InterfaceObjectNameGuide>(
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(InterfaceObjectNameGuide)}.ruleset.json"),
                    FileSystem = new MemoryFileSystem(files)
                });
        }

        [Test]
        [TestCase("NoLeadingI")]
        [TestCase("WhitespaceAfterI")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InterfaceObjectNameGuide);
        }

        [Test]
        [TestCase("LeadingI")]
        [TestCase("SingleCharacterI")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InterfaceObjectNameGuide);
        }

        [Test]
        [TestCase("AffixWithoutI")]
        [TestCase("AffixThenIWithWhitespace")]
        [TestCase("AffixThenNoLettersOrDigits")]
        public async Task HasDiagnosticWithAffixes(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateFixtureWithAffixes().HasDiagnosticAtAllMarkers(code, DiagnosticIds.InterfaceObjectNameGuide);
        }

        [Test]
        [TestCase("PrefixThenI")]
        [TestCase("AffixThenI")]
        [TestCase("SuffixAsLeadingAffixThenI")]
        public async Task NoDiagnosticWithAffixes(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateFixtureWithAffixes().NoDiagnosticAtAllMarkers(code, DiagnosticIds.InterfaceObjectNameGuide);
        }
    }
}
