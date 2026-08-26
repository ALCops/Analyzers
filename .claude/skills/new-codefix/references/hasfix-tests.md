# HasFix and HasFixAll tests

Used by `/new-codefix`. Fixture layout: `Rules/{RuleName}/HasFix/{Case}/current.al` (with `[|...|]` marker) and `expected.al` (no markers); `HasFixAll/{Case}/` likewise with several markers.

## HasFix

Start from the class in `.claude/skills/new-analyzer/references/test-class-template.md` and add:

- `using ALCops.{Cop}.CodeFixes;`
- a shared analyzer instance: `private static readonly Analyzers.{AnalyzerClassName} _analyzer = new();`
- the `HasFix` method:

```csharp
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
```

Note: `HasFix` uses `DiagnosticDescriptors.{Name}` (the full descriptor object), not `DiagnosticIds.{Name}` (the string ID). Both classes live in the analyzer project.

## HasFixAll

Add a dedicated `HasFixAll` test method when the `CodeFixProvider` uses a custom `FixAllProvider` (see `.claude/rules/codefix-development.md` → "Custom FixAllProvider"). This is required for rules where multiple diagnostics can produce edits on a shared ancestor node (e.g. removing multiple parameters from the same signature), because `TestCodeFix` only exercises the single-diagnostic path and misses batching regressions.

Directory layout is identical to `HasFix`: place `current.al` (with multiple `[|...|]` markers, one per expected diagnostic) and `expected.al` under `HasFixAll/{TestCaseName}/`.

```csharp
[Test]
[TestCase("{TestCaseName}")]
public async Task HasFixAll(string testCase)
{
    var currentCode = await File.ReadAllTextAsync(
        Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "current.al"))
        .ConfigureAwait(false);

    var expectedCode = await File.ReadAllTextAsync(
        Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "expected.al"))
        .ConfigureAwait(false);

    var fixture = RoslynFixtureFactory.Create<{CodeFixProviderClassName}>(
        new CodeFixTestFixtureConfig
        {
            AdditionalAnalyzers = [_analyzer]
        });

    fixture.TestFixAll(
        currentCode,
        expectedCode,
        DiagnosticIds.{DiagnosticIdConstant},
        codeFixIndex: 0,
        equivalenceKey: $"{nameof({CodeFixProviderClassName})}.All");
}
```

Key details:

- `TestFixAll` takes the **diagnostic ID string** (`DiagnosticIds.{Name}`), not the descriptor object — unlike `TestCodeFix`.
- `codeFixIndex` selects which registered `CodeAction` to apply. Use `0` unless a rule registers multiple actions in a deterministic order.
- `equivalenceKey` must exactly match the key assigned to the `CodeAction` in the provider. Passing the wrong key silently short-circuits FixAll.
- Prefer at least **two markers** in `current.al` targeting sibling nodes in the same list (`ParameterListSyntax`, `PropertyListSyntax`, etc.). A single-marker FixAll test does not exercise the merge behavior and would pass even with `BatchFixer` on a broken provider.
- Wrap the method in `RequireMinimumVersion(...)` if the underlying rule is version-scoped, same as `HasDiagnostic`.
