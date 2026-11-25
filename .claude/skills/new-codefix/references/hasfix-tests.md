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

Every `CodeFixProvider`, `BatchFixer`-based ones included, gets two FixAll tests: a `HasFixAll` case at Document scope and a `HasFixAllInProject` case with two `/*EOD*/` documents at Project scope. `TestCodeFix` only exercises the single-diagnostic path and misses batching regressions, and only a Project-scope test runs the whole-compilation diagnostics the editor's "Fix All in Project" uses. A custom `FixAllProvider` (see `.claude/rules/codefix-development.md` → "Custom FixAllProvider") additionally needs cases where several diagnostics edit a shared ancestor node. Reference implementation: the `UseParenthesisForFunctionCall` test class in `ALCops.FormattingCop.Test`.

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

### HasFixAllInProject

Same layout (`HasFixAll/{TestCaseName}/current.al` + `expected.al`), with the code split into two documents by a line containing exactly `/*EOD*/` and markers in both (see "Multi-document fixtures" in `.claude/rules/testing.md`). Add `using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;` for `FixAllScope`:

```csharp
fixture.TestFixAll(
    currentCode,
    expectedCode,
    DiagnosticIds.{DiagnosticIdConstant},
    equivalenceKey: nameof({CodeFixProviderClassName}),
    scope: FixAllScope.Project);
```
