using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TransferFieldsTypeMismatch : NavCodeAnalysisBase
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
                    Path.Combine("Rules", nameof(TransferFieldsTypeMismatch)));
        }

        [Test]
        [TestCase("InvocationRecWithCodeunit")]
        [TestCase("InvocationRecWithPage")]
        [TestCase("InvocationRecWithTable")]
        [TestCase("InvocationRecWithTablexRec")]
        [TestCase("InvocationSkipFieldsNotMatchingType")]
        [TestCase("InvocationWithReturnValue")]
        [TestCase("InvocationWithVarGlobals")]
        [TestCase("InvocationWithVarLocalAndGlobal")]
        [TestCase("InvocationWithVarLocals")]
        [TestCase("InvocationWithVarParam")]
        [TestCase("TableExt_Multiple_SameBase")]
        [TestCase("TableExtension")]
        [TestCase("TableExtensionTypeWithType")]
        [TestCase("TableExtensionTypeWithTypeLength")]
        public async Task HasDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExt_Multiple_SameBase", "TableExtension", "TableExtensionTypeWithType", "TableExtensionTypeWithTypeLength"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsTypeMismatch);
        }

        [Test]
        [TestCase("BuiltInInvocation")]
        [TestCase("Invocation_Pragma")]
        [TestCase("InvocationSkipFieldsNotMatchingType")]
        [TestCase("InvocationWithType")]
        [TestCase("InvocationWithTypeLength")]
        [TestCase("TableExt_Paired_Extension_Pragma")]
        [TestCase("TableExt_Paired_SingleTableExt")]
        [TestCase("TableExt_Unpaired")]
        public async Task NoDiagnostic(string testCase)
        {
            SkipTestIfVersionIsTooLow(
                ["TableExt_Paired_Extension_Pragma", "TableExt_Paired_SingleTableExt", "TableExt_Unpaired"],
                testCase,
                "13.0",
                "No support for tableextensions when target itself is already declared in the same module");

            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TransferFieldsTypeMismatch);
        }
    }
}