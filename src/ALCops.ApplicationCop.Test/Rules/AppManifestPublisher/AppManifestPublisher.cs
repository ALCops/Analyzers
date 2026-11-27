using ALCops.ApplicationCop.Analyzers;

namespace ALCops.ApplicationCop.Test;

public class AppManifestPublisher
{
    [Test]
    public async Task ConfiguredPatternReportsDifferentPublisher()
    {
        var diagnostics = await AppManifestRuleTestHelper.AnalyzeAsync<Analyzers.AppManifestPublisher>(
            DiagnosticIds.AppManifestPublisher,
            AppManifestRuleTestHelper.ReadFixture(nameof(AppManifestPublisher), "HasDiagnostic"),
            AppManifestRuleTestHelper.CreateManifest(publisher: "Other"),
            """{"Manifest":{"Publisher":{"AllowPattern":"^Contoso Ltd\\.$"}}}""");

        AppManifestRuleTestHelper.AssertNoAnalyzerException(diagnostics);
        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { DiagnosticIds.AppManifestPublisher }));
    }

    [Test]
    public async Task MatchingPatternDoesNotReport()
    {
        var diagnostics = await AppManifestRuleTestHelper.AnalyzeAsync<Analyzers.AppManifestPublisher>(
            DiagnosticIds.AppManifestPublisher,
            AppManifestRuleTestHelper.ReadFixture(nameof(AppManifestPublisher), "NoDiagnostic"),
            AppManifestRuleTestHelper.CreateManifest(),
            """{"Manifest":{"Publisher":{"AllowPattern":"^Contoso Ltd\\.$"}}}""");

        AppManifestRuleTestHelper.AssertNoAnalyzerException(diagnostics);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task DisallowedPatternReportsMatchingPublisher()
    {
        var diagnostics = await AppManifestRuleTestHelper.AnalyzeAsync<Analyzers.AppManifestPublisher>(
            DiagnosticIds.AppManifestPublisher,
            AppManifestRuleTestHelper.ReadFixture(nameof(AppManifestPublisher), "HasDiagnostic"),
            AppManifestRuleTestHelper.CreateManifest(publisher: "Contoso"),
            """{"Manifest":{"Publisher":{"DisallowPattern":"^Contoso$"}}}""");

        AppManifestRuleTestHelper.AssertNoAnalyzerException(diagnostics);
        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { DiagnosticIds.AppManifestPublisher }));
        Assert.That(diagnostics.Single().GetMessage(), Does.Contain("matches DisallowPattern"));
    }

    [Test]
    public async Task NoPatternDoesNotImposeAnExpectedPublisher()
    {
        var diagnostics = await AppManifestRuleTestHelper.AnalyzeAsync<Analyzers.AppManifestPublisher>(
            DiagnosticIds.AppManifestPublisher,
            AppManifestRuleTestHelper.ReadFixture(nameof(AppManifestPublisher), "NoDiagnostic"),
            AppManifestRuleTestHelper.CreateManifest(publisher: "Any publisher"));

        AppManifestRuleTestHelper.AssertNoAnalyzerException(diagnostics);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task InvalidPatternIsReported()
    {
        var diagnostics = await AppManifestRuleTestHelper.AnalyzeAsync<Analyzers.AppManifestPublisher>(
            DiagnosticIds.AppManifestPublisher,
            AppManifestRuleTestHelper.ReadFixture(nameof(AppManifestPublisher), "HasDiagnostic"),
            AppManifestRuleTestHelper.CreateManifest(publisher: "Contoso"),
            """{"Manifest":{"Publisher":{"AllowPattern":"["}}}""");

        AppManifestRuleTestHelper.AssertNoAnalyzerException(diagnostics);
        Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { DiagnosticIds.AppManifestPublisher }));
        Assert.That(diagnostics.Single().GetMessage(), Does.Contain("invalid configured regular expression"));
    }

    [Test]
    public async Task RuleIsDisabledUnlessEnabledByRuleset()
    {
        var diagnostics = await AppManifestRuleTestHelper.AnalyzeAsync<Analyzers.AppManifestPublisher>(
            DiagnosticIds.AppManifestPublisher,
            AppManifestRuleTestHelper.ReadFixture(nameof(AppManifestPublisher), "NoDiagnostic"),
            AppManifestRuleTestHelper.CreateManifest(publisher: "Other"),
            """{"Manifest":{"Publisher":{"AllowPattern":"^Contoso"}}}""",
            enableRule: false);

        Assert.That(diagnostics, Is.Empty);
    }
}
