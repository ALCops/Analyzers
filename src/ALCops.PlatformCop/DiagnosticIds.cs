namespace ALCops.PlatformCop;

public static class DiagnosticIds
{
    public static readonly string EditableFlowField = "PC0001";
    public static readonly string AutoIncrementInTemporaryTable = "PC0002";
    public static readonly string SetRangeWithFilterOperators = "PC0003";
    public static readonly string ListObjectsAreOneBased = "PC0004";
    public static readonly string ExtensiblePropertyExplicitlySet = "PC0005";
    public static readonly string AccessPropertyExplicitlySet = "PC0006";
    public static readonly string AutoCalcFieldsOnlyOnFlowFields = "PC0007";
    public static readonly string OperatorAndPlaceholderInFilterExpression = "PC0008";
    public static readonly string EventSubscriberVarKeyword = "PC0010";
    public static readonly string EventPublisherIsHandledByVar = "PC0011";
    public static readonly string FlowFilterFieldAssignment = "PC0012";
    public static readonly string RecordGetProcedureArguments = "PC0013";
    public static readonly string JsonTokenJPathUsesDoubleQuotes = "PC0014";
    public static readonly string GuidEmptyStringComparison = "PC0015";
    public static readonly string ClearCodeunitSingleInstance = "PC0016";
    public static readonly string PageRecordArgumentMismatch = "PC0017";
    public static readonly string PageRecordMethodRequiresSourceTable = "PC0018";
    public static readonly string FilterStringSingleQuoteEscaping = "PC0019";
    public static readonly string TransferFieldsTypeMismatch = "PC0020";
    public static readonly string TransferFieldsNameMismatch = "PC0021";
    public static readonly string PossibleOverflowAssigning = "PC0022";
    public static readonly string IsHandledParameterAssignment = "PC0023";
    public static readonly string ApplicationAreaOnApiPage = "PC0024";
    public static readonly string ODataKeyFieldsShouldUseSystemId = "PC0025";
    public static readonly string MandatoryFieldMissingOnApiPage = "PC0026";
    public static readonly string TemporaryRecordTriggerInvocation = "PC0027";
    public static readonly string TableRelationFieldLength = "PC0028";
    public static readonly string UseSequentialGuid = "PC0029";
    public static readonly string UsePartialRecordsOnRead = "PC0030";
    public static readonly string PartialRecordsCauseJitLoad = "PC0031";
    public static readonly string ReportLayoutPropertyLength = "PC0032";
    public static readonly string DuplicateODataEntityName = "PC0033";
    public static readonly string PlaceholderArgumentCountMismatch = "PC0034";
    public static readonly string UseSetAutoCalcFieldsForLoops = "PC0035";
    public static readonly string PageVariableSetRecordTemporaryRecord = "PC0036";
    public static readonly string UseValidateForFieldAssignment = "PC0037";
}