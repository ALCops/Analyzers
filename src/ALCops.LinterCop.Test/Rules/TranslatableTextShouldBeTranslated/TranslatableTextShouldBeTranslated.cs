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

        // Trans-unit id for a table Caption in namespace "MyCompany.App" when the compiler feature
        // TranslationsWithNamespaces is enabled: namespace-prefixed, unhashed segments joined by " - ".
        private static readonly byte[] NamespaceTranslatedTableCaptionXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Namespace MyCompany.App - Table MyTable - Property Caption" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Table</source>
                      <target>Min tabel</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Table MyTable - Property Caption</note>
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

        private static readonly byte[] AnalysisViewDefinitionContent = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "Id": "00000000-0000-0000-0000-000000000001",
                "Name": "MyAnalysisView",
                "TargetObjectId": 50100,
                "TargetObjectType": "Page"
            }
            """);

        private static AnalyzerTestFixture CreateFixtureWithEmptyXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", EmptyXliffContent },
                { "MyAnalysisView.analysis.json", AnalysisViewDefinitionContent }
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

        // COMPAT: CompilerFeatures.TranslationsWithNamespaces and CompilationOptions.WithCompilerFeatures are
        // resolved reflectively so this test project still compiles against older SDKs where the enum member is
        // absent. The namespace tests are version-gated (RequireMinimumVersion) so this only runs where present.
        private static Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions CreateNamespaceCompilationOptions()
        {
            var options = new Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions();
            var optionsType = typeof(Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions);
            var featuresType = optionsType.Assembly.GetType("Microsoft.Dynamics.Nav.CodeAnalysis.CompilerFeatures")!;
            var feature = Enum.Parse(featuresType, "TranslationsWithNamespaces");
            var withFeatures = optionsType.GetMethod("WithCompilerFeatures", new[] { featuresType })!;
            return (Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions)withFeatures.Invoke(options, new[] { feature })!;
        }

        private static AnalyzerTestFixture CreateFixtureWithNamespaceFeature(byte[] xliffContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", xliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem,
                    CompilationOptions = CreateNamespaceCompilationOptions()
                });
        }

        [Test]
        [TestCase("LocalLabel")]
        [TestCase("GlobalLabel")]
        [TestCase("TableFieldCaption")]
        [TestCase("EnumValueCaption")]
        [TestCase("PageControlToolTip")]
        [TestCase("PageAnalysisViewCaption")]
        [TestCase("ReportLabel")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            SkipTestIfVersionIsTooLow(
                ["PageAnalysisViewCaption"],
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithEmptyXliff();
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LockedLabel")]
        [TestCase("LockedReportLabel")]
        [TestCase("PageAnalysisViewLockedCaption")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            SkipTestIfVersionIsTooLow(
                ["PageAnalysisViewLockedCaption"],
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

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

        [Test]
        [TestCase("NamespaceTableCaption")]
        public async Task HasDiagnosticWithNamespaces(string testCase)
        {
            RequireMinimumVersion("18.0.38.52553",
                "Translations with namespaces (CompilerFeatures.TranslationsWithNamespaces) requires the 18.0.38.52553 SDK.");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithNamespaceFeature(EmptyXliffContent);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("NamespaceTableCaptionTranslated")]
        public async Task NoDiagnosticWithNamespaces(string testCase)
        {
            RequireMinimumVersion("18.0.38.52553",
                "Translations with namespaces (CompilerFeatures.TranslationsWithNamespaces) requires the 18.0.38.52553 SDK.");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithNamespaceFeature(NamespaceTranslatedTableCaptionXliffContent);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }
    }
}
