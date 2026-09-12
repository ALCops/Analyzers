using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class NamingPattern : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        private static readonly byte[] CustomNamingSettings = System.Text.Encoding.UTF8.GetBytes(
            """{"NamingPatterns": {"EnumValue": {"AllowPattern": "^[A-Z]", "AllowDescription": "should start with an uppercase letter"}, "Action": {"AllowPattern": "act[A-Za-z0-9]", "AllowDescription": "should begin with 'act'."}, "Control": {"AllowPattern": "ctl[A-Za-z0-9]", "AllowDescription": "should begin with 'ctl'."}}}""");

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
        [TestCase("ActionAreaLowerCase")]
        [TestCase("ControlAreaLowerCase")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        [Test]
        [TestCase("EnumValueLowerCaseStartCustomSettings")]
        [TestCase("ActionGroupCustomPattern")]
        public async Task HasDiagnosticWithCustomSettings(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(CustomNamingSettings);

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        [Test]
        [TestCase("ActionAreaCustomPattern")]
        [TestCase("ControlAreaCustomPattern")]
        [TestCase("PromotedCategoryGroupCustomPattern")]
        [TestCase("SystemActionCustomPattern")]
        public async Task NoDiagnosticWithCustomSettings(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["SystemActionCustomPattern"],
                testCase,
                "16.2.31",
                "The fixture's 'ConfigurationDialog' page type is rejected as a feature under development (AL0574) before SDK 16.2.31.");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(CustomNamingSettings);

            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }

        private static AnalyzerTestFixture CreateFixtureWithSettings(byte[] settings)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", settings }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.NamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }
    }
}
