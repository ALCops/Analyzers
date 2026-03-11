# Code Patterns — Analyzers, CodeFixes & Tests

## Resource Strings (.resx)

**File:** `src/ALCops.[CopName]/ALCops.[CopName]Analyzers.resx`

```xml
<data name="[DiagnosticName]Title" xml:space="preserve">
  <value>[Short title of the rule]</value>
</data>
<data name="[DiagnosticName]MessageFormat" xml:space="preserve">
  <value>[Message with {0}, {1} placeholders]</value>
</data>
<data name="[DiagnosticName]Description" xml:space="preserve">
  <value>[Longer description explaining why this rule exists]</value>
</data>
```

If a CodeFix is included, also add:
```xml
<data name="[DiagnosticName]ActionTitle" xml:space="preserve">
  <value>ALCops: [Description of the auto-fix action]</value>
</data>
```

## Diagnostic ID

**File:** `src/ALCops.[CopName]/DiagnosticIds.cs`

```csharp
public static readonly string [DiagnosticName] = "[PREFIX][NNNN]";
```

## Diagnostic Descriptor

**File:** `src/ALCops.[CopName]/DiagnosticDescriptors.cs`

```csharp
public static readonly DiagnosticDescriptor [DiagnosticName] = new(
    id: DiagnosticIds.[DiagnosticName],
    title: [CopName]Analyzers.[DiagnosticName]Title,
    messageFormat: [CopName]Analyzers.[DiagnosticName]MessageFormat,
    category: Category.[Category],
    defaultSeverity: DiagnosticSeverity.[Severity],
    isEnabledByDefault: true,  // Set to false for opinionated/opt-in rules
    description: [CopName]Analyzers.[DiagnosticName]Description,
    helpLinkUri: GetHelpUri(DiagnosticIds.[DiagnosticName]));
```

## Scaffold Template (Empty Analyzer)

Creates a compilable analyzer that reports zero diagnostics — used before writing tests (TDD red phase).

**File:** `src/ALCops.[CopName]/Analyzers/[DiagnosticName].cs`

```csharp
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.[CopName].Analyzers;

[DiagnosticAnalyzer]
public sealed class [DiagnosticName] : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.[DiagnosticName]);

    public override void Initialize(AnalysisContext context)
    {
        // TODO: Register analysis action during implementation phase
    }
}
```

## Full Analyzer Template (With Logic)

**File:** `src/ALCops.[CopName]/Analyzers/[DiagnosticName].cs`

```csharp
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.[CopName].Analyzers;

[DiagnosticAnalyzer]
public sealed class [DiagnosticName] : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.[DiagnosticName]);

    // Optional: version gating
    // public override VersionCompatibility SupportedVersions =>
    //     VersionProvider.VersionCompatibility.Fall2024OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.Register[Strategy]Action(
            Analyze[Target],
            EnumProvider.[KindEnum].[KindValue]);

    private static void Analyze[Target]([ContextType] ctx)
    {
        if (ctx.IsObsolete())
            return;

        // Analysis logic here

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.[DiagnosticName],
                location,
                messageArgs));
    }
}
```

### Analysis Strategy Options

| Strategy | Context Type | When to Use |
|----------|-------------|-------------|
| `RegisterSyntaxNodeAction` | `SyntaxNodeAnalysisContext` | Syntax-level patterns (tokens, literals, keywords) |
| `RegisterOperationAction` | `OperationAnalysisContext` | Semantic operations (invocations, assignments) |
| `RegisterSymbolAction` | `SymbolAnalysisContext` | Symbol-level analysis (types, methods, properties) |

### Performance Guidelines

- **Prefer targeted registrations** — register on the most specific syntax/operation/symbol kind possible. Avoid registering on broad kinds that fire thousands of times per file.
- **Avoid semantic model lookups in syntax actions** — `RegisterSyntaxNodeAction` should only inspect syntax. If you need type info or symbol resolution, use `RegisterOperationAction` or `RegisterSymbolAction` instead.
- **Use lazy initialization** for expensive one-time computations:
  ```csharp
  private static readonly Lazy<IReadOnlyDictionary<string, string>> _lookup = new(() =>
      BuildLookup(), LazyThreadSafetyMode.PublicationOnly);
  ```
- **Early return** — check the cheapest conditions first (e.g., `ctx.IsObsolete()`, kind checks) before doing expensive work.

### Passing Data to CodeFix

```csharp
var properties = ImmutableDictionary<string, string>.Empty
    .Add("PropertyName", someValue);

ctx.ReportDiagnostic(
    Diagnostic.Create(
        DiagnosticDescriptors.[DiagnosticName],
        location,
        properties,
        messageArgs));
```

### Configurable Rules (ALCopsSettings)

If the rule needs a configurable threshold, add a property to `ALCopsSettings.cs`:

```csharp
// In src/ALCops.Common/Settings/ALCopsSettings.cs
public int [DiagnosticName]Threshold { get; set; } = [default value];
```

Then load it in the analyzer:

```csharp
private static int LoadThreshold(Compilation compilation)
{
    var settings = ALCopsSettingsProvider.GetSettings(
        compilation.FileSystem?.GetDirectoryPath());
    return settings.[DiagnosticName]Threshold;
}
```

Users configure via `alcops.json`:
```json
{
  "[camelCaseDiagnosticName]Threshold": 10
}
```

## CodeFix Class Template

**File:** `src/ALCops.[CopName]/CodeFixes/[DiagnosticName].cs`

```csharp
using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.[CopName].CodeFixes;

[CodeFixProvider(nameof([DiagnosticName]CodeFixProvider))]
public sealed class [DiagnosticName]CodeFixProvider : CodeFixProvider
{
    // Use conditional compilation for the properties class
#if NETSTANDARD2_1
    private sealed class CodeFixProperties
    {
        // Properties extracted from diagnostic
        private CodeFixProperties(/* params */) { }
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties) { /* ... */ }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(/* params */)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties) { /* ... */ }
    }
#endif

    private class [DiagnosticName]CodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public [DiagnosticName]CodeAction(
            string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey,
            bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.[DiagnosticName].Id);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        // Implement fix registration
    }
}
```

## Common Imports Reference

```csharp
// Analyzer
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

// CodeFix (additional)
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

// Settings (if configurable)
using ALCops.Common.Settings;
```

---

## Test File Structure

```
src/ALCops.[CopName].Test/
  Rules/
    [DiagnosticName]/
      [DiagnosticName].cs          ← Test class
      HasDiagnostic/
        [TestCase1].al             ← AL code that SHOULD trigger the diagnostic
        [TestCase2].al
      NoDiagnostic/
        [TestCase1].al             ← AL code that should NOT trigger the diagnostic
        [TestCase2].al
      HasFix/                      ← Only if CodeFix is planned
        [TestCase1]/
          current.al               ← Code before fix
          expected.al              ← Code after fix
```

## Test Class Template

```csharp
using ALCops.[CopName].CodeFixes;  // Only if CodeFix exists
using RoslynTestKit;

namespace ALCops.[CopName].Test;

public class [DiagnosticName] : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private static readonly Analyzers.[DiagnosticName] _analyzer = new();
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _fixture = RoslynFixtureFactory.Create<Analyzers.[DiagnosticName]>();
        _testCasePath = Path.Combine(
            Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
            Path.Combine("Rules", nameof([DiagnosticName])));
    }

    [Test]
    [TestCase("[TestCase1]")]
    [TestCase("[TestCase2]")]
    public async Task HasDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(
            Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.[DiagnosticName]);
    }

    [Test]
    [TestCase("[TestCase1]")]
    [TestCase("[TestCase2]")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(
            Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.[DiagnosticName]);
    }

    // Include ONLY if CodeFix is planned
    [Test]
    [TestCase("[TestCase1]")]
    public async Task HasFix(string testCase)
    {
        var currentCode = await File.ReadAllTextAsync(
            Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
            .ConfigureAwait(false);

        var expectedCode = await File.ReadAllTextAsync(
            Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
            .ConfigureAwait(false);

        var fixture = RoslynFixtureFactory.Create<[CodeFixClassName]>(
            new CodeFixTestFixtureConfig
            {
                AdditionalAnalyzers = [_analyzer]
            });

        fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.[DiagnosticName]);
    }
}
```

## AL Test File Marker Syntax

Use `[|...|]` markers to indicate the exact span where the diagnostic is expected.

### HasDiagnostic example

```al
codeunit 50100 "My Codeunit"
{
    procedure MyProcedure()
    var
        MyVar: Integer;
    begin
        [|problematic code here|];
    end;
}
```

### NoDiagnostic example

Still use `[|...|]` markers — the test asserts NO diagnostic fires at those locations:

```al
codeunit 50100 "My Codeunit"
{
    procedure MyProcedure()
    var
        MyVar: Integer;
    begin
        [|valid code here|];
    end;
}
```

### HasFix example

**`current.al`** — Code before the fix (with marker at diagnostic location):
```al
codeunit 50100 MyCodeunit
{
    var
        MyCodeunit: Codeunit [|50100|];
}
```

**`expected.al`** — Code after the fix (no markers):
```al
codeunit 50100 MyCodeunit
{
    var
        MyCodeunit: Codeunit MyCodeunit;
}
```

## Build & Test Commands

```bash
# Build the analyzer project
dotnet build src/ALCops.[CopName]/ALCops.[CopName].csproj

# Build the test project
dotnet build src/ALCops.[CopName].Test/ALCops.[CopName].Test.csproj

# Run only the tests for the new diagnostic
dotnet test src/ALCops.[CopName].Test/ALCops.[CopName].Test.csproj --filter "FullyQualifiedName~[DiagnosticName]"

# Run ALL tests for the cop (regression check)
dotnet test src/ALCops.[CopName].Test/ALCops.[CopName].Test.csproj
```

## Key Assertion Methods

| Method | Purpose |
|--------|---------|
| `_fixture.HasDiagnosticAtAllMarkers(code, diagnosticId)` | Assert diagnostic fires at every `[│...\│]` marker |
| `_fixture.NoDiagnosticAtAllMarkers(code, diagnosticId)` | Assert NO diagnostic fires at any `[│...\│]` marker |
| `fixture.TestCodeFix(current, expected, descriptor)` | Assert CodeFix transforms current into expected |

## Version Gating in Tests

Use `SkipTestIfVersionIsTooLow()` when the diagnostic requires a minimum BC version:

```csharp
[Test]
public async Task HasDiagnostic(string testCase)
{
    SkipTestIfVersionIsTooLow(..., "14.0");
    // ... rest of test
}
```
