# Test class template

Used by `/new-analyzer` when creating `src/ALCops.{Cop}.Test/Rules/{RuleName}/{RuleName}.cs`. Conventions (marker syntax, fixture rules, version skipping) are in `.claude/rules/testing.md`.

Every test class follows this exact pattern:

```csharp
using RoslynTestKit;

namespace ALCops.{Cop}.Test
{
    public class {RuleName} : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.{AnalyzerClassName}>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof({RuleName})));
        }

        [Test]
        [TestCase("{TestCaseName1}")]
        [TestCase("{TestCaseName2}")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.{DiagnosticIdConstant});
        }

        [Test]
        [TestCase("{CleanCaseName1}")]
        [TestCase("{CleanCaseName2}")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.{DiagnosticIdConstant});
        }
    }
}
```

Key points:

- **Base class**: `NavCodeAnalysisBase` (from ALCops.RoslynTestKit). Provides `SkipTestIfVersionIsTooLow()`.
- **Fixture creation**: `RoslynFixtureFactory.Create<Analyzers.{AnalyzerClassName}>()` returns an `AnalyzerTestFixture`.
- **NUnit attributes**: `[SetUp]` on setup, `[Test]` + `[TestCase("name")]` on test methods. NUnit is globally imported via `<Using Include="NUnit.Framework" />` in the csproj, so no `using NUnit.Framework;` needed.
- **Only `using RoslynTestKit;`** is required at the top of the file. Add `using ALCops.{Cop}.CodeFixes;` only if testing a code fix.
- **Async tests**: All test methods are `async Task`, not `void`.
- **File loading**: Uses `File.ReadAllTextAsync` with `.ConfigureAwait(false)`.
- **The test class name matches the directory name**, not necessarily the analyzer class name. For example, the directory `NotBlankNotAllowedOnPrimaryKeyField` might test analyzer class `NotBlankOnPrimaryKeyField`.
