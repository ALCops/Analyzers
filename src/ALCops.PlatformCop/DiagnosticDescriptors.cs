using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor AutoIncrementInTemporaryTable = new(
        id: DiagnosticIds.AutoIncrementInTemporaryTable,
        title: PlatformCopAnalyzers.AutoIncrementInTemporaryTableTitle,
        messageFormat: PlatformCopAnalyzers.AutoIncrementInTemporaryTableFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.AutoIncrementInTemporaryTableDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.AutoIncrementInTemporaryTable));

    public static readonly DiagnosticDescriptor EditableFlowField = new(
        id: DiagnosticIds.EditableFlowField,
        title: PlatformCopAnalyzers.EditableFlowFieldTitle,
        messageFormat: PlatformCopAnalyzers.EditableFlowFieldFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.EditableFlowFieldDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EditableFlowField));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://alcops.dev/docs/analyzers/platformcop/{0}/", identifier.ToLower());
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