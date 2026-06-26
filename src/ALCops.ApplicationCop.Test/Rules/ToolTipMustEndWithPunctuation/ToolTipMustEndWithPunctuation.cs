using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class ToolTipMustEndWithPunctuation : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private AnalyzerTestFixture _analysisViewFixture;
        private AnalyzerTestFixture _customExclamationFixture;
        private AnalyzerTestFixture _emptyPunctuationListFixture;
        private AnalyzerTestFixture _invalidPunctuationFixture;
        private AnalyzerTestFixture _customNamesFixture;
        private string _testCasePath;

        private static readonly string[] AnalysisViewTestCases = ["PageAnalysisView"];

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>();
            _analysisViewFixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>(
                TestHelper.CreateAnalysisViewConfig());

            _customExclamationFixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>(
                TestHelper.CreateConfigWithSettings(
                    """
                    {
                        "ToolTipAllowedPunctuations": [
                            {
                                "Character": "!",
                                "Name": "exclamation mark"
                            }
                        ]
                    }
                    """));

            _emptyPunctuationListFixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>(
                TestHelper.CreateConfigWithSettings(
                    """
                    {
                        "ToolTipAllowedPunctuations": []
                    }
                    """));

            _invalidPunctuationFixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>(
                TestHelper.CreateConfigWithSettings(
                    """
                    {
                        "ToolTipAllowedPunctuations": [
                            {
                                "Character": null,
                                "Name": "broken"
                            },
                            {
                                "Character": "",
                                "Name": "empty"
                            },
                            {
                                "Character": "..",
                                "Name": "two chars"
                            }
                        ]
                    }
                    """));

            _customNamesFixture = RoslynFixtureFactory.Create<Analyzers.ToolTipPunctuation>(
                TestHelper.CreateConfigWithSettings(
                    """
                    {
                        "ToolTipAllowedPunctuations": [
                            {
                                "Character": "!",
                                "Name": "exclamation mark"
                            },
                            {
                                "Character": "?",
                                "Name": "question mark"
                            }
                        ]
                    }
                    """));

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(ToolTipMustEndWithPunctuation)));
        }

        [Test]
        [TestCase("PageAction")]
        [TestCase("PageAnalysisView")]
        [TestCase("PageField")]
        [TestCase("TableField")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableField"],
                testCase,
                "13.0",
                "ToolTips on fields in a table object are not supported in versions lower than 13.0."
            );
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipMustEndWithPunctuation);
        }

        [Test]
        [TestCase("PageAction")]
        [TestCase("PageAnalysisView")]
        [TestCase("PageField")]
        [TestCase("TableField")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableField"],
                testCase,
                "13.0",
                "ToolTips on fields in a table object are not supported in versions lower than 13.0."
            );
            SkipTestIfVersionIsTooLow(
                AnalysisViewTestCases,
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = AnalysisViewTestCases.Contains(testCase) ? _analysisViewFixture : _fixture;
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipMustEndWithPunctuation);
        }

        [Test]
        [TestCase("CustomExclamationMissing", "CustomExclamation")]
        [TestCase("PageField", "InvalidPunctuationFallback")]
        [TestCase("PageField", "CustomNames")]
        public async Task HasDiagnosticWithCustomSettings(string testCase, string fixtureName)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = GetFixture(fixtureName);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipMustEndWithPunctuation);
        }

        [Test]
        [TestCase("CustomExclamationAllowed", "CustomExclamation")]
        [TestCase("PageField", "EmptyPunctuationListFallback")]
        public async Task NoDiagnosticWithCustomSettings(string testCase, string fixtureName)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = GetFixture(fixtureName);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.ToolTipMustEndWithPunctuation);
        }

        private AnalyzerTestFixture GetFixture(string fixtureName)
        {
            return fixtureName switch
            {
                "CustomExclamation" => _customExclamationFixture,
                "EmptyPunctuationListFallback" => _emptyPunctuationListFixture,
                "InvalidPunctuationFallback" => _invalidPunctuationFixture,
                "CustomNames" => _customNamesFixture,
                _ => throw new ArgumentOutOfRangeException(nameof(fixtureName), fixtureName, "Unknown fixture name.")
            };
        }
    }
}