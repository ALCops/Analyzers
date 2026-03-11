using RoslynTestKit;

namespace ALCops.PlatformCop.Test;

public class RecordGetProcedureArguments : NavCodeAnalysisBase
{
    private AnalyzerTestFixture _fixture;
    private static readonly Analyzers.RecordGetProcedureArguments _analyzer = new();
    private string _testCasePath;

    [SetUp]
    public void Setup()
    {
        _fixture = RoslynFixtureFactory.Create<Analyzers.RecordGetProcedureArguments>();

        _testCasePath = Path.Combine(
            Directory.GetParent(
                Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                Path.Combine("Rules", nameof(RecordGetProcedureArguments)));
    }

    [Test]
    [TestCase("ImplicitConversionCodeToEnum")]
    [TestCase("ImplicitConversionEnumToAnotherEnum")]
    [TestCase("RecordGetCodeFieldLengthTooLong")]
    [TestCase("RecordGetEnum")]
    [TestCase("RecordGetGlobalVariable")]
    [TestCase("RecordGetLocalVariable")]
    [TestCase("RecordGetMethod")]
    [TestCase("RecordGetOptionMemberAccessCrossTable")]
    [TestCase("RecordGetOptionMemberAccessMismatchPrimaryKey")]
    [TestCase("RecordGetParameter")]
    [TestCase("RecordGetReportDataItem")]
    [TestCase("RecordGetReturnValue")]
    [TestCase("RecordGetSetupTableIncorrectArgumentsProvided")]
    [TestCase("RecordGetSetupTableNoArgumentsProvided")]
    [TestCase("RecordGetXmlPortTableElement")]
    public async Task HasDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.RecordGetProcedureArguments);
    }

    [Test]
    [TestCase("ImplicitConversionIntegerToEnumFromInteger")]
    [TestCase("ImplicitConversionIntegerToOption")]
    [TestCase("ImplicitConversionLabelToCode")]
    [TestCase("PrimaryKeyAsInteger")]
    [TestCase("RecordGetBuiltInMethodRecordId")]
    [TestCase("RecordGetCode10ToCode20")]
    [TestCase("RecordGetDecimalToInteger")]
    [TestCase("RecordGetEnum")]
    [TestCase("RecordGetFieldRecordId")]
    [TestCase("RecordGetGlobalVariable")]
    [TestCase("RecordGetLocalVariable")]
    [TestCase("RecordGetLocalVariableRecordId")]
    [TestCase("RecordGetMethod")]
    [TestCase("RecordGetMethodRecordId")]
    [TestCase("RecordGetOptionMemberAccessMatchesPrimaryKey")]
    [TestCase("RecordGetParameter")]
    [TestCase("RecordGetParameterRecordId")]
    [TestCase("RecordGetReportDataItem")]
    [TestCase("RecordGetReturnValue")]
    [TestCase("RecordGetReturnValueRecordId")]
    [TestCase("RecordGetSetupTableCorrectArgumentsProvided")]
    [TestCase("RecordGetSetupTableNoArgumentsProvided")]
    [TestCase("RecordGetXmlPortTableElement")]
    public async Task NoDiagnostic(string testCase)
    {
        var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
            .ConfigureAwait(false);

        _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.RecordGetProcedureArguments);
    }
}
