using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.FormattingCop.Test
{
    public class StatementBlocksSeparatedByBlankLine : NavCodeAnalysisBase
    {
        private static readonly byte[] OneLinerAllSettings = Utf8(
            """{"StatementBlockSpacing":{"OneLinerMode":"All"}}""");

        private static readonly byte[] ControlFlowOffSettings = Utf8(
            """{"StatementBlockSpacing":{"ControlFlowBefore":false,"ControlFlowAfter":false}}""");

        private static readonly byte[] ScopeLeavingOffSettings = Utf8(
            """{"StatementBlockSpacing":{"ScopeLeavingMode":"Off"}}""");

        private static readonly byte[] ExitOnlySettings = Utf8(
            """{"StatementBlockSpacing":{"ScopeLeavingMode":"ExitOnly"}}""");

        private static readonly byte[] ErrorOnlySettings = Utf8(
            """{"StatementBlockSpacing":{"ScopeLeavingMode":"ErrorOnly"}}""");

        private static readonly byte[] ElseChainRequireBlankSettings = Utf8(
            """{"StatementBlockSpacing":{"ElseChainBeforeMode":"RequireBlank"}}""");

        private static readonly byte[] ControlFlowBeforeOnlySettings = Utf8(
            """{"StatementBlockSpacing":{"ControlFlowBefore":true,"ControlFlowAfter":false}}""");

        private static readonly byte[] ControlFlowAfterOnlySettings = Utf8(
            """{"StatementBlockSpacing":{"ControlFlowBefore":false,"ControlFlowAfter":true}}""");

        // Malformed enum value must be tolerated by ALCopsSettingsProvider: settings fall back to
        // defaults silently instead of throwing. Under defaults ScopeLeavingMode=ExitAndError, so
        // the ExitOnly fixture markers must still fire — proving defaults kicked in.
        private static readonly byte[] MalformedSettings = Utf8(
            """{"StatementBlockSpacing":{"ScopeLeavingMode":"NotAnEnumValue"}}""");

        private AnalyzerTestFixture _fixture;
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

            _fixture = CreateFixture();
        }

        [Test]
        [TestCase("ControlFlowSpacingMissing")]
        [TestCase("ScopeLeavingSpacingMissing")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), testCase);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        [TestCase("ControlFlowSpacingValid")]
        [TestCase("ScopeLeavingSpacingValid")]
        [TestCase("DisabledByDefault")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await LoadFixtureAsync(nameof(NoDiagnostic), testCase);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task HasDiagnosticWithOneLinerAll()
        {
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "OneLinerAll");

            CreateFixture(OneLinerAllSettings)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task NoDiagnosticWithControlFlowDisabled()
        {
            // Reuse the default HasDiagnostic fixture; with ControlFlowBefore/After off, nothing must fire.
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ControlFlowSpacingMissing");

            CreateFixture(ControlFlowOffSettings)
                .NoDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        [TestCase("ExitOnly")]
        [TestCase("ErrorOnly")]
        public async Task NoDiagnosticWithScopeLeavingOff(string testCase)
        {
            // Fixtures contain only exit/Error markers; with ScopeLeavingMode=Off none of them must fire.
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), testCase);

            CreateFixture(ScopeLeavingOffSettings)
                .NoDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task HasDiagnosticWithExitOnly()
        {
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ExitOnly");

            CreateFixture(ExitOnlySettings)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task NoDiagnosticWithExitOnlySuppressesError()
        {
            // Fixture contains Error() markers; under ExitOnly config Error() must not fire.
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ErrorOnly");

            CreateFixture(ExitOnlySettings)
                .NoDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task HasDiagnosticWithErrorOnly()
        {
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ErrorOnly");

            CreateFixture(ErrorOnlySettings)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task NoDiagnosticWithErrorOnlySuppressesExit()
        {
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ExitOnly");

            CreateFixture(ErrorOnlySettings)
                .NoDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task HasDiagnosticWithElseChainRequireBlank()
        {
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ElseChainBlank");

            CreateFixture(ElseChainRequireBlankSettings)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task NoDiagnosticWithElseChainRequireBlank()
        {
            var code = await LoadFixtureAsync(nameof(NoDiagnostic), "ElseChainBlankValid");

            CreateFixture(ElseChainRequireBlankSettings)
                .NoDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task HasDiagnosticWithControlFlowBeforeOnly()
        {
            // Fixture only marks the 'if' keyword; with ControlFlowBefore=true, ControlFlowAfter=false
            // exactly that marker fires. The trailing Message() call (missing blank line after 'end;')
            // is intentionally unmarked because the "after" check is off.
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ControlFlowBeforeOnly");

            CreateFixture(ControlFlowBeforeOnlySettings)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task HasDiagnosticWithControlFlowAfterOnly()
        {
            // Fixture only marks the trailing Message() call; with ControlFlowBefore=false,
            // ControlFlowAfter=true exactly that marker fires. The 'if' keyword is intentionally
            // unmarked because the "before" check is off.
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ControlFlowAfterOnly");

            CreateFixture(ControlFlowAfterOnlySettings)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        [Test]
        public async Task HasDiagnosticWithMalformedJsonFallsBackToDefaults()
        {
            // Regression guard: an invalid enum value in alcops.json must be tolerated by the
            // provider (JsonException catch in DeserializeSettings) so the analyzer sees defaults.
            // Uses the ExitOnly fixture whose exit markers require the default ScopeLeavingMode of
            // ExitAndError to fire.
            var code = await LoadFixtureAsync(nameof(HasDiagnostic), "ExitOnly");

            CreateFixture(MalformedSettings)
                .HasDiagnosticAtAllMarkers(code, DiagnosticIds.StatementBlocksSeparatedByBlankLine);
        }

        private async Task<string> LoadFixtureAsync(string kind, string testCase) =>
            await File.ReadAllTextAsync(Path.Combine(_testCasePath, kind, $"{testCase}.al"))
                .ConfigureAwait(false);

        private AnalyzerTestFixture CreateFixture(byte[]? settingsJson = null) =>
            RoslynFixtureFactory.Create<Analyzers.StatementBlocksSeparatedByBlankLine>(
                new AnalyzerTestFixtureConfig
                {
                    // Physical ruleset enables the disabled-by-default FC0007 rule.
                    RuleSetPath = _ruleSetPath,
                    // Optional alcops.json inject via in-memory file system for config-driven tests.
                    FileSystem = settingsJson is null
                        ? null
                        : new MemoryFileSystem(new Dictionary<string, byte[]>
                        {
                            { "alcops.json", settingsJson }
                        })
                });

        private static byte[] Utf8(string s) => System.Text.Encoding.UTF8.GetBytes(s);
    }
}
