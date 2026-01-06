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
}