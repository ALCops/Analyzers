using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.PlatformCop.Helpers;

internal static class TransferFieldsRelations
{
    /// <summary>
    /// Finds all table relations where the given table matches the Source.
    /// </summary>
    /// <param name="table">The table symbol to match as Source.</param>
    /// <returns>All matching relations.</returns>
    public static IEnumerable<TableRelation> TryFindBySource(ITableTypeSymbol table)
    {
        foreach (var item in TableRelations)
        {
            if (Matches(item.Source, table))
                yield return item;
        }
    }

    /// <summary>
    /// Determines whether a table relation exists where the given table
    /// matches either Source or Target.
    /// </summary>
    /// <param name="table">The table symbol to match.</param>
    /// <returns>true if a matching relation exists; otherwise, false.</returns>
    public static bool HasTableRelation(ITableTypeSymbol table)
    {
        return TableRelations.Any(item =>
            Matches(item.Source, table) ||
            Matches(item.Target, table));
    }

    private static bool Matches(ObjectName configured, ITableTypeSymbol table)
    {
        var ns = table.GetContainingNamespaceQualifiedNameWithReflection() ?? string.Empty;
        var name = table.Name ?? string.Empty;

        if (StringComparer.Ordinal.Equals(configured.Name, name) &&
            StringComparer.Ordinal.Equals(configured.Namespace, ns))
            return true;

        // Keep backwards comptibility for objects without namespace
        if (string.IsNullOrEmpty(ns) &&
            StringComparer.Ordinal.Equals(configured.Name, name))
            return true;

        return false;
    }

    /// <summary>
    /// Represents a fully qualified object name with namespace and name as separate properties.
    /// </summary>
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    internal readonly struct ObjectName
    {
        public string Namespace { get; }
        public string Name { get; }

        public ObjectName(string @namespace, string name)
        {
            Namespace = @namespace;
            Name = name;
        }

        public override string ToString() =>
            string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
    }
#else
    internal readonly record struct ObjectName(string Namespace, string Name)
    {
        public override string ToString() =>
            string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
    }
#endif

    /// <summary>
    /// Represents a TransferFields relation between a source and target table,
    /// with version range indicating in which BC versions this relation was found.
    /// </summary>
    /// <param name="Source">The source table (record passed to TransferFields)</param>
    /// <param name="Target">The target table (instance calling TransferFields)</param>
    /// <param name="MinVersion">Minimum BC version where this relation was found (null = no lower bound)</param>
    /// <param name="MaxVersion">Maximum BC version where this relation was found (null = no upper bound)</param>
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    internal readonly struct TableRelation
    {
        public ObjectName Source { get; }
        public ObjectName Target { get; }
        public Version? MinVersion { get; }
        public Version? MaxVersion { get; }

        public TableRelation(ObjectName source, ObjectName target, Version? minVersion, Version? maxVersion)
        {
            Source = source;
            Target = target;
            MinVersion = minVersion;
            MaxVersion = maxVersion;
        }
    }
#else
    internal readonly record struct TableRelation(
        ObjectName Source,
        ObjectName Target,
        Version? MinVersion,
        Version? MaxVersion);
#endif

    internal static readonly ImmutableArray<TableRelation> TableRelations =
        ImmutableArray.Create(
            new TableRelation(
                new ObjectName("", "Acc. Schedule Line"),
                new ObjectName("", "Acc. Schedule Result Line"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Bank Rec. Header"),
                new ObjectName("", "Posted Bank Rec. Header"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Bank Rec. Line"),
                new ObjectName("", "Posted Bank Rec. Line"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Bank Statement Header"),
                new ObjectName("", "Issued Bank Statement Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Bank Statement Line"),
                new ObjectName("", "Issued Bank Statement Line"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Cash Document Header"),
                new ObjectName("", "Posted Cash Document Header"),
                new Version(16, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Cash Document Header CZP"),
                new ObjectName("", "Posted Cash Document Hdr. CZP"),
                new Version(17, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Cash Document Line"),
                new ObjectName("", "Posted Cash Document Line"),
                new Version(16, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Cash Document Line CZP"),
                new ObjectName("", "Posted Cash Document Line CZP"),
                new Version(17, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Column Layout"),
                new ObjectName("", "Acc. Schedule Result Column"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Credit Header"),
                new ObjectName("", "Posted Credit Header"),
                new Version(16, 0),
                new Version(20, 5)),
            new TableRelation(
                new ObjectName("", "Credit Line"),
                new ObjectName("", "Posted Credit Line"),
                new Version(16, 0),
                new Version(20, 5)),
            new TableRelation(
                new ObjectName("", "Deposit Header"),
                new ObjectName("", "Posted Deposit Header"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Detailed Fin. Charge Memo Line"),
                new ObjectName("", "Detailed Iss.Fin.Ch. Memo Line"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Detailed Reminder Line"),
                new ObjectName("", "Detailed Issued Reminder Line"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Email Outbox"),
                new ObjectName("", "Email Outbox For User"),
                new Version(17, 0),
                new Version(17, 0)),
            new TableRelation(
                new ObjectName("", "Graph Integration Record"),
                new ObjectName("", "Graph Integration Rec. Archive"),
                new Version(16, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Integration Record"),
                new ObjectName("", "Integration Record Archive"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Issued Bank Statement Line"),
                new ObjectName("", "Bank Statement Line"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Item Cross Reference"),
                new ObjectName("", "Item Reference"),
                new Version(17, 0),
                new Version(18, 4)),
            new TableRelation(
                new ObjectName("", "MS - Wallet Merchant Account"),
                new ObjectName("", "Payment Service Setup"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "MS - Wallet Merchant Template"),
                new ObjectName("", "MS - Wallet Merchant Account"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "MS - WorldPay Standard Account"),
                new ObjectName("Microsoft.Bank.Setup", "Payment Service Setup"),
                new Version(16, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("", "MS - WorldPay Std. Template"),
                new ObjectName("", "MS - WorldPay Standard Account"),
                new Version(16, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("", "O365 Sales Document"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(16, 0),
                new Version(23, 5)),
            new TableRelation(
                new ObjectName("", "O365 Sales Document"),
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new Version(16, 0),
                new Version(23, 5)),
            new TableRelation(
                new ObjectName("", "Payment Order Header"),
                new ObjectName("", "Issued Payment Order Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Payment Order Line"),
                new ObjectName("", "Issued Payment Order Line"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Posted Cash Document Hdr. CZP"),
                new ObjectName("", "Cash Document Header CZP"),
                new Version(17, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Posted Cash Document Header"),
                new ObjectName("", "Cash Document Header"),
                new Version(16, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Posted Cash Document Line"),
                new ObjectName("", "Cash Document Line"),
                new Version(16, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Posted Cash Document Line CZP"),
                new ObjectName("", "Cash Document Line CZP"),
                new Version(17, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Purch. Advance Letter Header"),
                new ObjectName("", "Purchase Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Purch. Cr. Memo Hdr."),
                new ObjectName("", "Purch. Inv. Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Purch. Inv. Header"),
                new ObjectName("", "Purch. Cr. Memo Hdr."),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Purch. Inv. Line"),
                new ObjectName("", "Purch. Cr. Memo Line"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Purchase Header"),
                new ObjectName("", "Purch. Advance Letter Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Reservation Entry"),
                new ObjectName("", "Reservation Entry Buffer"),
                new Version(16, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Sales Advance Letter Header"),
                new ObjectName("", "Sales Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Sales Cr.Memo Header"),
                new ObjectName("", "Sales Invoice Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Sales Cr.Memo Line"),
                new ObjectName("", "Sales Invoice Line"),
                new Version(16, 0),
                new Version(17, 5)),
            new TableRelation(
                new ObjectName("", "Sales Header"),
                new ObjectName("", "Sales Advance Letter Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Sales Invoice Header"),
                new ObjectName("", "Sales Cr.Memo Header"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Sales Invoice Line"),
                new ObjectName("", "Sales Cr.Memo Line"),
                new Version(16, 0),
                new Version(21, 5)),
            new TableRelation(
                new ObjectName("", "Sent Email"),
                new ObjectName("", "Sent Email For User"),
                new Version(17, 0),
                new Version(17, 0)),
            new TableRelation(
                new ObjectName("", "Service Cr.Memo Header"),
                new ObjectName("", "Service Invoice Header"),
                new Version(16, 0),
                new Version(17, 5)),
            new TableRelation(
                new ObjectName("", "Service Cr.Memo Line"),
                new ObjectName("", "Sales Line"),
                new Version(22, 1),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Service Cr.Memo Line"),
                new ObjectName("", "Service Invoice Line"),
                new Version(16, 0),
                new Version(17, 5)),
            new TableRelation(
                new ObjectName("", "Service Invoice Header"),
                new ObjectName("", "Service Cr.Memo Header"),
                new Version(16, 0),
                new Version(17, 5)),
            new TableRelation(
                new ObjectName("", "Service Invoice Line"),
                new ObjectName("", "Sales Line"),
                new Version(22, 1),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Stg Data Exch Def CA"),
                new ObjectName("", "Data Exch. Def"),
                new Version(16, 2),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Stg Data Exch Def MX"),
                new ObjectName("", "Data Exch. Def"),
                new Version(16, 2),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Stg Data Exch Def US"),
                new ObjectName("", "Data Exch. Def"),
                new Version(16, 2),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Stg Intrastat Jnl. Line"),
                new ObjectName("", "Intrastat Jnl. Line"),
                new Version(18, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Stg Item Journal Line"),
                new ObjectName("", "Item Journal Line"),
                new Version(18, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Stg Item Ledger Entry"),
                new ObjectName("", "Item Ledger Entry"),
                new Version(18, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Stg VAT Posting Setup"),
                new ObjectName("", "VAT Posting Setup"),
                new Version(16, 0),
                new Version(19, 5)),
            new TableRelation(
                new ObjectName("", "Tax Area Buffer"),
                new ObjectName("", "Native - API Tax Setup"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("", "Tax Group Buffer"),
                new ObjectName("", "Native - API Tax Setup"),
                new Version(16, 0),
                new Version(22, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Assembly.Document", "Assembly Header"),
                new ObjectName("Microsoft.Assembly.History", "Posted Assembly Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Assembly.Document", "Assembly Line"),
                new ObjectName("Microsoft.Assembly.History", "Posted Assembly Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Assembly.History", "Posted Assembly Header"),
                new ObjectName("Microsoft.Assembly.Document", "Assembly Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Assembly.History", "Posted Assembly Line"),
                new ObjectName("Microsoft.Assembly.Document", "Assembly Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.BankAccount", "Bank Account"),
                new ObjectName("Microsoft.CRM.Contact", "Contact"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.BankAccount", "Online Bank Acc. Link"),
                new ObjectName("Microsoft.Bank.StatementImport.Yodlee", "MS - Yodlee Bank Acc. Link"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Deposit", "Bank Deposit Header"),
                new ObjectName("Microsoft.Bank.Deposit", "Posted Bank Deposit Header"),
                new Version(20, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.DirectDebit", "Direct Debit Collection Entry"),
                new ObjectName("Microsoft.Bank.DirectDebit", "Direct Debit Collection Buffer"),
                new Version(22, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Documents", "Bank Statement Header CZB"),
                new ObjectName("Microsoft.Bank.Documents", "Iss. Bank Statement Header CZB"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Documents", "Bank Statement Line CZB"),
                new ObjectName("Microsoft.Bank.Documents", "Iss. Bank Statement Line CZB"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Documents", "Iss. Bank Statement Line CZB"),
                new ObjectName("Microsoft.Bank.Documents", "Bank Statement Line CZB"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Documents", "Payment Order Header CZB"),
                new ObjectName("Microsoft.Bank.Documents", "Iss. Payment Order Header CZB"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Documents", "Payment Order Line CZB"),
                new ObjectName("Microsoft.Bank.Documents", "Iss. Payment Order Line CZB"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.ElectronicFundsTransfer", "EFT Export"),
                new ObjectName("Microsoft.Bank.ElectronicFundsTransfer", "EFT Export Workset"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Customer Bill Header"),
                new ObjectName("Microsoft.Bank.Payment", "Issued Customer Bill Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Customer Bill Line"),
                new ObjectName("Microsoft.Bank.Payment", "Issued Customer Bill Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Payment Header"),
                new ObjectName("Microsoft.Bank.Payment", "Payment Header Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Payment Header FR"),
                new ObjectName("Microsoft.Bank.Payment", "Payment Header Archive FR"),
                new Version(28, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Payment Line"),
                new ObjectName("Microsoft.Bank.Payment", "Payment Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Payment Line FR"),
                new ObjectName("Microsoft.Bank.Payment", "Payment Line Archive FR"),
                new Version(28, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Ref. Payment - Exported"),
                new ObjectName("Microsoft.Bank.Payment", "Ref. Payment - Exported Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Ref. Payment - Exported Buffer"),
                new ObjectName("Microsoft.Bank.Payment", "Ref. Payment - Exported"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Vendor Bill Header"),
                new ObjectName("Microsoft.Bank.Payment", "Posted Vendor Bill Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Vendor Bill Line"),
                new ObjectName("Microsoft.Bank.Payment", "Posted Vendor Bill Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Payment", "Vendor Bill Withholding Tax"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Tmp Withholding Contribution"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.PayPal", "MS - PayPal Standard Account"),
                new ObjectName("Microsoft.Bank.Setup", "Payment Service Setup"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.PayPal", "MS - PayPal Standard Template"),
                new ObjectName("Microsoft.Bank.PayPal", "MS - PayPal Standard Account"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Applied Payment Entry"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Payment Application Proposal"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Posted Payment Recon. Hdr"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation"),
                new ObjectName("Microsoft.Bank.Statement", "Bank Account Statement"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation Line"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Posted Payment Recon. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation Line"),
                new ObjectName("Microsoft.Bank.Statement", "Bank Account Statement Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Rec. Line"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Rec. Sub-line"),
                new Version(16, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Payment Application Proposal"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Applied Payment Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Posted Payment Recon. Hdr"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation"),
                new Version(21, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Statement", "Bank Account Statement"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation"),
                new Version(17, 2),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Statement", "Bank Account Statement Line"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation Line"),
                new Version(17, 2),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.StatementImport.Yodlee", "MS - Yodlee Bank Acc. Link"),
                new ObjectName("Microsoft.Bank.BankAccount", "Online Bank Acc. Link"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Bank.StatementImport.Yodlee", "MS - Yodlee Bank Service Setup"),
                new ObjectName("Microsoft.Bank.StatementImport.Yodlee", "MS - Yodlee Bank Session"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.CRM.BusinessRelation", "Contact Business Relation"),
                new ObjectName("Microsoft.CRM.Outlook", "Office Contact Details"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.CRM.Contact", "Contact"),
                new ObjectName("Microsoft.Bank.BankAccount", "Bank Account"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.CRM.Contact", "Contact"),
                new ObjectName("Microsoft.Purchases.Vendor", "Vendor"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.CRM.Contact", "Contact"),
                new ObjectName("Microsoft.Sales.Customer", "Customer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.DataMigration", "Replication Record Link Buffer"),
                new ObjectName("System.Environment.Configuration", "Record Link"),
                new Version(27, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.DataMigration.BC", "Stg Incoming Document"),
                new ObjectName("Microsoft.EServices.EDocument", "Incoming Document"),
                new Version(16, 0),
                new Version(23, 0)),
            new TableRelation(
                new ObjectName("Microsoft.EServices.EDocument", "Incoming Document Attachment"),
                new ObjectName("Microsoft.EServices.EDocument", "Inc. Doc. Attachment Overview"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.EServices.EDocument", "Incoming Document Attachment"),
                new ObjectName("Microsoft.Integration.Graph", "Attachment Entity Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.EServices.EDocument", "SAT Payment Method"),
                new ObjectName("Microsoft.EServices.EDocument", "SAT Payment Term"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.EServices.EDocument", "SAT Payment Term"),
                new ObjectName("Microsoft.EServices.EDocument", "SAT Payment Method"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.ExpenseAgent", "Expense Report Header"),
                new ObjectName("Microsoft.ExpenseAgent", "Posted Expense Report Header"),
                new Version(28, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.ExpenseAgent", "Expense Report Line"),
                new ObjectName("Microsoft.ExpenseAgent", "Posted Expense Report Line"),
                new Version(28, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.ExpenseAgent", "Expense Report Line Item"),
                new ObjectName("Microsoft.ExpenseAgent", "Posted Exp. Rep. Line Item"),
                new Version(28, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.ExpenseAgent", "Expense Report Line Particip."),
                new ObjectName("Microsoft.ExpenseAgent", "Posted Exp. Rep. Line Particip"),
                new Version(28, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.ExpenseAgent", "Expense Report Line Per Diem"),
                new ObjectName("Microsoft.ExpenseAgent", "Posted Exp. Rep. Line Per Diem"),
                new Version(28, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Analysis", "Analysis by Dim. Parameters"),
                new ObjectName("Microsoft.Finance.Analysis", "Analysis by Dim. User Param."),
                new Version(16, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Analysis", "Analysis by Dim. User Param."),
                new ObjectName("Microsoft.Finance.Analysis", "Analysis by Dim. Parameters"),
                new Version(16, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Compensations", "Compensation Header CZC"),
                new ObjectName("Microsoft.Finance.Compensations", "Posted Compensation Header CZC"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Compensations", "Compensation Line CZC"),
                new ObjectName("Microsoft.Finance.Compensations", "Posted Compensation Line CZC"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Consolidation", "BAS XML Field ID"),
                new ObjectName("Microsoft.Finance.Consolidation", "BAS XML Field ID Setup"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Consolidation", "BAS XML Field ID Setup"),
                new ObjectName("Microsoft.Finance.Consolidation", "BAS XML Field ID"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Header"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Header Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Header"),
                new ObjectName("Microsoft.Finance.Deferral", "Posted Deferral Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line"),
                new ObjectName("Microsoft.Finance.Deferral", "Posted Deferral Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line Archive"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Posted Deferral Header"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Posted Deferral Line"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Dimension", "Dimension Set Entry"),
                new ObjectName("Microsoft.Finance.Dimension", "Dimension Set Entry Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Dimension", "Dimension Set Entry Buffer"),
                new ObjectName("Microsoft.Finance.Dimension", "Dimension Set Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.FinancialReports", "Acc. Schedule Line"),
                new ObjectName("Microsoft.Finance.FinancialReports", "Acc. Schedule Result Line CZL"),
                new Version(18, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.FinancialReports", "Column Layout"),
                new ObjectName("Microsoft.Finance.FinancialReports", "Acc. Schedule Result Col. CZL"),
                new Version(18, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Account", "G/L Account"),
                new ObjectName("Microsoft.Finance.Analysis", "G/L Account (Analysis View)"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Batch"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Posted Gen. Journal Batch"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Posted Gen. Journal Line"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Standard General Journal Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line"),
                new ObjectName("Microsoft.Purchases.Payables", "Waiting Journal"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Posted Gen. Journal Line"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Standard General Journal Line"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Ledger", "G/L Entry"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Ledger", "G/L Entry Posting Preview"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Ledger", "G/L Entry"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Review", "G/L Entry Application Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Base", "E-Commerce Merchant"),
                new ObjectName("Microsoft.Finance.GST.Base", "E-Comm. Merchant"),
                new Version(19, 2),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Distribution", "GST Distribution Header"),
                new ObjectName("Microsoft.Finance.GST.Distribution", "Posted GST Distribution Header"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Distribution", "GST Distribution Line"),
                new ObjectName("Microsoft.Finance.GST.Distribution", "Posted GST Distribution Line"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Distribution", "GST Payment Buffer"),
                new ObjectName("Microsoft.Finance.GST.Distribution", "Posted Settlement Entries"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Distribution", "Posted GST Distribution Line"),
                new ObjectName("Microsoft.Finance.GST.Distribution", "GST Distribution Line"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Subcontracting", "Applied Delivery Challan"),
                new ObjectName("Microsoft.Finance.GST.Subcontracting", "Posted Applied DeliveryChallan"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Subcontracting", "Delivery Challan Line"),
                new ObjectName("Microsoft.Finance.GST.Subcontracting", "GST Liability Line"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GST.Subcontracting", "GST Liability Line"),
                new ObjectName("Microsoft.Finance.GST.Subcontracting", "Posted GST Liability Line"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Cartera Doc."),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Closed Cartera Doc."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Cartera Doc."),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Posted Cartera Doc."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Closed Cartera Doc."),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Cartera Doc."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "CV Ledger Entry Buffer"),
                new ObjectName("Microsoft.Sales.Receivables", "Cust. Ledger Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new ObjectName("Microsoft.HumanResources.Payables", "Detailed Employee Ledger Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new ObjectName("Microsoft.Purchases.Payables", "Detailed Vendor Ledg. Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new ObjectName("Microsoft.Sales.Receivables", "Detailed Cust. Ledg. Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Posted Cartera Doc."),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Closed Cartera Doc."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.SalesTax", "Tax Area"),
                new ObjectName("Microsoft.Integration.Entity", "Tax Area Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.SalesTax", "Tax Group"),
                new ObjectName("Microsoft.Integration.Entity", "Tax Group Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.VAT.Ledger", "No Taxable Entry"),
                new ObjectName("Microsoft.Finance.VAT.Ledger", "VAT Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.VAT.Ledger", "VAT Entry"),
                new ObjectName("Microsoft.Finance.VAT.Ledger", "VAT Entry Posting Preview"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Business Posting Group"),
                new ObjectName("Microsoft.Integration.Entity", "Tax Area Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Product Posting Group"),
                new ObjectName("Microsoft.Integration.Entity", "Tax Group Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Setup Posting Groups"),
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Posting Setup"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Finance.WithholdingTax", "WHT Entry"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Temp WHT Entry - EFiling"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.FixedAssets.Ledger", "FA Ledger Entry"),
                new ObjectName("Microsoft.FixedAssets.Repair", "FA Ledg. Entry w. Issue"),
                new Version(19, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Foundation.Comment", "Comment Line"),
                new ObjectName("Microsoft.Foundation.Comment", "Comment Line Archive"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Foundation.Comment", "Comment Line Archive"),
                new ObjectName("Microsoft.Foundation.Comment", "Comment Line"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Foundation.Company", "Company Information"),
                new ObjectName("System.IO", "Config. Setup"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.HumanResources.Employee", "Employee"),
                new ObjectName("", "Company Officials"),
                new Version(16, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("Microsoft.HumanResources.Payables", "Detailed Employee Ledger Entry"),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.D365Sales", "CDS Available Virtual Table"),
                new ObjectName("Microsoft.Integration.D365Sales", "CDS Av. Virtual Table Buffer"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Area Buffer"),
                new ObjectName("Microsoft.Finance.SalesTax", "Tax Area"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Area Buffer"),
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Business Posting Group"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Group Buffer"),
                new ObjectName("Microsoft.Finance.SalesTax", "Tax Group"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Group Buffer"),
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Product Posting Group"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.FieldService", "FS Connection Setup"),
                new ObjectName("Microsoft.Integration.DynamicsFieldService", "FS Connection Setup"),
                new Version(24, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Graph", "Attachment Entity Buffer"),
                new ObjectName("Microsoft.Integration.Graph", "Unlinked Attachment"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Graph", "Unlinked Attachment"),
                new ObjectName("Microsoft.Integration.Graph", "Attachment Entity Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Shopify", "Shpfy Registered Store"),
                new ObjectName("Microsoft.Integration.Shopify", "Shpfy Registered Store New"),
                new Version(21, 0),
                new Version(23, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Comment", "IC Comment Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Comment Line"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Dimension", "IC Document Dimension"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Document Dimension"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Dimension", "IC Inbox/Outbox Jnl. Line Dim."),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC InOut Jnl. Line Dim."),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Jnl. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Purch. Header"),
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Purch. Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Trans."),
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Transaction"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Jnl. Line"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Jnl. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Jnl. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Header"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Purch Header"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Header"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Purch. Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Purchase Line"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Purch. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Sales Header"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Sales Line"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Transaction"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Transaction"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Transaction"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Trans."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Jnl. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Purch. Hdr"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Purch. Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Jnl. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Purch. Hdr"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Purch. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Transaction"),
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Trans."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Expect. Phys. Inv. Track. Line"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Exp. Phys. Invt. Tracking"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Phys. Inventory Comment Line"),
                new ObjectName("Microsoft.Inventory.Counting.Comment", "Phys. Invt. Comment Line"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Phys. Inventory Order Header"),
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Order Header"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Phys. Inventory Order Line"),
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Order Line"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Phys. Invt. Diff. List Buffer"),
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Count Buffer"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Phys. Invt. Recording Header"),
                new ObjectName("Microsoft.Inventory.Counting.Recording", "Phys. Invt. Record Header"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Phys. Invt. Recording Line"),
                new ObjectName("Microsoft.Inventory.Counting.Recording", "Phys. Invt. Record Line"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Phys. Invt. Tracking Buffer"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Phys. Invt. Tracking"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Post. Exp. Ph. In. Track. Line"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Pstd. Exp. Phys. Invt. Track"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Post. Phys. Invt. Order Header"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Order Hdr"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Posted Phys. Invt. Order Line"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Order Line"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Posted Phys. Invt. Rec. Header"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Record Hdr"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Posted Phys. Invt. Rec. Line"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Record Line"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting", "Posted Phys. Invt. Track. Line"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Pstd. Phys. Invt. Tracking"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Order Header"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Order Hdr"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Order Line"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Order Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Order Hdr"),
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Order Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Recording", "Phys. Invt. Record Header"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Record Hdr"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Recording", "Phys. Invt. Record Line"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Record Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Exp. Invt. Order Tracking"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Pstd.Exp.Invt.Order.Tracking"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Exp. Phys. Invt. Tracking"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Exp. Invt. Order Tracking"),
                new Version(24, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Exp. Phys. Invt. Tracking"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Pstd. Exp. Phys. Invt. Track"),
                new Version(16, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Phys. Invt. Tracking"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Invt. Order Tracking"),
                new Version(24, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Pstd. Exp. Phys. Invt. Track"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Pstd.Exp.Invt.Order.Tracking"),
                new Version(24, 0),
                new Version(26, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Receipt Header"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Header"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Receipt Line"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Line"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Shipment Header"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Header"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Shipment Line"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Line"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Intrastat", "Intrastat Jnl. Line"),
                new ObjectName("", "Intra - form Buffer"),
                new Version(16, 0),
                new Version(24, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Item", "Copy Item Buffer"),
                new ObjectName("Microsoft.Inventory.Item", "Copy Item Parameters"),
                new Version(16, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Item", "Copy Item Parameters"),
                new ObjectName("Microsoft.Inventory.Item", "Copy Item Buffer"),
                new Version(16, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Item", "Item"),
                new ObjectName("Microsoft.Inventory.Item", "Item Templ."),
                new Version(19, 3),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Item", "Item Entry Relation"),
                new ObjectName("Microsoft.Warehouse.Ledger", "Whse. Item Entry Relation"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Journal", "Item Journal Line"),
                new ObjectName("Microsoft.Inventory.Journal", "Standard Item Journal Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Journal", "Standard Item Journal Line"),
                new ObjectName("Microsoft.Inventory.Journal", "Item Journal Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Ledger", "Item Application Entry"),
                new ObjectName("Microsoft.Inventory.Ledger", "Item Application Entry History"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Tracking", "Matched Order Line"),
                new ObjectName("Microsoft.Inventory.Tracking", "Posted Matched Order Line"),
                new Version(28, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Tracking", "Reservation Entry"),
                new ObjectName("Microsoft.Inventory.Tracking", "Tracking Specification"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Tracking", "Tracking Specification"),
                new ObjectName("Microsoft.Inventory.Tracking", "Reservation Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Tracking", "Tracking Specification"),
                new ObjectName("Microsoft.Warehouse.Tracking", "Whse. Item Tracking Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.ProductionBOM", "Production BOM Comment Line"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Comp. Cmt Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Comment Line"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Rtng Comment Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Personnel"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Routing Personnel"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Quality Measure"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Rtng Qlty Meas."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Tool"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Routing Tool"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Pricing.PriceList", "Price List Line"),
                new ObjectName("Microsoft.Pricing.Worksheet", "Price Worksheet Line"),
                new Version(18, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Pricing.Worksheet", "Price Worksheet Line"),
                new ObjectName("Microsoft.Pricing.PriceList", "Price List Line"),
                new Version(18, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Archive"),
                new ObjectName("Microsoft.Projects.Project.Job", "Job"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Planning Line Archive"),
                new ObjectName("Microsoft.Projects.Project.Planning", "Job Planning Line"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Task Archive"),
                new ObjectName("Microsoft.Projects.Project.Job", "Job Task"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Job", "Job"),
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Archive"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Job", "Job Task"),
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Task Archive"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Planning", "Job Planning Line"),
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Planning Line Archive"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Cmt. Line Archive"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Comment Line"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Comment Line"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Cmt. Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail"),
                new ObjectName("Microsoft.Integration.Graph", "Employee Time Reg Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail Archive"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Header"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Header Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Line"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Line Archive"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Line"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Archive", "Purchase Header Archive"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Archive", "Purchase Line Archive"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Comment", "Purch. Comment Line"),
                new ObjectName("Microsoft.Purchases.Archive", "Purch. Comment Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Delivery Reminder Header"),
                new ObjectName("Microsoft.Purchases.Document", "Issued Deliv. Reminder Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Delivery Reminder Line"),
                new ObjectName("Microsoft.Purchases.Document", "Issued Deliv. Reminder Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Cr. Memo Entity Buffer"),
                new Version(22, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Entity Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purchase Order Entity Buffer"),
                new Version(17, 5),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Purchases.Archive", "Purchase Header Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Hdr."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Purchases.History", "Purch. Rcpt. Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Purchases.History", "Return Shipment Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Line Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Purchases.Archive", "Purchase Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Purchases.History", "Purch. Rcpt. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Purchases.History", "Return Shipment Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Posted Payment Order"),
                new ObjectName("Microsoft.Purchases.Payables", "Closed Payment Order"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Hdr."),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Purch. Tax Cr. Memo Hdr."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Hdr."),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Purch. Tax Inv. Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Hdr."),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Cr. Memo Entity Buffer"),
                new Version(22, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Hdr."),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Line"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Purch. Tax Cr. Memo Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Line"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Purch. Tax Inv. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Line"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Line Aggregate"),
                new Version(22, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Header"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Purch. Tax Inv. Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Entity Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Header"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Line"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Purch. Tax Inv. Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Line"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Line Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Rcpt. Header"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Rcpt. Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Return Shipment Header"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Return Shipment Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Payables", "Detailed Vendor Ledg. Entry"),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Payables", "Payment Order"),
                new ObjectName("Microsoft.Purchases.History", "Posted Payment Order"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Payables", "Vendor Ledger Entry"),
                new ObjectName("Microsoft.Purchases.Payables", "Vendor Ledger Entry Buffer"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Payables", "Waiting Journal"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Vendor", "Vendor"),
                new ObjectName("Microsoft.CRM.Contact", "Contact"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Vendor", "Vendor"),
                new ObjectName("Microsoft.Purchases.Vendor", "Vendor Templ."),
                new Version(18, 5),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Archive", "Sales Comment Line Archive"),
                new ObjectName("Microsoft.Sales.Comment", "Sales Comment Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Archive", "Sales Header Archive"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Archive", "Sales Line Archive"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Comment", "Sales Comment Line"),
                new ObjectName("Microsoft.Sales.Archive", "Sales Comment Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Customer", "Customer"),
                new ObjectName("Microsoft.CRM.Contact", "Contact"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Customer", "Customer"),
                new ObjectName("Microsoft.Sales.Customer", "Customer Templ."),
                new Version(18, 5),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("", "O365 Sales Document"),
                new Version(16, 0),
                new Version(23, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Cr. Memo Entity Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Entity Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Order Entity Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Quote Entity Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Sales.Archive", "Sales Header Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Sales.History", "Return Receipt Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Line Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Sales.Archive", "Sales Line Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Sales.History", "Return Receipt Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Finance Charge Memo Header"),
                new ObjectName("Microsoft.Sales.FinanceCharge", "Issued Fin. Charge Memo Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Finance Charge Memo Header"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Finance Charge Memo Line"),
                new ObjectName("Microsoft.Sales.FinanceCharge", "Issued Fin. Charge Memo Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Finance Charge Memo Line"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Issued Fin. Charge Memo Header"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Issued Fin. Charge Memo Line"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Posted Bill Group"),
                new ObjectName("Microsoft.Sales.Receivables", "Closed Bill Group"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Return Receipt Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Return Receipt Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Sales Tax Cr.Memo Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Sales Tax Invoice Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Cr. Memo Entity Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Sales Tax Cr.Memo Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Sales Tax Invoice Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Line Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("", "O365 Sales Document"),
                new Version(16, 0),
                new Version(23, 5)),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Sales Tax Invoice Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Entity Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.Finance.WithholdingTax", "Sales Tax Invoice Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Line Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Header"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Header"),
                new Version(19, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Line"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Line"),
                new Version(19, 1),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Receivables", "Bill Group"),
                new ObjectName("Microsoft.Sales.History", "Posted Bill Group"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Receivables", "Cust. Ledger Entry"),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "CV Ledger Entry Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Receivables", "Detailed Cust. Ledg. Entry"),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Reminder", "Reminder Header"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Reminder", "Reminder Line"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Archive", "Service Comment Line Archive"),
                new ObjectName("Microsoft.Service.Comment", "Service Comment Line"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Archive", "Service Header Archive"),
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Archive", "Service Item Line Archive"),
                new ObjectName("Microsoft.Service.Document", "Service Item Line"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Archive", "Service Line Archive"),
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Archive", "Service Order Allocat. Archive"),
                new ObjectName("Microsoft.Service.Document", "Service Order Allocation"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Comment", "Service Comment Line"),
                new ObjectName("Microsoft.Service.Archive", "Service Comment Line Archive"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Comment", "Service Comment Line"),
                new ObjectName("Microsoft.Service.Contract", "Filed Serv. Contract Cmt. Line"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Contract/Service Discount"),
                new ObjectName("Microsoft.Service.Contract", "Filed Contract/Serv. Discount"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Service Contract Header"),
                new ObjectName("Microsoft.Service.Contract", "Filed Service Contract Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Service Contract Line"),
                new ObjectName("Microsoft.Service.Contract", "Filed Contract Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Service Hour"),
                new ObjectName("Microsoft.Service.Contract", "Filed Contract Service Hour"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.Archive", "Service Header Archive"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.History", "Service Invoice Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.History", "Service Shipment Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Item Line"),
                new ObjectName("Microsoft.Service.Archive", "Service Item Line Archive"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Item Line"),
                new ObjectName("Microsoft.Service.History", "Service Shipment Item Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.Archive", "Service Line Archive"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.History", "Service Invoice Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.History", "Service Shipment Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Order Allocation"),
                new ObjectName("Microsoft.Service.Archive", "Service Order Allocat. Archive"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Header"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Header"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Header"),
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Line"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Line"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Invoice Header"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Invoice Header"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Invoice Header"),
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Invoice Line"),
                new ObjectName("Microsoft.EServices.EDocument", "Document Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Service.History", "Service Invoice Line"),
                new ObjectName("Microsoft.EServices.EDocument", "E-Invoice Export Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Billing Line"),
                new ObjectName("Microsoft.SubscriptionBilling", "Billing Line Archive"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Billing Line Archive"),
                new ObjectName("Microsoft.SubscriptionBilling", "Billing Line"),
                new Version(25, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Customer Contract"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(25, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Customer Subscription Contract"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new Version(26, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Service Comm. Archive"),
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Service Commitment"),
                new Version(25, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Service Commitment"),
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Service Comm. Archive"),
                new Version(25, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Sub. Line Archive"),
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Subscription Line"),
                new Version(26, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Subscription Line"),
                new ObjectName("Microsoft.SubscriptionBilling", "Sales Sub. Line Archive"),
                new Version(26, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Service Commitment"),
                new ObjectName("Microsoft.SubscriptionBilling", "Planned Service Commitment"),
                new Version(25, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Subscription Line"),
                new ObjectName("Microsoft.SubscriptionBilling", "Planned Subscription Line"),
                new Version(26, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Vendor Contract"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new Version(25, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("Microsoft.SubscriptionBilling", "Vendor Subscription Contract"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new Version(26, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sustainability.ESGReporting", "Sust. ESG Reporting Name"),
                new ObjectName("Microsoft.Sustainability.ESGReporting", "Sust. Posted ESG Report Header"),
                new Version(27, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Sustainability.Journal", "Sustainability Jnl. Line"),
                new ObjectName("Microsoft.Sustainability.Ledger", "Sustainability Ledger Entry"),
                new Version(24, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.Activity.History", "Registered Whse. Activity Hdr."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Pick Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Put-away Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Registered Invt. Movement Hdr."),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.Activity.History", "Registered Whse. Activity Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Pick Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Put-away Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Registered Invt. Movement Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Document", "Warehouse Receipt Header"),
                new ObjectName("Microsoft.Warehouse.History", "Posted Whse. Receipt Header"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Document", "Warehouse Receipt Line"),
                new ObjectName("Microsoft.Warehouse.History", "Posted Whse. Receipt Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Document", "Warehouse Shipment Line"),
                new ObjectName("Microsoft.Warehouse.History", "Posted Whse. Shipment Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.GateEntry", "Gate Entry Attachment"),
                new ObjectName("Microsoft.Warehouse.GateEntry", "Posted Gate Entry Attachment"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.GateEntry", "Gate Entry Header"),
                new ObjectName("Microsoft.Warehouse.GateEntry", "Posted Gate Entry Header"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.GateEntry", "Gate Entry Line"),
                new ObjectName("Microsoft.Warehouse.GateEntry", "Posted Gate Entry Line"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Tracking", "Whse. Item Tracking Line"),
                new ObjectName("Microsoft.Inventory.Tracking", "Reservation Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Tracking", "Whse. Item Tracking Line"),
                new ObjectName("Microsoft.Inventory.Tracking", "Tracking Specification"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Apps", "NAV App Tenant Operation"),
                new ObjectName("System.Apps", "Extension Deployment Status"),
                new Version(17, 0),
                null),
            new TableRelation(
                new ObjectName("System.Automation", "Approval Comment Line"),
                new ObjectName("System.Automation", "Posted Approval Comment Line"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Automation", "Approval Entry"),
                new ObjectName("System.Automation", "Posted Approval Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Automation", "Workflow - Record Change"),
                new ObjectName("System.Automation", "Workflow Record Change Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Automation", "Workflow Step Argument"),
                new ObjectName("System.Automation", "Workflow Step Argument Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Automation", "Workflow Step Argument Archive"),
                new ObjectName("System.Automation", "Workflow Step Argument"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Automation", "Workflow Step Instance"),
                new ObjectName("System.Automation", "Workflow Step Instance Archive"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Azure.Identity", "Custom Permission Set In Plan"),
                new ObjectName("System.Azure.Identity", "Permission Set In Plan Buffer"),
                new Version(22, 0),
                null),
            new TableRelation(
                new ObjectName("System.Azure.Identity", "Default Permission Set In Plan"),
                new ObjectName("System.Azure.Identity", "Custom Permission Set In Plan"),
                new Version(20, 0),
                null),
            new TableRelation(
                new ObjectName("System.Azure.Identity", "Default Permission Set In Plan"),
                new ObjectName("System.Azure.Identity", "Permission Set In Plan Buffer"),
                new Version(22, 0),
                null),
            new TableRelation(
                new ObjectName("System.Azure.Identity", "Permission Set In Plan Buffer"),
                new ObjectName("System.Azure.Identity", "Custom Permission Set In Plan"),
                new Version(22, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("System.Email", "Email Outbox"),
                new ObjectName("System.Email", "Sent Email"),
                new Version(17, 1),
                null),
            new TableRelation(
                new ObjectName("System.Email", "Email Scenario Attachments"),
                new ObjectName("System.Email", "Email Attachments"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("System.Environment", "Table Information"),
                new ObjectName("System.DataAdministration", "Table Information Cache"),
                new Version(18, 2),
                null),
            new TableRelation(
                new ObjectName("System.Environment.Configuration", "Extra Settings"),
                new ObjectName("System.Environment.Configuration", "Application User Settings"),
                new Version(20, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("System.Environment.Configuration", "Notification Entry"),
                new ObjectName("System.Environment.Configuration", "Sent Notification Entry"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Feedback", "Onboarding Signal"),
                new ObjectName("System.Feedback", "Onboarding Signal Buffer"),
                new Version(22, 2),
                null),
            new TableRelation(
                new ObjectName("System.Globalization", "Translation"),
                new ObjectName("System.Globalization", "Translation Buffer"),
                new Version(27, 2),
                null),
            new TableRelation(
                new ObjectName("System.Integration", "Data Migration Error"),
                new ObjectName("", "GP Migration Error Overview"),
                new Version(23, 0),
                null),
            new TableRelation(
                new ObjectName("System.Integration", "Data Migration Error"),
                new ObjectName("Microsoft.DataMigration.SL", "SL Migration Error Overview"),
                new Version(25, 2),
                null),
            new TableRelation(
                new ObjectName("System.Integration", "Intelligent Cloud Status"),
                new ObjectName("Microsoft.DataMigration", "Cloud Migration Override Log"),
                new Version(23, 1),
                null),
            new TableRelation(
                new ObjectName("System.Integration", "Tenant Web Service"),
                new ObjectName("System.Integration", "Web Service Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Integration", "Web Service"),
                new ObjectName("System.Integration", "Web Service Aggregate"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Integration", "Web Service Aggregate"),
                new ObjectName("System.Integration", "Tenant Web Service"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Integration", "Web Service Aggregate"),
                new ObjectName("System.Integration", "Web Service"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Integration.Word", "Word Templates Related Buffer"),
                new ObjectName("System.Integration.Word", "Word Templates Related Table"),
                new Version(22, 0),
                null),
            new TableRelation(
                new ObjectName("System.Integration.Word", "Word Templates Related Table"),
                new ObjectName("System.Integration.Word", "Word Templates Related Buffer"),
                new Version(22, 0),
                null),
            new TableRelation(
                new ObjectName("System.IO", "Config. Field Mapping"),
                new ObjectName("System.IO", "Config. Field Map"),
                new Version(19, 0),
                new Version(25, 5)),
            new TableRelation(
                new ObjectName("System.IO", "Config. Setup"),
                new ObjectName("Microsoft.Foundation.Company", "Company Information"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.Security.AccessControl", "Aggregate Permission Set"),
                new ObjectName("System.Security.AccessControl", "Permission Set Buffer"),
                new Version(16, 0),
                null),
            new TableRelation(
                new ObjectName("System.TestTools.AITestToolkit", "AIT Log Entry"),
                new ObjectName("System.TestTools.AITestToolkit", "Agent Task Log"),
                new Version(28, 0),
                null),
            new TableRelation(
                new ObjectName("System.Threading", "Job Queue Entry"),
                new ObjectName("System.Threading", "Job Queue Entry Buffer"),
                new Version(18, 0),
                null),
            new TableRelation(
                new ObjectName("System.Utilities", "Date"),
                new ObjectName("System.DateTime", "Date Lookup Buffer"),
                new Version(16, 0),
                null)
        );
}