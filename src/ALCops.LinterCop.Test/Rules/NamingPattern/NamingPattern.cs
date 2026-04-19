using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class NamingPattern : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

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
        [TestCase("EnumValueLowerCaseStart")]
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
        [TestCase("ParameterPascalCase")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.NamingPattern);
        }
    }
}
