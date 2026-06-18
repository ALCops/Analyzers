---
applyTo: 'src/*.Test/**'
---

# Testing Guide for ALCops Analyzers

This project uses **NUnit 4.1.0** with **ALCops.RoslynTestKit 0.4.1** to test AL code analyzers for Business Central. All test projects target **net8.0**.

## Project Structure

6 analyzer/test project pairs (see `project-overview.instructions.md` for full solution layout). Each test project follows the namespace pattern `ALCops.{Cop}.Test`.

## Directory Layout per Rule

Each rule has its own directory under `Rules/` in the test project:

```
src/ALCops.{Cop}.Test/
├── AssemblyInfo.cs                          # [assembly: Parallelizable(ParallelScope.All)]
├── ALCops.{Cop}.Test.csproj
└── Rules/
    └── {RuleName}/
        ├── {RuleName}.cs                    # Test class (same name as directory)
        ├── HasDiagnostic/
        │   ├── SomeViolation.al             # AL code that SHOULD trigger the diagnostic
        │   └── AnotherViolation.al
        ├── NoDiagnostic/
        │   ├── CorrectUsage.al              # AL code that should NOT trigger the diagnostic
        │   └── AnotherCorrectUsage.al
        └── HasFix/                          # Only when a CodeFixProvider exists
            └── {TestCaseName}/
                ├── current.al               # Code before the fix
                └── expected.al              # Code after the fix
```

## Test Class Template

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

### Key points

- **Base class**: `NavCodeAnalysisBase` (from ALCops.RoslynTestKit). Provides `SkipTestIfVersionIsTooLow()`.
- **Fixture creation**: `RoslynFixtureFactory.Create<Analyzers.{AnalyzerClassName}>()` returns an `AnalyzerTestFixture`.
- **NUnit attributes**: `[SetUp]` on setup, `[Test]` + `[TestCase("name")]` on test methods. NUnit is globally imported via `<Using Include="NUnit.Framework" />` in the csproj, so no `using NUnit.Framework;` needed.
- **Only `using RoslynTestKit;`** is required at the top of the file. Add `using ALCops.{Cop}.CodeFixes;` only if testing a code fix.
- **Async tests**: All test methods are `async Task`, not `void`.
- **File loading**: Uses `File.ReadAllTextAsync` with `.ConfigureAwait(false)`.
- **The test class name matches the directory name**, not necessarily the analyzer class name. For example, the directory `NotBlankNotAllowedOnPrimaryKeyField` might test analyzer class `NotBlankOnPrimaryKeyField`.

## Diagnostic Marker Syntax in .al Files

Markers use `[|...|]` to delimit the exact span where a diagnostic is expected (or verified absent).

### HasDiagnostic files: markers indicate expected diagnostic locations

```al
codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        if [|MyTable.Count() > 1|] then;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }
}
```

Multiple markers per file are valid:

```al
codeunit 50101 "My Codeunit"
{
    var
        MyCodeunit: Codeunit [|50100|];
        MyPage: Page [|50100|];
}
```

### NoDiagnostic files: markers indicate locations verified clean

```al
codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        if [|MyTable.Count() = 2|] then;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }
}
```

### HasFix files: no markers, uses current/expected pairs

- `HasFix/{TestCaseName}/current.al` has `[|...|]` markers showing where the diagnostic occurs.
- `HasFix/{TestCaseName}/expected.al` has the corrected code with no markers.

## Assertion Methods

| Method | Purpose |
|---|---|
| `_fixture.HasDiagnosticAtAllMarkers(code, diagnosticId)` | Assert diagnostic reported at every `[|...|]` marker |
| `_fixture.NoDiagnosticAtAllMarkers(code, diagnosticId)` | Assert NO diagnostic at any `[|...|]` marker |
| `fixture.TestCodeFix(currentCode, expectedCode, diagnosticDescriptor)` | Assert code fix transforms current into expected |

## Testing Code Fixes

When the analyzer has a `CodeFixProvider`, add a `HasFix` test method:

```csharp
using ALCops.{Cop}.CodeFixes;
using RoslynTestKit;

namespace ALCops.{Cop}.Test
{
    public class {RuleName} : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.{AnalyzerClassName} _analyzer = new();
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

        // ... HasDiagnostic and NoDiagnostic methods ...

        [Test]
        [TestCase("{TestCaseName}")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<{CodeFixProviderClassName}>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.{DescriptorName});
        }
    }
}
```

Note: `HasFix` uses `DiagnosticDescriptors.{Name}` (the full descriptor object), not `DiagnosticIds.{Name}` (the string ID). Both classes live in the analyzer project.

## Version-Conditional Test Skipping

### Skipping an entire test method (net8.0-only rules)

When a rule is entirely net8.0-only (e.g., it depends on SDK APIs absent in netstandard2.1), use `RequireMinimumVersion` at the top of each test method. This skips ALL test cases when the loaded SDK version is too low, with no arrays to maintain:

```csharp
[Test]
[TestCase("LocalLabel")]
[TestCase("GlobalLabel")]
[TestCase("TableFieldCaption")]
public async Task HasDiagnostic(string testCase)
{
    RequireMinimumVersion("16.0",
        "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

    var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
        .ConfigureAwait(false);

    _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.{DiagnosticIdConstant});
}
```

Version 16.0 corresponds to the net8.0 SDK. Adding new `[TestCase]` attributes requires no other changes.

### Skipping specific test cases

When only SOME test cases require a minimum version (e.g., a specific AL syntax feature), use `SkipTestIfVersionIsTooLow` with an explicit list of affected test cases:

```csharp
[Test]
[TestCase("ConditionalExpressionNested")]
[TestCase("RegularCase")]
public async Task HasDiagnostic(string testCase)
{
    SkipTestIfVersionIsTooLow(
        ["ConditionalExpressionNested"],  // only these test cases are skipped
        testCase,                          // current test case
        "14.0",                            // minimum version
        "This test requires .NET 8 or higher due to Conditional Expressions.");  // reason (optional)

    var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
        .ConfigureAwait(false);

    _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.{DiagnosticIdConstant});
}
```

**Prefer `RequireMinimumVersion` over `SkipTestIfVersionIsTooLow`** when all test cases in a method share the same version requirement. The array-based approach requires maintaining duplicate lists that must stay in sync with `[TestCase]` attributes.

### Why `#if` pragmas don't work in test projects

Test projects always compile as `net8.0`, so `NETSTANDARD2_1` is never defined. The version difference is a runtime property of which SDK DLL gets loaded (CI tests against both netstandard2.1 and net8.0 analyzer binaries), so it must be a runtime check.

## Testing Analyzers with File System Dependencies

Analyzers that access `Compilation.FileSystem` (e.g., to read XLIFF translation files) need a virtual file system during tests. Use `MemoryFileSystem` (SDK built-in) injected via `AnalyzerTestFixtureConfig.FileSystem` (requires RoslynTestKit 1.1.0+):

```csharp
using Microsoft.Dynamics.Nav.CodeAnalysis;

private static readonly byte[] EmptyXliffContent = System.Text.Encoding.UTF8.GetBytes(
    """
    <?xml version="1.0" encoding="utf-8"?>
    <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
      <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
        <body><group id="body"></group></body>
      </file>
    </xliff>
    """);

private static AnalyzerTestFixture CreateFixtureWithEmptyXliff()
{
    var files = new Dictionary<string, byte[]>
    {
        { "Translations/TestApp.da-DK.xlf", EmptyXliffContent }
    };
    var fileSystem = new MemoryFileSystem(files);

    return RoslynFixtureFactory.Create<Analyzers.MyAnalyzer>(
        new AnalyzerTestFixtureConfig
        {
            FileSystem = fileSystem
        });
}
```

Key details about `MemoryFileSystem`:
- Keys use **forward slashes** (e.g., `"Translations/TestApp.da-DK.xlf"`)
- `GetDirectoryPath()` always returns `""` (empty string)
- Accepts `Dictionary<string, byte[]>` in constructor

## Custom Compilation Options

For tests that need non-default compilation settings (e.g., OnPrem target):

```csharp
[TestCase("HttpClientHandler")]
public async Task NoDiagnosticWithTargetOnPrem(string testCase)
{
    var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
        .ConfigureAwait(false);

    var fixture = RoslynFixtureFactory.Create<Analyzers.{AnalyzerClassName}>(
        new AnalyzerTestFixtureConfig
        {
            CompilationOptions = new CompilationOptions(target: CompilationTarget.OnPrem)
        });

    fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.{DiagnosticIdConstant});
}
```

This requires `using Microsoft.Dynamics.Nav.CodeAnalysis;` for `CompilationOptions` and `CompilationTarget`.

## Testing rules that are `isEnabledByDefault: false`

Rules declared with `isEnabledByDefault: false` never run in tests unless a ruleset explicitly enables them. Inject a co-located ruleset JSON fixture via `AnalyzerTestFixtureConfig.RuleSetPath` (requires RoslynTestKit 1.4.0+, which loads the ruleset and applies it to the compilation's diagnostic options).

Add a `{RuleName}.ruleset.json` next to the test class in its `Rules/{RuleName}/` folder:

```json
{
  "name": "Enable AC0013",
  "description": "Enables AC0013 for tests.",
  "rules": [ { "id": "AC0013", "action": "Info" } ]
}
```

Set `action` to the rule's default severity (`Info`, `Warning`, etc.) to enable it. Then wire it in `Setup` (compute `_testCasePath` **before** creating the fixture, since the path feeds `RuleSetPath`):

```csharp
[SetUp]
public void Setup()
{
    _testCasePath = Path.Combine(
        Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
        Path.Combine("Rules", nameof(MyRule)));

    _fixture = RoslynFixtureFactory.Create<Analyzers.MyRule>(
        new AnalyzerTestFixtureConfig
        {
            RuleSetPath = Path.Combine(_testCasePath, $"{nameof(MyRule)}.ruleset.json")
        });
}
```

The ruleset JSON resolves via the same source-tree absolute path as the `.al` fixtures, so no `CopyToOutputDirectory` is required. For code-fix tests, pass `RuleSetPath` on `CodeFixTestFixtureConfig` too.

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Test class name | Matches rule directory name | `UseQueryOrFindWithNextInsteadOfCount` |
| Test method (positive) | `HasDiagnostic` | Always this exact name |
| Test method (negative) | `NoDiagnostic` | Always this exact name |
| Test method (code fix) | `HasFix` | Always this exact name |
| TestCase parameter | PascalCase, describes the scenario | `"RecordCountEqualsOne"` |
| .al fixture file | `{TestCaseName}.al`, matching the TestCase value | `RecordCountEqualsOne.al` |
| HasFix directory | `{TestCaseName}/current.al` + `expected.al` | `GlobalVariable/current.al` |

## Writing AL Fixture Files

### Rules for .al fixtures

1. Use object IDs in the 50000-50199 range (test object range).
2. Include all dependent objects in the same file (tables, enums, codeunits needed by the test).
3. Keep fixtures minimal: only include code relevant to the rule being tested.
4. Place `[|...|]` markers precisely around the syntax node the analyzer targets.
5. Every `HasDiagnostic` fixture must have at least one marker. Every `NoDiagnostic` fixture must also have markers (on the same kind of syntax node, but in a valid scenario).

### Typical fixture structure

```al
codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        // The marker wraps the exact expression/statement the analyzer flags
        [|SomeCodeThatTriggersTheDiagnostic|];
    end;
}

// Supporting objects defined in the same file
table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
```

## Running Tests

```bash
# Run all tests in a specific test project
dotnet test src/ALCops.LinterCop.Test/

# Run tests for a specific rule
dotnet test src/ALCops.LinterCop.Test/ --filter "FullyQualifiedName~UseQueryOrFindWithNextInsteadOfCount"

# Run only HasDiagnostic tests for a rule
dotnet test src/ALCops.LinterCop.Test/ --filter "FullyQualifiedName~UseQueryOrFindWithNextInsteadOfCount.HasDiagnostic"

# Run a specific test case
dotnet test src/ALCops.LinterCop.Test/ --filter "FullyQualifiedName~UseQueryOrFindWithNextInsteadOfCount.HasDiagnostic(\"RecordCountEqualsOne\")"

# Run all test projects
dotnet test ALCops.sln
```

Tests run in parallel across assemblies (`[assembly: Parallelizable(ParallelScope.All)]` in `AssemblyInfo.cs`).

## Step-by-Step: Adding Tests for a New Rule

1. **Create directory structure**: `src/ALCops.{Cop}.Test/Rules/{RuleName}/` with `{RuleName}.cs`, `HasDiagnostic/`, and `NoDiagnostic/` subdirectories
2. **Write .al fixture files**: `HasDiagnostic/*.al` (code triggering diagnostic, with `[|...|]` markers) and `NoDiagnostic/*.al` (valid code, also with markers)
3. **Write test class**: Follow the Test Class Template above using `RoslynFixtureFactory.Create<Analyzers.{AnalyzerClassName}>()`
4. **Run**: `dotnet test src/ALCops.{Cop}.Test/ --filter "FullyQualifiedName~{RuleName}"`

## Common Mistakes to Avoid

- **Forgetting markers in NoDiagnostic files.** Both HasDiagnostic and NoDiagnostic .al files need `[|...|]` markers. The difference is whether a diagnostic is expected at those locations.
- **Using `DiagnosticDescriptors` instead of `DiagnosticIds` for HasDiagnostic/NoDiagnostic.** `HasDiagnosticAtAllMarkers` and `NoDiagnosticAtAllMarkers` take a string diagnostic ID. `TestCodeFix` takes a `DiagnosticDescriptor` object.
- **Mismatching TestCase name and .al filename.** The `[TestCase("Foo")]` value must exactly match `Foo.al` in the corresponding subdirectory.
- **Missing supporting objects.** If your AL code references a table, enum, or other object, define it in the same .al file.
- **Not using `async Task` for test methods.** All test methods must be `async Task`, not `void` or synchronous.
- **Forgetting `.ConfigureAwait(false)`** on `ReadAllTextAsync` calls.
