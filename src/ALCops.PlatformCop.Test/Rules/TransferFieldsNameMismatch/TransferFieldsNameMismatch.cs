using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TransferFieldsNameMismatch : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

        private static readonly byte[] AppSourceCopWithAffixes = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "mandatoryPrefix": "ABC ",
                "mandatorySuffix": " XYZ",
                "mandatoryAffixes": ["FOO"]
            }
            """);

        private static AnalyzerTestFixture CreateFixtureWithAffixes()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "AppSourceCop.json", AppSourceCopWithAffixes }
            };

            return RoslynFixtureFactory.Create<Analyzers.TransferFieldsSchemaCompatibility>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = new MemoryFileSystem(files)
                });
        }

        private static readonly byte[] AppSourceCopWithCoincidentalAffix = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "mandatoryAffixes": ["MER"]
            }
            """);

        private static AnalyzerTestFixture CreateFixtureWithCoincidentalAffix()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "AppSourceCop.json", AppSourceCopWithCoincidentalAffix }
            };

            return RoslynFixtureFactory.Create<Analyzers.TransferFieldsSchemaCompatibility>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = new MemoryFileSystem(files)
                });
        }

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.TransferFieldsSchemaCompatibility>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(TransferFieldsNameMismatch)));
        }

        [Test]
        [TestCase("InvocationRecWithCodeunit")]
        [TestCase("InvocationRecWithPage")]
        [TestCase("InvocationRecWithTable")]
        [TestCase("InvocationRecWithTablexRec")]
        [TestCase("InvocationSkipFieldsNotMatchingType")]
        [TestCase("InvocationWithInitPrimaryKeyFieldsIsTrue")]
        [TestCase("InvocationWithReturnValue")]
        [TestCase("InvocationWithVarGlobals")]
        [TestCase("InvocationWithVarLocalAndGlobal")]
        [TestCase("InvocationWithVarLocals")]
        [TestCase("InvocationWithVarParam")]
        [TestCase("InvocationWithTableExtension")]
        [TestCase("Invocation_SourceTableObsoleteStatePending")]
        [TestCase("TableExt_Multiple_SameBase")]
        [TestCase("TableExtension")]
        [TestCase("TableExt_NamespaceCasingMismatch")]
        [TestCase("InvocationBareSelfInTableExtension")]
        [TestCase("InvocationThisSelfInTable")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InvocationWithTableExtension", "InvocationBareSelfInTableExtension", "TableExt_Multiple_SameBase", "TableExtension", "TableExtensionTypeWithLength", "TableExt_NamespaceCasingMismatch"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            SkipTestIfVersionIsTooLow(
                ["InvocationThisSelfInTable"],
                testCase,
                "14.0",
                "The 'this' self-reference keyword requires runtime version 14.0 (BC 2024 wave 2).");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }

        [Test]
        [TestCase("BuiltInInvocation")]
        [TestCase("Invocation_ObsoleteStateRemoved")]
        [TestCase("Invocation_Pragma")]
        [TestCase("Invocation_SourceTableObsoleteStateRemoved")]
        [TestCase("Invocation_TargetTableObsoleteStateRemoved")]
        [TestCase("InvocationSkipFieldsNotMatchingType")]
        [TestCase("InvocationWithInitPrimaryKeyFieldsIsFalse")]
        [TestCase("InvocationWithTableExtension")]
        [TestCase("TableExt_ObsoleteStateRemoved")]
        [TestCase("TableExt_Paired_Extension_Pragma")]
        [TestCase("TableExt_Paired_SingleTableExt")]
        [TestCase("TableExt_SourceBaseTableObsoleteStateRemoved")]
        [TestCase("TableExt_TargetBaseTableObsoleteStateRemoved")]
        [TestCase("TableExt_Unpaired")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InvocationWithTableExtension", "TableExt_ObsoleteStateRemoved", "TableExt_Paired_Extension_Pragma", "TableExt_Paired_SingleTableExt", "TableExt_SourceBaseTableObsoleteStateRemoved", "TableExt_TargetBaseTableObsoleteStateRemoved", "TableExt_Unpaired"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }

        [Test]
        [TestCase("Affix_Invocation_CoreNameDiffers")]
        [TestCase("Affix_Invocation_OwnTableFieldsNotStripped")]
        public async Task HasDiagnosticWithAffixes(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["Affix_Invocation_CoreNameDiffers"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateFixtureWithAffixes().HasDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }

        [Test]
        [TestCase("Affix_Invocation_PrefixStripped")]
        [TestCase("Affix_Invocation_SuffixStripped")]
        [TestCase("Affix_Invocation_AffixTrimmed")]
        [TestCase("Affix_TableExt_BothSidesStripped")]
        public async Task NoDiagnosticWithAffixes(string testCase)
        {
            RequireMinimumVersion("13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateFixtureWithAffixes().NoDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }

        [Test]
        [TestCase("Affix_GluedCoincidence_StillFires")]
        public async Task HasDiagnosticWithCoincidentalAffix(string testCase)
        {
            RequireMinimumVersion("13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateFixtureWithCoincidentalAffix().HasDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }

        [Test]
        [TestCase("Affix_GluedCoreCollision_DocumentedLimitation")]
        public async Task NoDiagnosticWithCoincidentalAffix(string testCase)
        {
            RequireMinimumVersion("13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            CreateFixtureWithCoincidentalAffix().NoDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }
    }
}