using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TransferFieldsNameMismatch : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private string _testCasePath;

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
        [TestCase("TableExt_Multiple_SameBase")]
        [TestCase("TableExtension")]
        [TestCase("TableExt_NamespaceCasingMismatch")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InvocationWithTableExtension", "TableExt_Multiple_SameBase", "TableExtension", "TableExtensionTypeWithLength", "TableExt_NamespaceCasingMismatch"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }

        [Test]
        [TestCase("BuiltInInvocation")]
        [TestCase("Invocation_ObsoleteStateRemoved")]
        [TestCase("Invocation_Pragma")]
        [TestCase("InvocationSkipFieldsNotMatchingType")]
        [TestCase("InvocationWithInitPrimaryKeyFieldsIsFalse")]
        [TestCase("InvocationWithTableExtension")]
        [TestCase("TableExt_ObsoleteStateRemoved")]
        [TestCase("TableExt_Paired_Extension_Pragma")]
        [TestCase("TableExt_Paired_SingleTableExt")]
        [TestCase("TableExt_Unpaired")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["InvocationWithTableExtension", "TableExt_ObsoleteStateRemoved", "TableExt_Paired_Extension_Pragma", "TableExt_Paired_SingleTableExt", "TableExt_Unpaired"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsNameMismatch);
        }
    }
}