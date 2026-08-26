using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class StatementBlocksSeparatedByBlankLine : NavCodeAnalysisBase
    {
        // Named alcops.json snippets injected via MemoryFileSystem. Keys are the settingsKey
        // TestCase parameter below; null means "no alcops.json — use defaults".
        private static readonly Dictionary<string, byte[]> Settings = new()
        {
            ["OneLinerAll"]           = Utf8("""{"StatementBlockSpacing":{"OneLinerMode":"All"}}"""),
            ["ControlFlowOff"]        = Utf8("""{"StatementBlockSpacing":{"ControlFlowBefore":false,"ControlFlowAfter":false}}"""),
            ["ScopeLeavingOff"]       = Utf8("""{"StatementBlockSpacing":{"ScopeLeavingMode":"Off"}}"""),
            ["ExitOnly"]              = Utf8("""{"StatementBlockSpacing":{"ScopeLeavingMode":"ExitOnly"}}"""),
            ["ErrorOnly"]             = Utf8("""{"StatementBlockSpacing":{"ScopeLeavingMode":"ErrorOnly"}}"""),
            ["ElseChainRequireBlank"] = Utf8("""{"StatementBlockSpacing":{"ElseChainBeforeMode":"RequireBlank"}}"""),
            ["ControlFlowBeforeOnly"] = Utf8("""{"StatementBlockSpacing":{"ControlFlowBefore":true,"ControlFlowAfter":false}}"""),
            ["ControlFlowAfterOnly"]  = Utf8("""{"StatementBlockSpacing":{"ControlFlowBefore":false,"ControlFlowAfter":true}}"""),
            // Malformed enum value must be tolerated by ALCopsSettingsProvider: settings fall back
            // to defaults silently (JsonException catch in DeserializeSettings). Under defaults
            // ScopeLeavingMode=ExitAndError, so ExitOnly-fixture markers must still fire — proving
            // defaults kicked in.
            ["Malformed"]             = Utf8("""{"StatementBlockSpacing":{"ScopeLeavingMode":"NotAnEnumValue"}}"""),
            // Regression guard for issue #328: explicit `null` on the nested settings object
            // deserializes without JsonException (deserializers ignore NRT). Provider must
            // normalize null back to defaults so the analyzer does not NRE on property access.
            ["StatementBlockSpacingNull"] = Utf8("""{"StatementBlockSpacing":null}"""),
        };

        private string _testCasePath;
        private string _ruleSetPath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(StatementBlocksSeparatedByBlankLine)));

            _ruleSetPath = Path.Combine(_testCasePath, $"{nameof(StatementBlocksSeparatedByBlankLine)}.ruleset.json");
        }

        [Test]
        // Default settings (no alcops.json injected).
        [TestCase(null, "ControlFlowSpacingMissing")]
        [TestCase(null, "ControlFlowInteractionSpacing")]
        [TestCase(null, "ScopeLeavingSpacingMissing")]
        // Comment-only lines between statements must not satisfy the blank-line requirement.
        [TestCase(null, "CommentBetweenStatements")]
        // Config-driven positive cases.
        [TestCase("OneLinerAll", "OneLinerAll")]
        [TestCase("ScopeLeavingOff", "ControlFlowInteractionSpacing")]
        [TestCase("ExitOnly", "ExitOnly")]
        [TestCase("ErrorOnly", "ErrorOnly")]
        [TestCase("ElseChainRequireBlank", "ElseChainBlank")]
        // ControlFlowBeforeOnly fixture marks only the 'if' keyword; the trailing Message() call
        // (missing blank line after 'end;') is intentionally unmarked because "after" is off.
        [TestCase("ControlFlowBeforeOnly", "ControlFlowBeforeOnly")]
        // ControlFlowAfterOnly fixture marks only the trailing Message() call; the 'if' keyword
        // is intentionally unmarked because "before" is off.
        [TestCase("ControlFlowAfterOnly", "ControlFlowAfterOnly")]
        [TestCase("ControlFlowAfterOnly", "ControlFlowAfterAdjacentControlFlow")]
        // Regression guard for malformed alcops.json → provider must fall back to defaults; the
        // ExitOnly fixture's exit markers require the default ScopeLeavingMode=ExitAndError.
        [TestCase("Malformed", "ExitOnly")]
        // Regression guard for issue #328: explicit null on the nested settings object must not
        // NRE and must fall back to defaults; ExitOnly markers fire under the default mode.
        [TestCase("StatementBlockSpacingNull", "ExitOnly")]
        public async Task HasDiagnostic(string? settingsKey, string fixtureName)
        {
            var code = await LoadFixtureAsync(fixtureName);

            CreateFixture(settingsKey)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        // Default settings.
        [TestCase(null, "ControlFlowSpacingValid")]
        [TestCase(null, "ScopeLeavingSpacingValid")]
        [TestCase(null, "DisabledByDefault")]
        // Disabling ControlFlowBefore/After entirely must silence the ControlFlowSpacingMissing fixture.
        [TestCase("ControlFlowOff", "ControlFlowSpacingMissing")]
        // ScopeLeavingMode=Off must silence both scope-leaving fixtures.
        [TestCase("ScopeLeavingOff", "ExitOnly")]
        [TestCase("ScopeLeavingOff", "ErrorOnly")]
        // ExitOnly config must suppress Error() markers in the ErrorOnly fixture.
        [TestCase("ExitOnly", "ErrorOnly")]
        // ErrorOnly config must suppress exit markers in the ExitOnly fixture.
        [TestCase("ErrorOnly", "ExitOnly")]
        // ElseChainBeforeMode=RequireBlank must accept the well-spaced else-chain fixture.
        [TestCase("ElseChainRequireBlank", "ElseChainBlankValid")]
        public async Task NoDiagnostic(string? settingsKey, string fixtureName)
        {
            var code = await LoadFixtureAsync(fixtureName);

            CreateFixture(settingsKey)
                .NoDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        [TestCase("ControlFlowInteractionSpacing", 4)]
        public async Task ReportsExpectedDiagnosticCount(string fixtureName, int expectedCount)
        {
            var code = await LoadFixtureAsync(fixtureName);
            var fixture = new DiagnosticCountingFixture(_ruleSetPath);

            Assert.That(
                fixture.CountDiagnostics(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine),
                Is.EqualTo(expectedCount));
        }

        // Fixtures live in either HasDiagnostic/ or NoDiagnostic/ subfolders (repo convention).
        // Since names are unique across both, callers only supply the fixture name and this helper
        // resolves the folder — keeping test signatures free of layout details.
        private async Task<string> LoadFixtureAsync(string fixtureName)
        {
            foreach (var folder in new[] { nameof(HasDiagnostic), nameof(NoDiagnostic) })
            {
                var path = Path.Combine(_testCasePath, folder, $"{fixtureName}.al");

                if (File.Exists(path))
                {
                    return await File.ReadAllTextAsync(path).ConfigureAwait(false);
                }
            }

            throw new FileNotFoundException(
                $"Fixture '{fixtureName}.al' not found in {nameof(HasDiagnostic)}/ or {nameof(NoDiagnostic)}/ under '{_testCasePath}'.");
        }

        private AnalyzerTestFixture CreateFixture(string? settingsKey) =>
            RoslynFixtureFactory.Create<Analyzers.StatementBlocksSeparatedByBlankLine>(
                new AnalyzerTestFixtureConfig
                {
                    // Physical ruleset enables the disabled-by-default FC0007 rule.
                    RuleSetPath = _ruleSetPath,
                    // Optional alcops.json injected via in-memory file system for config-driven cases.
                    FileSystem = settingsKey is null
                        ? null
                        : new MemoryFileSystem(new Dictionary<string, byte[]>
                        {
                            { "alcops.json", Settings[settingsKey] }
                        })
                });

        private static byte[] Utf8(string s) => System.Text.Encoding.UTF8.GetBytes(s);

        private sealed class DiagnosticCountingFixture(string ruleSetPath) : AnalyzerTestFixture
        {
            protected override string LanguageName => LanguageNames.AL;
            protected override string? RuleSetPath => ruleSetPath;

            protected override DiagnosticAnalyzer CreateAnalyzer() =>
                new Analyzers.StatementBlocksSeparatedByBlankLine();

            public int CountDiagnostics(string markupCode, string diagnosticId)
            {
                var markup = new CodeMarkup(markupCode);
                var document = CreateDocumentFromCode(markup.Code);
                var compilation = document.Project
                    .GetCompilationAsync(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                if (compilation is null)
                {
                    return 0;
                }

                var diagnostics = compilation
                    .WithAnalyzers(
                        ImmutableArray.Create(CreateAnalyzer()),
                        cancellationToken: CancellationToken.None)
                    .GetAnalyzerDiagnosticsAsync()
                    .GetAwaiter()
                    .GetResult();

                return diagnostics.Count(diagnostic => diagnostic.Id == diagnosticId);
            }
        }
    }
}
