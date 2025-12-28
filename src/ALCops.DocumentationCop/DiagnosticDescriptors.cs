using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.DocumentationCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor CommitRequiresComment = new(
        id: DiagnosticIds.CommitRequiresComment,
        title: DocumentationCopAnalyzers.CommitRequiresCommentTitle,
        messageFormat: DocumentationCopAnalyzers.CommitRequiresCommentMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: DocumentationCopAnalyzers.CommitRequiresCommentDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.CommitRequiresComment));

    public static readonly DiagnosticDescriptor EmptyStatementRequiresComment = new(
        id: DiagnosticIds.EmptyStatementRequiresComment,
        title: DocumentationCopAnalyzers.EmptyStatementRequiresCommentTitle,
        messageFormat: DocumentationCopAnalyzers.EmptyStatementRequiresCommentMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: DocumentationCopAnalyzers.EmptyStatementRequiresCommentDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EmptyStatementRequiresComment));

    public static readonly DiagnosticDescriptor PublicProcedureRequiresDocumentation = new(
        id: DiagnosticIds.PublicProcedureRequiresDocumentation,
        title: DocumentationCopAnalyzers.PublicProcedureRequiresDocumentationTitle,
        messageFormat: DocumentationCopAnalyzers.PublicProcedureRequiresDocumentationMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: DocumentationCopAnalyzers.PublicProcedureRequiresDocumentationDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PublicProcedureRequiresDocumentation));

    public static readonly DiagnosticDescriptor WriteToFlowFieldRequiresComment = new(
        id: DiagnosticIds.WriteToFlowFieldRequiresComment,
        title: DocumentationCopAnalyzers.WriteToFlowFieldRequiresCommentTitle,
        messageFormat: DocumentationCopAnalyzers.WriteToFlowFieldRequiresCommentMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: DocumentationCopAnalyzers.WriteToFlowFieldRequiresCommentDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.WriteToFlowFieldRequiresComment));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://alcops.dev/docs/analyzers/documentationcop/{0}/", identifier.ToLower());
    }

    /// <summary>
    /// Categories used to group diagnostics. These follow Roslyn conventions, 
    /// and make it easy to filter diagnostics in rulesets or suppressions.
    /// </summary>
    internal static class Category
    {
        /// <summary>
        /// Design issues: application-level or object-level correctness problems.
        /// Example: FlowFields should not be editable, incorrect usage of Confirm Management.
        /// </summary>
        public const string Design = "Design";

        /// <summary>
        /// Naming issues: enforce naming conventions for fields, methods, enums, etc.
        /// Example: Boolean-returning procedures must start with 'Is', 'Has', 'Can'.
        /// </summary>
        public const string Naming = "Naming";

        /// <summary>
        /// Style issues: coding style, formatting, and readability conventions.
        /// Example: Require parentheses in complex expressions, disallow 'with' statements.
        /// </summary>
        public const string Style = "Style";

        /// <summary>
        /// Usage issues: incorrect or discouraged use of AL language constructs.
        /// Example: Calling MODIFY/INSERT/DELETE without TRUE for RunTrigger.
        /// </summary>
        public const string Usage = "Usage";

        /// <summary>
        /// Performance issues: rules that help improve runtime efficiency or avoid unnecessary resource usage.
        /// Example: Avoid 'FindSet' without 'while', prefer 'SetRange' over 'SetFilter' when applicable.
        /// </summary>
        public const string Performance = "Performance";

        /// <summary>
        /// Security issues: rules related to exposure, permissions, or unsafe practices.
        /// Example: Avoid exposing internal APIs, hard-coded credentials, or missing permission checks.
        /// </summary>
        public const string Security = "Security";
    }
}