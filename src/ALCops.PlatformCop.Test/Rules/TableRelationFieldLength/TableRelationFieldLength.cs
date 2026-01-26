using ALCops.PlatformCop.CodeFixes;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TableRelationFieldLength : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.TableRelationFieldLength _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TableRelationFieldLength>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TableRelationFieldLength)));
        }

        [Test]
        [TestCase("TableRelationLonger")]
        [TestCase("TableRelationImplicitFieldPrimaryKey")]
        [TestCase("TableRelationImplicitFieldPrimaryKeyWithNamespace")]
        [TestCase("TableExtRelationLonger")]
        [TestCase("TableRelationWithNamespace")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExtRelationLonger"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TableRelationFieldLength);
        }

        [Test]
        [TestCase("TableRelationEqual")]
        [TestCase("TableRelationShorter")]
        [TestCase("TableExtRelationEqual")]
        [TestCase("TableExtRelationShorter")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExtRelationEqual", "TableExtRelationShorter"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TableRelationFieldLength);
        }

        // [Test]
        // [TestCase("ReplaceSetRangeWithSetFilter")]
        // [TestCase("ReplaceSetRangeWithSetFilterUsingRec")]
        // public async Task HasFix(string testCase)
        // {
        //     var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
        //         .ConfigureAwait(false);

        //     var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
        //         .ConfigureAwait(false);

        //     var fixture = RoslynFixtureFactory.Create<TableRelationFieldLengthCodeFix>(
        //         new CodeFixTestFixtureConfig
        //         {
        //             AdditionalAnalyzers = [_analyzer]
        //         });

        //     fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.TableRelationFieldLength);
        // }
    }
}