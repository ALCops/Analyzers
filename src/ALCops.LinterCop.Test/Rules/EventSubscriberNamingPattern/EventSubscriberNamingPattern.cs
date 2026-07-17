using ALCops.LinterCop.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class EventSubscriberNamingPattern : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.EventSubscriberNamingPattern _analyzer = new();
        private string _testCasePath;

        // PascalCase-glued template with a leading 'On'. Used to exercise the acronym-aware
        // renderer path, which is bypassed by the raw-form default template.
        private const string PascalTemplate = "On{EventSource}_{EventName}[_{ElementName}]";

        private static readonly byte[] CustomTemplateSettings = System.Text.Encoding.UTF8.GetBytes(
            """{"SubscriberNamingPattern": "Handle{EventSource}{EventName}"}""");

        private static readonly byte[] OptionalGroupTemplateSettings = System.Text.Encoding.UTF8.GetBytes(
            $$"""{"SubscriberNamingPattern": "{{PascalTemplate}}"}""");

        private static readonly byte[] PascalTemplateSettings = System.Text.Encoding.UTF8.GetBytes(
            $$"""{"SubscriberNamingPattern": "{{PascalTemplate}}"}""");

        // Combined with a PascalCase template so the acronym behaviour is observable
        // (the raw-form template emits source names verbatim and never invokes the acronym renderer).
        private static readonly byte[] CustomAcronymSettings = System.Text.Encoding.UTF8.GetBytes(
            $$"""{"SubscriberNamingPattern": "{{PascalTemplate}}", "KnownAcronyms": ["Acme"]}""");

        // Overrides the built-in default "VAT" with the alternate canonical casing "Vat".
        private static readonly byte[] OverrideDefaultAcronymSettings = System.Text.Encoding.UTF8.GetBytes(
            $$"""{"SubscriberNamingPattern": "{{PascalTemplate}}", "KnownAcronyms": ["Vat"]}""");

        // Pins "Lcy" as an accepted variant alongside the original casing "LCY".
        // Both spellings are accepted; any third variant still triggers a diagnostic.
        private static readonly byte[] LcyAcronymSettings = System.Text.Encoding.UTF8.GetBytes(
            $$"""{"SubscriberNamingPattern": "{{PascalTemplate}}", "KnownAcronyms": ["Lcy"]}""");

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(EventSubscriberNamingPattern)));
        }

        [Test]
        [TestCase("WrongName")]
        [TestCase("WrongNameWithElementName")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("AcronymWrongCasing")]
        [TestCase("IdAbbreviationUppercase")]
        [TestCase("TwoLetterAcronymNormalized")]
        public async Task HasDiagnosticPascalTemplate(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(PascalTemplateSettings);

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("DefaultTemplate")]
        [TestCase("WithElementName")]
        [TestCase("RawEventSourceWithSpace")]
        [TestCase("RawElementNameWithSpace")]
        [TestCase("NotASubscriber")]
        [TestCase("DerivedNameExceedsMaxLength")]
        [TestCase("PreferredNameCollidesWithSibling")]
        [TestCase("TwoSubscribersSameEventBothMisnamed")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("AcronymFromKnownListPreserved")]
        [TestCase("IdAbbreviationNormalized")]
        [TestCase("TwoLetterAcronymPreserved")]
        [TestCase("UnknownAcronymPreserved")]
        [TestCase("ElementNameWithPercent")]
        public async Task NoDiagnosticPascalTemplate(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(PascalTemplateSettings);

            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        private static AnalyzerTestFixture CreateFixtureWithSettings(byte[] settings)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", settings }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        [Test]
        [TestCase("CustomTemplate")]
        public async Task HasDiagnosticWithCustomSettings(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", CustomTemplateSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("OptionalGroupViolation")]
        public async Task HasDiagnosticOptionalGroupTemplate(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", OptionalGroupTemplateSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("OptionalGroupEmptyElement")]
        [TestCase("OptionalGroupWithElement")]
        public async Task NoDiagnosticOptionalGroupTemplate(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", OptionalGroupTemplateSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("CustomAcronymPinnedRejectsUppercase")]
        public async Task HasDiagnosticWithCustomAcronyms(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", CustomAcronymSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("CustomAcronymPinnedCanonical")]
        public async Task NoDiagnosticWithCustomAcronyms(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", CustomAcronymSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("CustomAcronymOverridesDefault")]
        public async Task HasDiagnosticWithOverriddenDefaultAcronym(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", OverrideDefaultAcronymSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("CustomAcronymOverridesDefault")]
        public async Task NoDiagnosticWithOverriddenDefaultAcronym(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", OverrideDefaultAcronymSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<Analyzers.EventSubscriberNamingPattern>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });

            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("AcronymRejectsThirdVariant")]
        public async Task HasDiagnosticWithLcyAcronym(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(LcyAcronymSettings);

            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("AcronymAcceptsOriginalCasing")]
        [TestCase("AcronymAcceptsRegistryVariant")]
        public async Task NoDiagnosticWithLcyAcronym(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(LcyAcronymSettings);

            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("RenameToDefaultTemplate")]
        [TestCase("RenameWithElementName")]
        [TestCase("RenameToRawWithSpaces")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<EventSubscriberNamingPatternCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.EventSubscriberNamingPattern);
        }

        [Test]
        [TestCase("RenameWithAcronym")]
        public async Task HasFixPascalTemplate(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", PascalTemplateSettings }
            };
            var fileSystem = new MemoryFileSystem(files);

            var fixture = RoslynFixtureFactory.Create<EventSubscriberNamingPatternCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer],
                    FileSystem = fileSystem
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.EventSubscriberNamingPattern);
        }
    }
}
