using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class DuplicatePropertyBetweenActionAndRunObject : NavCodeAnalysisBase
    {
        private const string RuntimeGateReason =
            "AC0036 is gated on runtime 18.0; the test harness compiles at the SDK's current release.";

        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.DuplicatePropertyBetweenActionAndRunObject>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(DuplicatePropertyBetweenActionAndRunObject));
        }

        [Test]
        [TestCase("PageTargetCaption")]
        [TestCase("PageTargetToolTip")]
        [TestCase("PageTargetAbout")]
        [TestCase("PageTargetAllProperties")]
        [TestCase("ReportTargetCaption")]
        [TestCase("ReportTargetToolTip")]
        [TestCase("ReportTargetRequestPageAbout")]
        [TestCase("RequestPageAction")]
        [TestCase("PageExtensionAddedAction")]
        [TestCase("PageExtensionModifyCaption")]
        [TestCase("NamespacedObjects")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.DuplicatePropertyBetweenActionAndRunObject);
        }

        [Test]
        [TestCase("DifferentValue")]
        [TestCase("DifferentCase")]
        [TestCase("EmptyValue")]
        [TestCase("TargetWithoutProperty")]
        [TestCase("CodeunitTarget")]
        [TestCase("ObsoleteAction")]
        [TestCase("ObsoleteTarget")]
        [TestCase("ActionCaptionML")]
        [TestCase("TargetCaptionML")]
        [TestCase("ActionGroup")]
        [TestCase("ActionRef")]
        [TestCase("ModifyWhenBaseActionHasCaption")]
        [TestCase("ModifyWhenBaseActionHasCaptionML")]
        [TestCase("ModifyWhenBaseActionHasNoRunObject")]
        [TestCase("PageCustomizationModify")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.DuplicatePropertyBetweenActionAndRunObject);
        }

        // The RunObject target lives in a second workspace project. The workspace binds it as a source
        // symbol, so a report's request page is reachable there too; an app consumed as a symbol package
        // would bind reference symbols instead, which this harness cannot produce. NoException catches a
        // crash on the foreign symbol, which the marker assertion would hide behind the filtered AD0001.
        [Test]
        [TestCase("DependencyPage")]
        [TestCase("DependencyReport")]
        [TestCase("DependencyReportRequestPageAbout")]
        public async Task HasDiagnosticWithDependencyProject(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var fixture = await CreateFixtureWithDependencyProject(testCase).ConfigureAwait(false);
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            fixture.NoException(code.Replace("[|", "").Replace("|]", ""));
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.DuplicatePropertyBetweenActionAndRunObject);
        }

        // An explicit runtime below the gate: the SDK's VersionChecker then reads the runtime from the
        // parse options instead of falling back to its current release, and the rule must stay silent.
        [Test]
        [TestCase("RuntimeBelowGate")]
        public async Task NoDiagnosticBelowRuntimeGate(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<Analyzers.DuplicatePropertyBetweenActionAndRunObject>(
                new AnalyzerTestFixtureConfig
                {
                    ParseOptions = new ParseOptions(new Version(17, 0))
                });

            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.DuplicatePropertyBetweenActionAndRunObject);
        }

        private async Task<AnalyzerTestFixture> CreateFixtureWithDependencyProject(string testCase)
        {
            var dependencyCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, "DependencyProject", $"{testCase}.al"))
                .ConfigureAwait(false);

            return RoslynFixtureFactory.Create<Analyzers.DuplicatePropertyBetweenActionAndRunObject>(
                new AnalyzerTestFixtureConfig
                {
                    DependencyProjects = [new DependencyProject("DependencyApp", dependencyCode)]
                });
        }
    }
}
