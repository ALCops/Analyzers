using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.LinterCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ApplicationAreaRedundancy = new(
        id: DiagnosticIds.ApplicationAreaRedundancy,
        title: LinterCopAnalyzers.ApplicationAreaRedundancyMessageFormat,
        messageFormat: LinterCopAnalyzers.ApplicationAreaRedundancyMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.ApplicationAreaRedundancyDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ApplicationAreaRedundancy));

    public static readonly DiagnosticDescriptor CyclomaticComplexityMetric = new(
        id: DiagnosticIds.CyclomaticComplexityMetric,
        title: LinterCopAnalyzers.CyclomaticComplexityMetricMessageFormat,
        messageFormat: LinterCopAnalyzers.CyclomaticComplexityMetricMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: LinterCopAnalyzers.CyclomaticComplexityMetricDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.CyclomaticComplexityMetric));

    public static readonly DiagnosticDescriptor CyclomaticComplexityThresholdExceeded = new(
        id: DiagnosticIds.CyclomaticComplexityThresholdExceeded,
        title: LinterCopAnalyzers.CyclomaticComplexityThresholdExceededTitle,
        messageFormat: LinterCopAnalyzers.CyclomaticComplexityThresholdExceededMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.CyclomaticComplexityThresholdExceededDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.CyclomaticComplexityThresholdExceeded));

    public static readonly DiagnosticDescriptor DataClassificationRedundancy = new(
        id: DiagnosticIds.DataClassificationRedundancy,
        title: LinterCopAnalyzers.DataClassificationRedundancyMessageFormat,
        messageFormat: LinterCopAnalyzers.DataClassificationRedundancyMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.DataClassificationRedundancyDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.DataClassificationRedundancy));

    public static readonly DiagnosticDescriptor ErrorInvocationUsingTextConstant = new(
          id: DiagnosticIds.ErrorInvocationUsingTextConstant,
          title: LinterCopAnalyzers.ErrorInvocationUsingTextConstantMessageFormat,
          messageFormat: LinterCopAnalyzers.ErrorInvocationUsingTextConstantMessageFormat,
          category: Category.Design,
          defaultSeverity: DiagnosticSeverity.Warning,
          isEnabledByDefault: true,
          description: LinterCopAnalyzers.ErrorInvocationUsingTextConstantDescription,
          helpLinkUri: GetHelpUri(DiagnosticIds.ErrorInvocationUsingTextConstant));

    public static readonly DiagnosticDescriptor ExplicitlySetRunTrigger = new(
        id: DiagnosticIds.ExplicitlySetRunTrigger,
        title: LinterCopAnalyzers.ExplicitlySetRunTriggerMessageFormat,
        messageFormat: LinterCopAnalyzers.ExplicitlySetRunTriggerMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.ExplicitlySetRunTriggerDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ExplicitlySetRunTrigger));

    public static readonly DiagnosticDescriptor IdentifiersInEventSubscribers = new(
        id: DiagnosticIds.IdentifiersInEventSubscribers,
        title: LinterCopAnalyzers.IdentifiersInEventSubscribersTitle,
        messageFormat: LinterCopAnalyzers.IdentifiersInEventSubscribersMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.IdentifiersInEventSubscribersDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.IdentifiersInEventSubscribers));

    public static readonly DiagnosticDescriptor MaintainabilityIndexMetric = new(
        id: DiagnosticIds.MaintainabilityIndexMetric,
        title: LinterCopAnalyzers.MaintainabilityIndexMetricTitle,
        messageFormat: LinterCopAnalyzers.MaintainabilityIndexMetricMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: LinterCopAnalyzers.MaintainabilityIndexMetricDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.MaintainabilityIndexMetric));

    public static readonly DiagnosticDescriptor MaintainabilityIndexThresholdExceeded = new(
        id: DiagnosticIds.MaintainabilityIndexThresholdExceeded,
        title: LinterCopAnalyzers.MaintainabilityIndexThresholdExceededTitle,
        messageFormat: LinterCopAnalyzers.MaintainabilityIndexThresholdExceededMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.MaintainabilityIndexThresholdExceededDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.MaintainabilityIndexThresholdExceeded));

    public static readonly DiagnosticDescriptor ObjectIdInDeclaration = new(
        id: DiagnosticIds.ObjectIdInDeclaration,
        title: LinterCopAnalyzers.ObjectIdInDeclarationTitle,
        messageFormat: LinterCopAnalyzers.ObjectIdInDeclarationMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.ObjectIdInDeclarationDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ObjectIdInDeclaration));

    public static readonly DiagnosticDescriptor ObjectIdInDeclarationWithoutCodeFix = new(
        id: DiagnosticIds.ObjectIdInDeclaration,
        title: LinterCopAnalyzers.ObjectIdInDeclarationTitle,
        messageFormat: LinterCopAnalyzers.ObjectIdInDeclarationFormatWithoutCodeFix,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.ObjectIdInDeclarationDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ObjectIdInDeclaration));

    public static readonly DiagnosticDescriptor RecordInstanceIsolationLevel = new(
        id: DiagnosticIds.RecordInstanceIsolationLevel,
        title: LinterCopAnalyzers.RecordInstanceIsolationLevelTitle,
        messageFormat: LinterCopAnalyzers.RecordInstanceIsolationLevelMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.RecordInstanceIsolationLevelDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.RecordInstanceIsolationLevel));

    public static readonly DiagnosticDescriptor UseSecretTextForSensitiveText = new(
        id: DiagnosticIds.UseSecretTextForSensitiveText,
        title: LinterCopAnalyzers.UseSecretTextForSensitiveTextTitle,
        messageFormat: LinterCopAnalyzers.UseSecretTextForSensitiveTextMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: LinterCopAnalyzers.UseSecretTextForSensitiveTextDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.UseSecretTextForSensitiveText));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://alcops.dev/docs/analyzers/lintercop/{0}/", identifier.ToLower());
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