using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor AllowInCustomizationsForOmittedFields = new(
        id: DiagnosticIds.AllowInCustomizationsForOmittedFields,
        title: ApplicationCopAnalyzers.AllowInCustomizationsForOmittedFieldsTitle,
        messageFormat: ApplicationCopAnalyzers.AllowInCustomizationsForOmittedFieldsMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.AllowInCustomizationsForOmittedFieldsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.AllowInCustomizationsForOmittedFields));

    public static readonly DiagnosticDescriptor CaptionRequired = new(
        id: DiagnosticIds.CaptionRequired,
        title: ApplicationCopAnalyzers.CaptionRequiredTitle,
        messageFormat: ApplicationCopAnalyzers.CaptionRequiredMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.CaptionRequiredDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.CaptionRequired));

    public static readonly DiagnosticDescriptor ConfirmImplementConfirmManagement = new(
        id: DiagnosticIds.ConfirmImplementConfirmManagement,
        title: ApplicationCopAnalyzers.ConfirmImplementConfirmManagementTitle,
        messageFormat: ApplicationCopAnalyzers.ConfirmImplementConfirmManagementMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.ConfirmImplementConfirmManagementDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ConfirmImplementConfirmManagement));

    public static readonly DiagnosticDescriptor EmptyCaptionLocked = new(
        id: DiagnosticIds.EmptyCaptionLocked,
        title: ApplicationCopAnalyzers.EmptyCaptionLockedTitle,
        messageFormat: ApplicationCopAnalyzers.EmptyCaptionLockedMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.EmptyCaptionLockedDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EmptyCaptionLocked));

    public static readonly DiagnosticDescriptor EnumEmptyValueHasCaption = new(
        id: DiagnosticIds.EnumEmptyValueHasCaption,
        title: ApplicationCopAnalyzers.EnumEmptyValueHasCaptionTitle,
        messageFormat: ApplicationCopAnalyzers.EnumEmptyValueHasCaptionMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.EnumEmptyValueHasCaptionDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EnumEmptyValueHasCaption));

    public static readonly DiagnosticDescriptor EnumValueHasEmptyCaption = new(
        id: DiagnosticIds.EnumValueHasEmptyCaption,
        title: ApplicationCopAnalyzers.EnumValueHasEmptyCaptionTitle,
        messageFormat: ApplicationCopAnalyzers.EnumValueHasEmptyCaptionMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.EnumValueHasEmptyCaptionDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EnumValueHasEmptyCaption));

    public static readonly DiagnosticDescriptor FieldGroupsRequired = new(
        id: DiagnosticIds.FieldGroupsRequired,
        title: ApplicationCopAnalyzers.FieldGroupsRequiredTitle,
        messageFormat: ApplicationCopAnalyzers.FieldGroupsRequiredMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: ApplicationCopAnalyzers.FieldGroupsRequiredDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.FieldGroupsRequired));

    public static readonly DiagnosticDescriptor GlobalLanguageImplementTranslationHelper = new(
        id: DiagnosticIds.GlobalLanguageImplementTranslationHelper,
        title: ApplicationCopAnalyzers.GlobalLanguageImplementTranslationHelperTitle,
        messageFormat: ApplicationCopAnalyzers.GlobalLanguageImplementTranslationHelperMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.GlobalLanguageImplementTranslationHelperDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.GlobalLanguageImplementTranslationHelper));

    public static readonly DiagnosticDescriptor LabelLockedMustHaveTokSuffix = new(
        id: DiagnosticIds.LabelLockedMustHaveTokSuffix,
        title: ApplicationCopAnalyzers.LabelLockedMustHaveTokSuffixTitle,
        messageFormat: ApplicationCopAnalyzers.LabelLockedMustHaveTokSuffixMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: ApplicationCopAnalyzers.LabelLockedMustHaveTokSuffixDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.LabelLockedMustHaveTokSuffix));

    public static readonly DiagnosticDescriptor LabelWithTokSuffixMustBeLocked = new(
        id: DiagnosticIds.LabelWithTokSuffixMustBeLocked,
        title: ApplicationCopAnalyzers.LabelWithTokSuffixMustBeLockedTitle,
        messageFormat: ApplicationCopAnalyzers.LabelWithTokSuffixMustBeLockedMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.LabelWithTokSuffixMustBeLockedDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.LabelWithTokSuffixMustBeLocked));

    public static readonly DiagnosticDescriptor LineSeparatorShouldUseTypeHelper = new(
        id: DiagnosticIds.LineSeparatorShouldUseTypeHelper,
        title: ApplicationCopAnalyzers.LineSeparatorShouldUseTypeHelperTitle,
        messageFormat: ApplicationCopAnalyzers.LineSeparatorShouldUseTypeHelperMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.LineSeparatorShouldUseTypeHelperDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.LineSeparatorShouldUseTypeHelper));

    public static readonly DiagnosticDescriptor InstallAndUpgradeCodeunitsShouldBeInternal = new(
        id: DiagnosticIds.InstallAndUpgradeCodeunitsShouldBeInternal,
        title: ApplicationCopAnalyzers.InstallAndUpgradeCodeunitsShouldBeInternalTitle,
        messageFormat: ApplicationCopAnalyzers.InstallAndUpgradeCodeunitsShouldBeInternalMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.InstallAndUpgradeCodeunitsShouldBeInternalDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.InstallAndUpgradeCodeunitsShouldBeInternal));

    public static readonly DiagnosticDescriptor IntegrationEventInInternalCodeunit = new(
        id: DiagnosticIds.IntegrationEventInInternalCodeunit,
        title: ApplicationCopAnalyzers.IntegrationEventInInternalCodeunitTitle,
        messageFormat: ApplicationCopAnalyzers.IntegrationEventInInternalCodeunitMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.IntegrationEventInInternalCodeunitDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.IntegrationEventInInternalCodeunit));

    public static readonly DiagnosticDescriptor LookupPageIdAndDrillDownPageId = new(
        id: DiagnosticIds.LookupPageIdAndDrillDownPageId,
        title: ApplicationCopAnalyzers.LookupPageIdAndDrillDownPageIdTitle,
        messageFormat: ApplicationCopAnalyzers.LookupPageIdAndDrillDownPageIdMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.LookupPageIdAndDrillDownPageIdDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.LookupPageIdAndDrillDownPageId));

    public static readonly DiagnosticDescriptor NotBlankRequiredOnPrimaryKeyField = new(
        id: DiagnosticIds.NotBlankRequiredOnPrimaryKeyField,
        title: ApplicationCopAnalyzers.NotBlankRequiredOnPrimaryKeyFieldTitle,
        messageFormat: ApplicationCopAnalyzers.NotBlankRequiredOnPrimaryKeyFieldMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.NotBlankRequiredOnPrimaryKeyFieldDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NotBlankRequiredOnPrimaryKeyField));

    public static readonly DiagnosticDescriptor NotBlankNotAllowedOnPrimaryKeyField = new(
        id: DiagnosticIds.NotBlankNotAllowedOnPrimaryKeyField,
        title: ApplicationCopAnalyzers.NotBlankNotAllowedOnPrimaryKeyFieldTitle,
        messageFormat: ApplicationCopAnalyzers.NotBlankNotAllowedOnPrimaryKeyFieldMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.NotBlankNotAllowedOnPrimaryKeyFieldDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.NotBlankNotAllowedOnPrimaryKeyField));

    public static readonly DiagnosticDescriptor PermissionSetCaptionLength = new(
        id: DiagnosticIds.PermissionSetCaptionLength,
        title: ApplicationCopAnalyzers.PermissionSetCaptionLengthTitle,
        messageFormat: ApplicationCopAnalyzers.PermissionSetCaptionLengthMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.PermissionSetCaptionLengthDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PermissionSetCaptionLength));

    public static readonly DiagnosticDescriptor PermissionSetCoverage = new(
        id: DiagnosticIds.PermissionSetCoverage,
        title: ApplicationCopAnalyzers.PermissionSetCoverageTitle,
        messageFormat: ApplicationCopAnalyzers.PermissionSetCoverageMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.PermissionSetCoverageDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PermissionSetCoverage));

    public static readonly DiagnosticDescriptor PublicEventPublisher = new(
        id: DiagnosticIds.PublicEventPublisher,
        title: ApplicationCopAnalyzers.PublicEventPublisherTitle,
        messageFormat: ApplicationCopAnalyzers.PublicEventPublisherMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.PublicEventPublisherDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PublicEventPublisher));

    public static readonly DiagnosticDescriptor RunPageImplementPageManagement = new(
        id: DiagnosticIds.RunPageImplementPageManagement,
        title: ApplicationCopAnalyzers.RunPageImplementPageManagementTitle,
        messageFormat: ApplicationCopAnalyzers.RunPageImplementPageManagementMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.RunPageImplementPageManagementDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.RunPageImplementPageManagement));

    public static readonly DiagnosticDescriptor TableDataPerCompanyDeclaration = new(
        id: DiagnosticIds.TableDataPerCompanyDeclaration,
        title: ApplicationCopAnalyzers.TableDataPerCompanyDeclarationTitle,
        messageFormat: ApplicationCopAnalyzers.TableDataPerCompanyDeclarationMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: ApplicationCopAnalyzers.TableDataPerCompanyDeclarationDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.TableDataPerCompanyDeclaration));

    public static readonly DiagnosticDescriptor ToolTipDoNotUseLineBreaks = new(
         id: DiagnosticIds.ToolTipDoNotUseLineBreaks,
         title: ApplicationCopAnalyzers.ToolTipDoNotUseLineBreaksTitle,
         messageFormat: ApplicationCopAnalyzers.ToolTipDoNotUseLineBreaksMessageFormat,
         category: Category.Design,
         defaultSeverity: DiagnosticSeverity.Info,
         isEnabledByDefault: true,
         description: ApplicationCopAnalyzers.ToolTipDoNotUseLineBreaksDescription,
         helpLinkUri: GetHelpUri(DiagnosticIds.ToolTipDoNotUseLineBreaks));

    public static readonly DiagnosticDescriptor ToolTipMaximumLength = new(
        id: DiagnosticIds.ToolTipMaximumLength,
        title: ApplicationCopAnalyzers.ToolTipMaximumLengthTitle,
        messageFormat: ApplicationCopAnalyzers.ToolTipMaximumLengthMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.ToolTipMaximumLengthDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ToolTipMaximumLength));

    public static readonly DiagnosticDescriptor ToolTipMustEndWithDot = new(
        id: DiagnosticIds.ToolTipMustEndWithDot,
        title: ApplicationCopAnalyzers.ToolTipMustEndWithDotTitle,
        messageFormat: ApplicationCopAnalyzers.ToolTipMustEndWithDotMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.ToolTipMustEndWithDotDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ToolTipMustEndWithDot));

    public static readonly DiagnosticDescriptor ToolTipShouldStartWithSpecifies = new(
        id: DiagnosticIds.ToolTipShouldStartWithSpecifies,
        title: ApplicationCopAnalyzers.ToolTipShouldStartWithSpecifiesTitle,
        messageFormat: ApplicationCopAnalyzers.ToolTipShouldStartWithSpecifiesMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.ToolTipShouldStartWithSpecifiesDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ToolTipShouldStartWithSpecifies));

    public static readonly DiagnosticDescriptor ZeroEnumValueReservedForEmpty = new(
        id: DiagnosticIds.ZeroEnumValueReservedForEmpty,
        title: ApplicationCopAnalyzers.ZeroEnumValueReservedForEmptyTitle,
        messageFormat: ApplicationCopAnalyzers.ZeroEnumValueReservedForEmptyMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ApplicationCopAnalyzers.ZeroEnumValueReservedForEmptyDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ZeroEnumValueReservedForEmpty));

    public static string GetHelpUri(string identifier)
    {
        return string.Format(CultureInfo.InvariantCulture, "https://alcops.dev/docs/analyzers/applicationcop/{0}/", identifier.ToLower());
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