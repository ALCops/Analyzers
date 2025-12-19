using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class ObjectIdInDeclaration : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.ObjectIdInDeclaration _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ObjectIdInDeclaration>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ObjectIdInDeclaration)));
        }

        [Test]
        [TestCase("DataTypeCodeunit")]
        [TestCase("DataTypePage")]
        [TestCase("DataTypeQuery")]
        [TestCase("DataTypeRecordRef")]
        [TestCase("DataTypeReport")]
        [TestCase("DataTypeXmlPort")]
        [TestCase("EventSubscriber")]
        [TestCase("GlobalVariable")]
        [TestCase("LocalVariable")]
        [TestCase("PagePart")]
        [TestCase("Profile")]
        [TestCase("ReportDataItem")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["DataTypeQuery"],
                testCase,
                "14.0",
                "error AL0132: 'Query' does not contain a definition for 'SaveAsJson'"
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ObjectIdInDeclaration);
        }

        [Test]
        [TestCase("DataTypePage")]
        [TestCase("DataTypeRecord")]
        [TestCase("DataTypeRecordRef")]
        [TestCase("EventSubscriber")]
        [TestCase("GlobalVariable")]
        [TestCase("LocalVariable")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ObjectIdInDeclaration);
        }

        [Test]
        [TestCase("DataTypeCodeunit")]
        [TestCase("DataTypePage")]
        [TestCase("DataTypeQuery")]
        [TestCase("DataTypeRecordRef")]
        [TestCase("DataTypeReport")]
        [TestCase("DataTypeXmlPort")]
        [TestCase("EventSubscriberCodeunit")]
        [TestCase("EventSubscribeReport")]
        [TestCase("EventSubscriberPage")]
        [TestCase("EventSubscriberQuery")]
        [TestCase("EventSubscriberCodeunit")]
        [TestCase("EventSubscribeTable")]
        [TestCase("EventSubscribeXmlPort")]
        [TestCase("GlobalVariable")]
        [TestCase("LocalVariable")]
        [TestCase("PagePart")]
        [TestCase("Profile")]
        [TestCase("ReportDataItem")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<ObjectIdInDeclarationCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.ObjectIdInDeclaration);
        }
    }
}