using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class TranslatableTextShouldBeTranslated : NavCodeAnalysisBase
    {
        private string _testCasePath;

        private static readonly byte[] EmptyXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                  </group>
                </body>
              </file>
            </xliff>
            """);

        private static readonly byte[] TranslatedReportLabelXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Report 2858589782 - ReportLabel 973805576" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Report Label Text</source>
                      <target>Berichtsbezeichnungstext</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportLabel MyReportLabel</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        private static readonly byte[] SettingsWithDaDK = System.Text.Encoding.UTF8.GetBytes(
            """{"LanguagesToTranslate": ["da-DK"]}""");

        private static readonly byte[] SettingsWithDaDKAndDeDE = System.Text.Encoding.UTF8.GetBytes(
            """{"LanguagesToTranslate": ["da-DK", "de-DE"]}""");

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TranslatableTextShouldBeTranslated)));
        }

        private static AnalyzerTestFixture CreateFixtureWithEmptyXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", EmptyXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithoutXliff()
        {
            var files = new Dictionary<string, byte[]>();
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithSettings(byte[] settingsContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", settingsContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithXliffAndSettings(byte[] settingsContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", EmptyXliffContent },
                { "alcops.json", settingsContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithTranslatedReportLabelXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", TranslatedReportLabelXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        [Test]
        [TestCase("LocalLabel")]
        [TestCase("GlobalLabel")]
        [TestCase("TableFieldCaption")]
        [TestCase("EnumValueCaption")]
        [TestCase("PageControlToolTip")]
        [TestCase("ReportLabel")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithEmptyXliff();
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LockedLabel")]
        [TestCase("LockedReportLabel")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithEmptyXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("TranslatedReportLabel")]
        public async Task NoDiagnosticTranslated(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithTranslatedReportLabelXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("NoXliffFiles")]
        public async Task NoDiagnosticNoXliff(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithoutXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LocalLabel")]
        public async Task HasDiagnosticWithLanguagesToTranslateNoXliff(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(SettingsWithDaDK);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LocalLabel")]
        public async Task HasDiagnosticWithLanguagesToTranslatePartialXliff(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithXliffAndSettings(SettingsWithDaDKAndDeDE);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }
    }
}
