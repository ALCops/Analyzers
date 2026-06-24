namespace ALCops.DocumentationCop;

public static class DiagnosticIds
{
    public static readonly string AnalyzerException = "DC0000";
    public static readonly string CommitRequiresComment = "DC0001";
    public static readonly string WriteToFlowFieldRequiresComment = "DC0002";
    public static readonly string EmptyStatementRequiresComment = "DC0003";
    public static readonly string PublicProcedureRequiresDocumentation = "DC0004";
    public static readonly string XmlDocumentationProcedureConsistency = "DC0005";
	public static readonly string InternalProcedureRequiresDocumentation = "DC0006";
	public static readonly string PublicObjectRequiresDocumentation = "DC0007";
	public static readonly string InternalObjectRequiresDocumentation = "DC0008";
	public static readonly string EventRequiresDocumentation = "DC0009";
	public static readonly string InternalEventRequiresDocumentation = "DC0010";
}