using System.Globalization;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor AccessPropertyExplicitlySet = new(
        id: DiagnosticIds.AccessPropertyExplicitlySet,
        title: PlatformCopAnalyzers.AccessPropertyExplicitlySetTitle,
        messageFormat: PlatformCopAnalyzers.AccessPropertyExplicitlySetMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: PlatformCopAnalyzers.AccessPropertyExplicitlySetDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.AccessPropertyExplicitlySet));

    public static readonly DiagnosticDescriptor ApplicationAreaOnApiPage = new(
        id: DiagnosticIds.ApplicationAreaOnApiPage,
        title: PlatformCopAnalyzers.ApplicationAreaOnApiPageTitle,
        messageFormat: PlatformCopAnalyzers.ApplicationAreaOnApiPageMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.ApplicationAreaOnApiPageDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ApplicationAreaOnApiPage));

    public static readonly DiagnosticDescriptor AutoCalcFieldsOnlyOnFlowFields = new(
        id: DiagnosticIds.AutoCalcFieldsOnlyOnFlowFields,
        title: PlatformCopAnalyzers.AutoCalcFieldsOnlyOnFlowFieldsTitle,
        messageFormat: PlatformCopAnalyzers.AutoCalcFieldsOnlyOnFlowFieldsMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.AutoCalcFieldsOnlyOnFlowFieldsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.AutoCalcFieldsOnlyOnFlowFields));

    public static readonly DiagnosticDescriptor AutoIncrementInTemporaryTable = new(
        id: DiagnosticIds.AutoIncrementInTemporaryTable,
        title: PlatformCopAnalyzers.AutoIncrementInTemporaryTableTitle,
        messageFormat: PlatformCopAnalyzers.AutoIncrementInTemporaryTableMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.AutoIncrementInTemporaryTableDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.AutoIncrementInTemporaryTable));

    public static readonly DiagnosticDescriptor ClearCodeunitSingleInstance = new(
         id: DiagnosticIds.ClearCodeunitSingleInstance,
         title: PlatformCopAnalyzers.ClearCodeunitSingleInstanceTitle,
         messageFormat: PlatformCopAnalyzers.ClearCodeunitSingleInstanceMessageFormat,
         category: Category.Design,
         defaultSeverity: DiagnosticSeverity.Warning,
         isEnabledByDefault: true,
         description: PlatformCopAnalyzers.ClearCodeunitSingleInstanceDescription,
         helpLinkUri: GetHelpUri(DiagnosticIds.ClearCodeunitSingleInstance));

    public static readonly DiagnosticDescriptor EditableFlowField = new(
        id: DiagnosticIds.EditableFlowField,
        title: PlatformCopAnalyzers.EditableFlowFieldTitle,
        messageFormat: PlatformCopAnalyzers.EditableFlowFieldMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.EditableFlowFieldDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EditableFlowField));

    public static readonly DiagnosticDescriptor EventPublisherIsHandledByVar = new(
        id: DiagnosticIds.EventPublisherIsHandledByVar,
        title: PlatformCopAnalyzers.EventPublisherIsHandledByVarTitle,
        messageFormat: PlatformCopAnalyzers.EventPublisherIsHandledByVarMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.EventPublisherIsHandledByVarDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EventPublisherIsHandledByVar));

    public static readonly DiagnosticDescriptor EventSubscriberVarKeyword = new(
        id: DiagnosticIds.EventSubscriberVarKeyword,
        title: PlatformCopAnalyzers.EventSubscriberVarKeywordTitle,
        messageFormat: PlatformCopAnalyzers.EventSubscriberVarKeywordMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.EventSubscriberVarKeywordDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.EventSubscriberVarKeyword));

    public static readonly DiagnosticDescriptor ExtensiblePropertyExplicitlySet = new(
        id: DiagnosticIds.ExtensiblePropertyExplicitlySet,
        title: PlatformCopAnalyzers.ExtensiblePropertyExplicitlySetTitle,
        messageFormat: PlatformCopAnalyzers.ExtensiblePropertyExplicitlySetMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: PlatformCopAnalyzers.ExtensiblePropertyExplicitlySetDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ExtensiblePropertyExplicitlySet));

    public static readonly DiagnosticDescriptor FilterStringSingleQuoteEscaping = new(
        id: DiagnosticIds.FilterStringSingleQuoteEscaping,
        title: PlatformCopAnalyzers.FilterStringSingleQuoteEscapingTitle,
        messageFormat: PlatformCopAnalyzers.FilterStringSingleQuoteEscapingMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.FilterStringSingleQuoteEscapingDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.FilterStringSingleQuoteEscaping));

    public static readonly DiagnosticDescriptor FlowFilterFieldAssignment = new(
        id: DiagnosticIds.FlowFilterFieldAssignment,
        title: PlatformCopAnalyzers.FlowFilterFieldAssignmentTitle,
        messageFormat: PlatformCopAnalyzers.FlowFilterFieldAssignmentMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.FlowFilterFieldAssignmentDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.FlowFilterFieldAssignment));

    public static readonly DiagnosticDescriptor GuidEmptyStringComparison = new(
        id: DiagnosticIds.GuidEmptyStringComparison,
        title: PlatformCopAnalyzers.GuidEmptyStringComparisonTitle,
        messageFormat: PlatformCopAnalyzers.GuidEmptyStringComparisonMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.GuidEmptyStringComparisonDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.GuidEmptyStringComparison));

    public static readonly DiagnosticDescriptor IsHandledParameterAssignment = new(
        id: DiagnosticIds.IsHandledParameterAssignment,
        title: PlatformCopAnalyzers.IsHandledParameterAssignmentTitle,
        messageFormat: PlatformCopAnalyzers.IsHandledParameterAssignmentMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.IsHandledParameterAssignmentDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.IsHandledParameterAssignment));

    public static readonly DiagnosticDescriptor JsonTokenJPathUsesDoubleQuotes = new(
        id: DiagnosticIds.JsonTokenJPathUsesDoubleQuotes,
        title: PlatformCopAnalyzers.JsonTokenJPathUsesDoubleQuotesTitle,
        messageFormat: PlatformCopAnalyzers.JsonTokenJPathUsesDoubleQuotesMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.JsonTokenJPathUsesDoubleQuotesDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.JsonTokenJPathUsesDoubleQuotes));

    public static readonly DiagnosticDescriptor ListObjectsAreOneBased = new(
        id: DiagnosticIds.ListObjectsAreOneBased,
        title: PlatformCopAnalyzers.ListObjectsAreOneBasedTitle,
        messageFormat: PlatformCopAnalyzers.ListObjectsAreOneBasedMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.ListObjectsAreOneBasedDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ListObjectsAreOneBased));

    public static readonly DiagnosticDescriptor MandatoryFieldMissingOnApiPage = new(
         id: DiagnosticIds.MandatoryFieldMissingOnApiPage,
         title: PlatformCopAnalyzers.MandatoryFieldMissingOnApiPageTitle,
         messageFormat: PlatformCopAnalyzers.MandatoryFieldMissingOnApiPageMessageFormat,
         category: Category.Design,
         defaultSeverity: DiagnosticSeverity.Warning,
         isEnabledByDefault: true,
         description: PlatformCopAnalyzers.MandatoryFieldMissingOnApiPageDescription,
         helpLinkUri: GetHelpUri(DiagnosticIds.MandatoryFieldMissingOnApiPage));

    public static readonly DiagnosticDescriptor ODataKeyFieldsShouldUseSystemId = new(
        id: DiagnosticIds.ODataKeyFieldsShouldUseSystemId,
        title: PlatformCopAnalyzers.ODataKeyFieldsShouldUseSystemIdTitle,
        messageFormat: PlatformCopAnalyzers.ODataKeyFieldsShouldUseSystemIdMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.ODataKeyFieldsShouldUseSystemIdDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ODataKeyFieldsShouldUseSystemId));

    public static readonly DiagnosticDescriptor OperatorAndPlaceholderInFilterExpression = new(
        id: DiagnosticIds.OperatorAndPlaceholderInFilterExpression,
        title: PlatformCopAnalyzers.OperatorAndPlaceholderInFilterExpressionTitle,
        messageFormat: PlatformCopAnalyzers.OperatorAndPlaceholderInFilterExpressionMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.OperatorAndPlaceholderInFilterExpressionDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.OperatorAndPlaceholderInFilterExpression));

    public static readonly DiagnosticDescriptor PageRecordArgumentMismatch = new(
        id: DiagnosticIds.PageRecordArgumentMismatch,
        title: PlatformCopAnalyzers.PageRecordArgumentMismatchTitle,
        messageFormat: PlatformCopAnalyzers.PageRecordArgumentMismatchMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.PageRecordArgumentMismatchDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PageRecordArgumentMismatch));

    public static readonly DiagnosticDescriptor PageRecordMethodRequiresSourceTable = new(
        id: DiagnosticIds.PageRecordMethodRequiresSourceTable,
        title: PlatformCopAnalyzers.PageRecordMethodRequiresSourceTableTitle,
        messageFormat: PlatformCopAnalyzers.PageRecordMethodRequiresSourceTableMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.PageRecordMethodRequiresSourceTableDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PageRecordMethodRequiresSourceTable));

    public static readonly DiagnosticDescriptor RecordGetProcedureArguments = new(
        id: DiagnosticIds.RecordGetProcedureArguments,
        title: PlatformCopAnalyzers.RecordGetProcedureArgumentsTitle,
        messageFormat: PlatformCopAnalyzers.RecordGetProcedureArgumentsMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.RecordGetProcedureArgumentsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.RecordGetProcedureArguments));

    public static readonly DiagnosticDescriptor PossibleOverflowAssigning = new(
        id: DiagnosticIds.PossibleOverflowAssigning,
        title: PlatformCopAnalyzers.PossibleOverflowAssigningTitle,
        messageFormat: PlatformCopAnalyzers.PossibleOverflowAssigningMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.PossibleOverflowAssigningDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PossibleOverflowAssigning));

    public static readonly DiagnosticDescriptor SetRangeWithFilterOperators = new(
        id: DiagnosticIds.SetRangeWithFilterOperators,
        title: PlatformCopAnalyzers.SetRangeWithFilterOperatorsTitle,
        messageFormat: PlatformCopAnalyzers.SetRangeWithFilterOperatorsMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.SetRangeWithFilterOperatorsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.SetRangeWithFilterOperators));

    public static readonly DiagnosticDescriptor TableRelationFieldLength = new(
        id: DiagnosticIds.TableRelationFieldLength,
        title: PlatformCopAnalyzers.TableRelationFieldLengthTitle,
        messageFormat: PlatformCopAnalyzers.TableRelationFieldLengthMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.TableRelationFieldLengthDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.TableRelationFieldLength));

    public static readonly DiagnosticDescriptor TemporaryRecordTriggerInvocation = new(
         id: DiagnosticIds.TemporaryRecordTriggerInvocation,
         title: PlatformCopAnalyzers.TemporaryRecordTriggerInvocationTitle,
         messageFormat: PlatformCopAnalyzers.TemporaryRecordTriggerInvocationMessageFormat,
         category: Category.Design,
         defaultSeverity: DiagnosticSeverity.Warning,
         isEnabledByDefault: true,
         description: PlatformCopAnalyzers.TemporaryRecordTriggerInvocationDescription,
         helpLinkUri: GetHelpUri(DiagnosticIds.TemporaryRecordTriggerInvocation));

    public static readonly DiagnosticDescriptor TransferFieldsNameMismatch = new(
        id: DiagnosticIds.TransferFieldsNameMismatch,
        title: PlatformCopAnalyzers.TransferFieldsNameMismatchTitle,
        messageFormat: PlatformCopAnalyzers.TransferFieldsNameMismatchMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.TransferFieldsNameMismatchDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.TransferFieldsNameMismatch));

    public static readonly DiagnosticDescriptor TransferFieldsTypeMismatch = new(
        id: DiagnosticIds.TransferFieldsTypeMismatch,
        title: PlatformCopAnalyzers.TransferFieldsTypeMismatchTitle,
        messageFormat: PlatformCopAnalyzers.TransferFieldsTypeMismatchMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.TransferFieldsTypeMismatchDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.TransferFieldsTypeMismatch));

    public static readonly DiagnosticDescriptor UsePartialRecordsOnRead = new(
        id: DiagnosticIds.UsePartialRecordsOnRead,
        title: PlatformCopAnalyzers.UsePartialRecordsOnReadTitle,
        messageFormat: PlatformCopAnalyzers.UsePartialRecordsOnReadMessageFormat,
        category: Category.Performance,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.UsePartialRecordsOnReadDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.UsePartialRecordsOnRead));

    public static readonly DiagnosticDescriptor PartialRecordsCauseJitLoad = new(
        id: DiagnosticIds.PartialRecordsCauseJitLoad,
        title: PlatformCopAnalyzers.PartialRecordsCauseJitLoadTitle,
        messageFormat: PlatformCopAnalyzers.PartialRecordsCauseJitLoadMessageFormat,
        category: Category.Performance,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.PartialRecordsCauseJitLoadDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PartialRecordsCauseJitLoad));

    public static readonly DiagnosticDescriptor UseSequentialGuid = new(
        id: DiagnosticIds.UseSequentialGuid,
        title: PlatformCopAnalyzers.UseSequentialGuidTitle,
        messageFormat: PlatformCopAnalyzers.UseSequentialGuidMessageFormat,
        category: Category.Performance,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.UseSequentialGuidDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.UseSequentialGuid));

    public static readonly DiagnosticDescriptor ReportLayoutPropertyLength = new(
        id: DiagnosticIds.ReportLayoutPropertyLength,
        title: PlatformCopAnalyzers.ReportLayoutPropertyLengthTitle,
        messageFormat: PlatformCopAnalyzers.ReportLayoutPropertyLengthMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.ReportLayoutPropertyLengthDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.ReportLayoutPropertyLength));

    public static readonly DiagnosticDescriptor DuplicateODataEntityName = new(
        id: DiagnosticIds.DuplicateODataEntityName,
        title: PlatformCopAnalyzers.DuplicateODataEntityNameTitle,
        messageFormat: PlatformCopAnalyzers.DuplicateODataEntityNameMessageFormat,
        category: Category.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.DuplicateODataEntityNameDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.DuplicateODataEntityName));

    public static readonly DiagnosticDescriptor PlaceholderArgumentCountMismatch = new(
        id: DiagnosticIds.PlaceholderArgumentCountMismatch,
        title: PlatformCopAnalyzers.PlaceholderArgumentCountMismatchTitle,
        messageFormat: PlatformCopAnalyzers.PlaceholderArgumentCountMismatchMessageFormat,
        category: Category.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.PlaceholderArgumentCountMismatchDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.PlaceholderArgumentCountMismatch));

    public static readonly DiagnosticDescriptor UseSetAutoCalcFieldsForLoops = new(
        id: DiagnosticIds.UseSetAutoCalcFieldsForLoops,
        title: PlatformCopAnalyzers.UseSetAutoCalcFieldsForLoopsTitle,
        messageFormat: PlatformCopAnalyzers.UseSetAutoCalcFieldsForLoopsMessageFormat,
        category: Category.Performance,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: PlatformCopAnalyzers.UseSetAutoCalcFieldsForLoopsDescription,
        helpLinkUri: GetHelpUri(DiagnosticIds.UseSetAutoCalcFieldsForLoops));

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