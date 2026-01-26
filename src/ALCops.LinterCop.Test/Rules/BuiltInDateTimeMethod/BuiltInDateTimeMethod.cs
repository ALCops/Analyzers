using ALCops.LinterCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class BuiltInDateTimeMethod : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.BuiltInDateTimeMethod _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.BuiltInDateTimeMethod>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(BuiltInDateTimeMethod)));
        }

        [Test]
        [TestCase("Date2DMY")]
        [TestCase("Date2DWY")]
        [TestCase("DT2Date")]
        [TestCase("DT2Time")]
        [TestCase("FormatHour")]
        [TestCase("FormatMillisecond")]
        [TestCase("FormatMinute")]
        [TestCase("FormatSecond")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.BuiltInDateTimeMethod);
        }

        // [Test]
        // public async Task NoDiagnostic(string testCase)
        // {
        //     var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
        //         .ConfigureAwait(false);

        //     _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.BuiltInDateTimeMethod);
        // }

        [Test]
        [TestCase("Date2DMY_Day")]
        [TestCase("Date2DMY_Month")]
        [TestCase("Date2DMY_Year")]
        [TestCase("Date2DWY_DayOfWeek")]
        [TestCase("Date2DWY_WeekNo")]
        [TestCase("DT2Date")]
        [TestCase("DT2Time")]
        [TestCase("Format_Hour")]
        [TestCase("Format_Minute")]
        [TestCase("Format_Second")]
        [TestCase("Format_Millisecond")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<BuiltInDateTimeMethodCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.BuiltInDateTimeMethod);
        }
    }
}