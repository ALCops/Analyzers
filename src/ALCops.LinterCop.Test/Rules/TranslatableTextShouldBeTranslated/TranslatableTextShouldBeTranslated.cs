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

		// "Page 2931038265 - Control 1296262074 - Property 1295455071"
		// "Page 2931038265 - Control 2674903734 - Property 1295455071"
        private static readonly byte[] TranslatedPageControlToolTipXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="de-DE" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is a tooltip.</source>
                      <target>Dies ist ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property ToolTip</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 2674903734 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is also a tooltip.</source>
                      <target>Dies ist ebenfalls ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control SecondField - Property ToolTip</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Legacy runtime (<= Spring2020CU1 / 5.1) hashes the source-cased property name, not the
        // canonical PropertyKind name. For source "Tooltip" the compiler emits hash 2001309823
        // (FNV-1a of UTF-16 "Tooltip") instead of 1295455071 (FNV-1a of "ToolTip"). This XLIFF is
        // what the compiler generates for LegacyRuntimePageControlToolTip.al on runtime <= 5.1;
        // used to verify the analyzer does NOT report a false positive when the runtime-version
        // gate correctly disables the canonical override. If the analyzer applied the canonical
        // override unconditionally, it would hash to 1295455071 and miss this trans-unit, raising
        // a false-positive diagnostic on the fully-translated property.
        private static readonly byte[] LegacySourceCasedTooltipXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="de-DE" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 2001309823" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is a tooltip.</source>
                      <target>Dies ist ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property Tooltip</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Same fixture as LegacySourceCasedTooltipXliffContent but using the canonical property
        // name hash (1295455071 for "ToolTip") instead of the source-cased hash. Used together
        // with the source-cased AL fixture on a legacy-runtime app.json to prove the analyzer
        // correctly reports a missing translation when the runtime is <= 5.1 (fix scenario:
        // source-cased hash 2001309823 not in XLIFF -> diagnostic). Under the buggy unconditional
        // canonical override, the analyzer would hash to 1295455071 and match this trans-unit,
        // silently missing the diagnostic (false negative).
        private static readonly byte[] LegacyCanonicalTooltipXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="de-DE" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is a tooltip.</source>
                      <target>Dies ist ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property ToolTip</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Minimal app.json declaring runtime 5.1 (Spring2020CU1). Used to force the analyzer down
        // the pre-canonical-override compiler path in LC0091's runtime-version gate.
        private static readonly byte[] AppJsonRuntime51Content = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "id": "00000000-0000-0000-0000-000000000042",
                "name": "TestApp",
                "publisher": "TestPublisher",
                "version": "1.0.0.0",
                "runtime": "5.1",
                "idRanges": [ { "from": 50000, "to": 99999 } ],
                "features": [ "TranslationFile" ]
            }
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

        private static AnalyzerTestFixture CreateFixtureWithTranslatedPageControlToolTipXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.de-DE.xlf", TranslatedPageControlToolTipXliffContent }
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

        // Fixture used to exercise LC0091's runtime-version gate. The app.json declares runtime 5.1
        // (Spring2020CU1), which causes the analyzer to hash the source-cased property name instead
        // of the canonical PropertyKind name — matching the compiler's SymbolExtensions.GetTranslationName
        // behavior on legacy runtimes.
        private static AnalyzerTestFixture CreateFixtureWithLegacyRuntime(byte[] xliffContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "app.json", AppJsonRuntime51Content },
                { "Translations/TestApp.de-DE.xlf", xliffContent }
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
        [TestCase("PageControlToolTipWrongCasing")]
        public async Task NoDiagnosticPropertyCasing(string testCase)
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

            var fixture = CreateFixtureWithTranslatedPageControlToolTipXliff();
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

        // Regression tests for the runtime-version gate around the canonical property-name override.
        // On runtime <= Spring2020CU1 (5.1) the compiler hashes the source-cased property name
        // (SymbolExtensions.GetTranslationName returns property.Name); on newer runtimes it uses the
        // canonical PropertyKind name. LC0091 must mirror this gate — an unconditional canonical
        // override would produce false positives (or false negatives) on legacy apps whose XLIFF
        // uses source-cased hashes. Both tests use a single-field page with source-cased "Tooltip"
        // and an app.json declaring runtime 5.1, and paired XLIFFs designed to fail under the buggy
        // unconditional-override behavior.
        [Test]
        [TestCase("LegacyRuntimePageControlToolTip")]
        public async Task HasDiagnosticLegacyRuntime(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            // XLIFF contains only the canonical hash (1295455071). With the runtime gate correctly
            // disabling the canonical override on runtime 5.1, the analyzer hashes source-cased
            // "Tooltip" to 2001309823, which is NOT in the XLIFF -> diagnostic fires as expected.
            // Under the buggy unconditional-override path, the analyzer would match the canonical
            // trans-unit and silently miss the diagnostic (false negative regression).
            var fixture = CreateFixtureWithLegacyRuntime(LegacyCanonicalTooltipXliffContent);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LegacyRuntimePageControlToolTip")]
        public async Task NoDiagnosticLegacyRuntime(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            // XLIFF contains only the source-cased hash (2001309823). With the runtime gate
            // correctly disabling the canonical override on runtime 5.1, the analyzer hashes to
            // the same source-cased hash and matches the trans-unit -> no diagnostic. Under the
            // buggy unconditional-override path, the analyzer would hash to 1295455071 and NOT
            // match, raising a false-positive diagnostic on a fully-translated property.
            var fixture = CreateFixtureWithLegacyRuntime(LegacySourceCasedTooltipXliffContent);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }
    }
}
