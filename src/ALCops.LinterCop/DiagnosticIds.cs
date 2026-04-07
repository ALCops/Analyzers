namespace ALCops.LinterCop;

public static class DiagnosticIds
{
    public static readonly string ObjectIdInDeclaration = "LC0003";
    public static readonly string RecordInstanceIsolationLevel = "LC0031";
    public static readonly string MaintainabilityIndexMetric = "LC0007";
    public static readonly string MaintainabilityIndexThresholdExceeded = "LC0008";
    public static readonly string CyclomaticComplexityMetric = "LC0009";
    public static readonly string CyclomaticComplexityThresholdExceeded = "LC0010";
    public static readonly string DataClassificationRedundancy = "LC0019";
    public static readonly string ApplicationAreaRedundancy = "LC0020";
    public static readonly string IdentifiersInEventSubscribers = "LC0028";
    public static readonly string AppManifestRuntimeBehind = "LC0033";
    public static readonly string ExplicitlySetRunTrigger = "LC0040";
    public static readonly string UseSecretTextForSensitiveText = "LC0043";
    public static readonly string ErrorInvocationUsingTextConstant = "LC0048";
    public static readonly string InternalProcedureNotReferenced = "LC0052";
    public static readonly string InternalProcedureOnlyUsedInCurrentObject = "LC0053";
    public static readonly string InterfaceObjectNameGuide = "LC0054";
    public static readonly string ApiPageCanonicalFieldNameGuide = "LC0063";
    public static readonly string UseIsEmptyMethodInsteadOfCount = "LC0081";
    public static readonly string UseQueryOrFindWithNextInsteadOfCount = "LC0082";
    public static readonly string BuiltInDateTimeMethod = "LC0083";
    public static readonly string PageStyleStringLiteral = "LC0086";
    public static readonly string OptionTypeShouldBeEnum = "LC0088";
    public static readonly string CognitiveComplexityMetric = "LC0089";
    public static readonly string CognitiveComplexityIncrement = "LC0089i";
    public static readonly string CognitiveComplexityThresholdExceeded = "LC0090";
    public static readonly string AllowInCustomizationsRedundancy = "LC0094";
    public static readonly string UsePartialRecordsOnRead = "LC0095";
}