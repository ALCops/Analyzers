using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class NamingPattern : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        private static readonly byte[] EnumValueNamingSettings = System.Text.Encoding.UTF8.GetBytes(
            """{"NamingPatterns": {"EnumValue": {"AllowPattern": "^[A-Z]", "AllowDescription": "should start with an uppercase letter"}}}""");

        // AppSourceCop mandatory affix that is a leading substring of a valid PascalCase
        // object name (e.g. "Cust" in "Customer"). Stripping it as a prefix used to turn
        // "Customer" into "omer", a false positive.
        private static readonly byte[] AppSourceCopPrefixAffix = System.Text.Encoding.UTF8.GetBytes(
            """{"mandatoryAffixes": ["Cust"]}""");

        // AppSourceCop mandatory affix that is a trailing substring of a valid object name
        // (e.g. "mer" in "Customer"). Stripping it as a suffix used to turn "Customer" into
        // "Custo".
        private static readonly byte[] AppSourceCopSuffixAffix = System.Text.Encoding.UTF8.GetBytes(
            """{"mandatoryAffixes": ["mer"]}""");

        // Object names must end with "er"; combined with the suffix affix "mer", the full
        // name "Customer" satisfies the pattern while the affix-stripped "Custo" does not.
        private static readonly byte[] ObjectEndsWithErSettings = System.Text.Encoding.UTF8.GetBytes(
            """{"NamingPatterns": {"Object": {"AllowPattern": "er$", "AllowDescription": "should end with 'er'"}}}""");


        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.NamingPattern>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(NamingPattern)));
        }

        [Test]
        [TestCase("ProcedureLowerCaseStart")]
        [TestCase("VariableLowerCaseStart")]
        [TestCase("VariableWithSpecialChars")]
        [TestCase("ParameterLowerCaseStart")]
        [TestCase("ReturnValueLowerCaseStart")]
        [TestCase("ObjectLowerCaseStart")]
        [TestCase("FieldWithSpecialChars")]
        [TestCase("ActionLowerCaseStart")]
        [TestCase("ControlLowerCaseStart")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        [Test]
        [TestCase("ProcedurePascalCase")]
        [TestCase("VariablePascalCase")]
        [TestCase("FieldWithLettersAndDigits")]
        [TestCase("ObsoleteProcedure")]
        [TestCase("TriggerMethod")]
        [TestCase("InterfaceImplementingMethod")]
        [TestCase("EventSubscriberPascalCase")]
        [TestCase("EventSubscriberPlatformParams")]
        [TestCase("EventSubscriberUserParams")]
        [TestCase("ApiPageControlCamelCase")]
        [TestCase("ActionAcceleratorKey")]
        [TestCase("SingleLetterVariable")]
        [TestCase("SingleLetterParameter")]
        [TestCase("UnderscorePrefix")]
        [TestCase("XRecVariable")]
        [TestCase("XRecParameter")]
        [TestCase("EnumValueBlankSpace")]
        [TestCase("EnumValueLowerCaseStart")]
        [TestCase("ParameterPascalCase")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        [Test]
        [TestCase("EnumValueLowerCaseStartCustomSettings")]
        public async Task HasDiagnosticWithCustomSettings(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", EnumValueNamingSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.NamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        private AnalyzerTestFixture CreateFixture(Dictionary<string, byte[]> files) =>
            RoslynFixtureFactory.Create<Analyzers.NamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = new MemoryFileSystem(files)
                });

        // Regression tests for affix over-stripping (issue #436 / PR #447).
        // The affix is only stripped as a fallback when the full name already violates the
        // pattern; an object whose full name is valid must never be penalized for merely
        // starting or ending with a mandatory affix.
        [Test]
        public async Task NoDiagnostic_ObjectPrefixCollision()
        {
            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), "ObjectPrefixCollision.al"))
                .ConfigureAwait(false);

            CreateFixture(new Dictionary<string, byte[]>
            {
                { "AppSourceCop.json", AppSourceCopPrefixAffix }
            }).NoDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        [Test]
        public async Task NoDiagnostic_ObjectSuffixCollision()
        {
            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), "ObjectSuffixCollision.al"))
                .ConfigureAwait(false);

            CreateFixture(new Dictionary<string, byte[]>
            {
                { "AppSourceCop.json", AppSourceCopSuffixAffix },
                { "alcops.json", ObjectEndsWithErSettings }
            }).NoDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        [Test]
        public async Task HasDiagnostic_ObjectAffixGenuineViolation()
        {
            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), "ObjectAffixGenuineViolation.al"))
                .ConfigureAwait(false);

            CreateFixture(new Dictionary<string, byte[]>
            {
                { "AppSourceCop.json", AppSourceCopPrefixAffix }
            }).HasDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }
    }
}
