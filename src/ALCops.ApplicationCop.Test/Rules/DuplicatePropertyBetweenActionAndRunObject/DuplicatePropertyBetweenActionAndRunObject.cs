using ALCops.ApplicationCop.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class DuplicatePropertyBetweenActionAndRunObject : NavCodeAnalysisBase
    {
        private const string RuntimeGateReason =
            "AC0036 is gated on runtime 18.0; the test harness compiles at the SDK's current release.";

        private static readonly Analyzers.DuplicatePropertyBetweenActionAndRunObject _analyzer = new();
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
        [TestCase("PageExtensionModifyToolTipWhenBaseHasCaption")]
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
        [TestCase("QueryTarget")]
        [TestCase("ObsoleteAction")]
        [TestCase("ObsoleteTarget")]
        [TestCase("ObsoletePage")]
        [TestCase("ObsoleteBaseAction")]
        [TestCase("ActionCaptionML")]
        [TestCase("TargetCaptionML")]
        [TestCase("ActionGroup")]
        [TestCase("ActionRef")]
        [TestCase("ModifyWhenBaseActionHasCaption")]
        [TestCase("ModifyWhenBaseActionHasCaptionML")]
        [TestCase("ModifyWhenBaseActionHasToolTipML")]
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

        [Test]
        [TestCase("PageTargetCaption")]
        [TestCase("PageTargetToolTip")]
        [TestCase("ReportTargetCaption")]
        [TestCase("ReportTargetRequestPageAbout")]
        [TestCase("PageExtensionAddedAction")]
        [TestCase("PageExtensionModifyCaption")]
        [TestCase("PageExtensionModifyKeepsOtherProperty")]
        [TestCase("PageExtensionModifyKeepsTrigger")]
        [TestCase("TwoDuplicatedProperties")]
        [TestCase("PrecedingComment")]
        [TestCase("DirectiveOnFollowingProperty")]
        [TestCase("InactiveConditionalOnFollowingProperty")]
        [TestCase("NamespacedObjects")]
        public async Task HasFix(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            CreateCodeFixFixture().TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.DuplicatePropertyBetweenActionAndRunObject);
        }

        [Test]
        [TestCase("TwoPropertiesOneAction")]
        [TestCase("AllFourProperties")]
        [TestCase("TwoActionsInDocument")]
        [TestCase("ModifyEmptiedByTwoFixes")]
        [TestCase("MixedDirectiveAndCleanProperties")]
        public async Task HasFixAll(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFixAll), testCase, "expected.al"))
                .ConfigureAwait(false);

            CreateCodeFixFixture().TestFixAll(
                currentCode,
                expectedCode,
                DiagnosticIds.DuplicatePropertyBetweenActionAndRunObject,
                codeFixIndex: 0,
                equivalenceKey: nameof(DuplicatePropertyBetweenActionAndRunObjectCodeFixProvider));
        }

        [Test]
        [TestCase("ActiveConditionalDirective")]
        [TestCase("InactiveConditionalOnPrecedingProperty")]
        [TestCase("PragmaDirective")]
        [TestCase("RegionDirective")]
        [TestCase("ModifyBlockDirectiveBeforeClosingBrace")]
        public async Task NoFix(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoFix), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateCodeFixFixture().NoCodeFix(code, DiagnosticDescriptors.DuplicatePropertyBetweenActionAndRunObject);
        }

        // NoCodeFix synthesises a diagnostic at the marker when the analyzer reports none, so a NoFix
        // fixture would also pass when AC0036 never fires. This proves the bail-out is what is tested.
        [Test]
        [TestCase("ActiveConditionalDirective")]
        [TestCase("InactiveConditionalOnPrecedingProperty")]
        [TestCase("PragmaDirective")]
        [TestCase("RegionDirective")]
        [TestCase("ModifyBlockDirectiveBeforeClosingBrace")]
        public async Task NoFixStillReported(string testCase)
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoFix), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.DuplicatePropertyBetweenActionAndRunObject);
        }

        // The code fix harness compares expected.al as text and never compiles it. The analyzer fixture
        // throws on a compile error, so this proves every fixed output, an emptied modify block included,
        // is valid AL.
        [Test]
        public async Task FixedCodeCompiles()
        {
            RequireMinimumVersion("18.0", RuntimeGateReason);

            var expectedFiles = new[] { nameof(HasFix), nameof(HasFixAll) }
                .SelectMany(folder => Directory.GetFiles(Path.Combine(_testCasePath, folder), "expected.al", SearchOption.AllDirectories))
                .ToList();

            Assert.That(expectedFiles, Is.Not.Empty);

            foreach (var file in expectedFiles)
            {
                var code = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                _fixture.NoException(code);
            }
        }

        private static CodeFixTestFixture CreateCodeFixFixture() =>
            RoslynFixtureFactory.Create<DuplicatePropertyBetweenActionAndRunObjectCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

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
