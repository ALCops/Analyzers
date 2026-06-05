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
    }
}
