using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
    public class ObjectRequiresDocumentation : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(ObjectRequiresDocumentation));

            _fixture = RoslynFixtureFactory.Create<Analyzers.ObjectRequiresDocumentation>(
                // Inject a ruleset to enable testing for rules, that are not enabled by default (isEnabledByDefault: false).
                new AnalyzerTestFixtureConfig
                {
                    RuleSetPath = Path.Combine(_testCasePath, $"{nameof(ObjectRequiresDocumentation)}.ruleset.json")
                });
        }

        [Test]
        [TestCase("PublicCodeunit")]
        [TestCase("ProfileWithoutXmlDocumentation")]
        [TestCase("PublicInterfaceAndControlAddIn")]
        [TestCase("UndocumentedExtensions")]
        [TestCase("UndocumentedProfileExtension")]
        [TestCase("UndocumentedPageCustomization")]
        public async Task PublicHasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["UndocumentedExtensions", "UndocumentedProfileExtension", "UndocumentedPageCustomization"],
                testCase,
                "13.0",
                "No support for extension objects when the target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ["UndocumentedProfileExtension"],
                testCase,
                "14.0",
                "Profile extensions are not supported before AL 14.0");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicHasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PublicObjectRequiresDocumentation);
        }

        [Test]
        [TestCase("PublicCodeunit")]
        [TestCase("NamespacedQuotedProfileWithXmlDocumentation")]
        [TestCase("PublicInterfaceAndControlAddIn")]
        [TestCase("DocumentedExtensions")]
        [TestCase("DocumentedProfileExtension")]
        [TestCase("DocumentedPageCustomization")]
        public async Task PublicNoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["DocumentedExtensions", "DocumentedProfileExtension", "DocumentedPageCustomization"],
                testCase,
                "13.0",
                "No support for extension objects when the target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ["DocumentedProfileExtension"],
                testCase,
                "14.0",
                "Profile extensions are not supported before AL 14.0");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicNoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PublicObjectRequiresDocumentation);
        }

        [Test]
        [TestCase("InternalCodeunit")]
        [TestCase("InternalInterface")]
        public async Task InternalHasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalHasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalObjectRequiresDocumentation);
        }

        [Test]
        [TestCase("InternalCodeunit")]
        [TestCase("InternalInterface")]
        [TestCase("UndocumentedExtensions")]
        public async Task InternalNoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["UndocumentedExtensions"],
                testCase,
                "13.0",
                "No support for extension objects when the target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalNoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalObjectRequiresDocumentation);
        }
    }
}
