using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.PlatformCop.Helpers;

internal static class TransferFieldsRelations
{
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

    internal readonly struct TableRelation
    {
        public ObjectName Table { get; }
        public ObjectName RelatedTable { get; }

        public TableRelation(ObjectName table, ObjectName relatedTable)
        {
            Table = table;
            RelatedTable = relatedTable;
        }
    }
#else
    internal readonly record struct ObjectName(string Namespace, string Name)
    {
        public override string ToString() =>
            string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
    }

    internal readonly record struct TableRelation(ObjectName Table, ObjectName RelatedTable);
#endif

    internal static readonly ImmutableArray<TableRelation> TableRelations =
        ImmutableArray.Create(
            new TableRelation(
                new ObjectName("Microsoft.Assembly.History", "Posted Assembly Header"),
                new ObjectName("Microsoft.Assembly.Document", "Assembly Header")),
            new TableRelation(
                new ObjectName("Microsoft.Assembly.History", "Posted Assembly Line"),
                new ObjectName("Microsoft.Assembly.Document", "Assembly Line")),
            new TableRelation(
                new ObjectName("Microsoft.Bank.BankAccount", "Bank Account"),
                new ObjectName("Microsoft.CRM.Contact", "Contact")),
            new TableRelation(
                new ObjectName("Microsoft.Bank.DirectDebit", "Direct Debit Collection Entry"),
                new ObjectName("Microsoft.Bank.DirectDebit", "Direct Debit Collection Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Applied Payment Entry"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Payment Application Proposal")),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Posted Payment Recon. Hdr")),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation"),
                new ObjectName("Microsoft.Bank.Statement", "Bank Account Statement")),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation Line"),
                new ObjectName("Microsoft.Bank.Reconciliation", "Posted Payment Recon. Line")),
            new TableRelation(
                new ObjectName("Microsoft.Bank.Reconciliation", "Bank Acc. Reconciliation Line"),
                new ObjectName("Microsoft.Bank.Statement", "Bank Account Statement Line")),
            new TableRelation(
                new ObjectName("Microsoft.CRM.BusinessRelation", "Contact Business Relation"),
                new ObjectName("Microsoft.CRM.Outlook", "Office Contact Details")),
            new TableRelation(
                new ObjectName("Microsoft.CRM.Contact", "Contact"),
                new ObjectName("Microsoft.Purchases.Vendor", "Vendor")),
            new TableRelation(
                new ObjectName("Microsoft.EServices.EDocument", "Incoming Document Attachment"),
                new ObjectName("Microsoft.EServices.EDocument", "Inc. Doc. Attachment Overview")),
            new TableRelation(
                new ObjectName("Microsoft.EServices.EDocument", "Incoming Document Attachment"),
                new ObjectName("Microsoft.Integration.Graph", "Attachment Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Analysis", "Analysis by Dim. User Param."),
                new ObjectName("Microsoft.Finance.Analysis", "Analysis by Dim. Parameters")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Header"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Header Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Posted Deferral Header"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Header")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.Deferral", "Posted Deferral Line"),
                new ObjectName("Microsoft.Finance.Deferral", "Deferral Line")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Account", "G/L Account"),
                new ObjectName("Microsoft.Finance.Analysis", "G/L Account (Analysis View)")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Batch"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Posted Gen. Journal Batch")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Posted Gen. Journal Line"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Standard General Journal Line"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Journal", "Gen. Journal Line")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.GeneralLedger.Ledger", "G/L Entry"),
                new ObjectName("Microsoft.Finance.GeneralLedger.Ledger", "G/L Entry Posting Preview")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new ObjectName("Microsoft.HumanResources.Payables", "Detailed Employee Ledger Entry")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new ObjectName("Microsoft.Purchases.Payables", "Detailed Vendor Ledg. Entry")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "Detailed CV Ledg. Entry Buffer"),
                new ObjectName("Microsoft.Sales.Receivables", "Detailed Cust. Ledg. Entry")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.VAT.Ledger", "VAT Entry"),
                new ObjectName("Microsoft.Finance.VAT.Ledger", "VAT Entry Posting Preview")),
            new TableRelation(
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Setup Posting Groups"),
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Posting Setup")),
            new TableRelation(
                new ObjectName("Microsoft.Foundation.Comment", "Comment Line"),
                new ObjectName("Microsoft.Foundation.Comment", "Comment Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Integration.D365Sales", "CDS Available Virtual Table"),
                new ObjectName("Microsoft.Integration.D365Sales", "CDS Av. Virtual Table Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Area Buffer"),
                new ObjectName("Microsoft.Finance.SalesTax", "Tax Area")),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Area Buffer"),
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Business Posting Group")),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Group Buffer"),
                new ObjectName("Microsoft.Finance.SalesTax", "Tax Group")),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Entity", "Tax Group Buffer"),
                new ObjectName("Microsoft.Finance.VAT.Setup", "VAT Product Posting Group")),
            new TableRelation(
                new ObjectName("Microsoft.Integration.Graph", "Attachment Entity Buffer"),
                new ObjectName("Microsoft.Integration.Graph", "Unlinked Attachment")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Comment", "IC Comment Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Comment Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Dimension", "IC Document Dimension"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Document Dimension")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Dimension", "IC Inbox/Outbox Jnl. Line Dim."),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC InOut Jnl. Line Dim.")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Jnl. Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Jnl. Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Jnl. Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Header"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Purch Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Header"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Purch. Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Purch. Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Purchase Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Sales Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Transaction"),
                new ObjectName("Microsoft.Intercompany.DataExchange", "Buffer IC Inbox Transaction")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Inbox", "IC Inbox Transaction"),
                new ObjectName("Microsoft.Intercompany.Inbox", "Handled IC Inbox Trans.")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Jnl. Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Jnl. Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Purch. Hdr"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Purch. Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Sales Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Sales Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Transaction"),
                new ObjectName("Microsoft.Intercompany.Outbox", "Handled IC Outbox Trans.")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Order Line"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Order Line")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Order Hdr"),
                new ObjectName("Microsoft.Inventory.Counting.Document", "Phys. Invt. Order Header")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Recording", "Phys. Invt. Record Header"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Record Hdr")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Recording", "Phys. Invt. Record Line"),
                new ObjectName("Microsoft.Inventory.Counting.History", "Pstd. Phys. Invt. Record Line")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Exp. Invt. Order Tracking"),
                new ObjectName("Microsoft.Inventory.Counting.Tracking", "Pstd.Exp.Invt.Order.Tracking")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Receipt Header"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Header")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Receipt Line"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Line")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Shipment Header"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Header")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.History", "Invt. Shipment Line"),
                new ObjectName("Microsoft.Inventory.Document", "Invt. Document Line")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Item", "Copy Item Parameters"),
                new ObjectName("Microsoft.Inventory.Item", "Copy Item Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Item", "Item"),
                new ObjectName("Microsoft.Inventory.Item", "Item Templ.")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Item", "Item Entry Relation"),
                new ObjectName("Microsoft.Warehouse.Ledger", "Whse. Item Entry Relation")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Journal", "Item Journal Line"),
                new ObjectName("Microsoft.Inventory.Journal", "Standard Item Journal Line")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Ledger", "Item Application Entry"),
                new ObjectName("Microsoft.Inventory.Ledger", "Item Application Entry History")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Tracking", "Reservation Entry"),
                new ObjectName("Microsoft.Inventory.Tracking", "Tracking Specification")),
            new TableRelation(
                new ObjectName("Microsoft.Inventory.Tracking", "Tracking Specification"),
                new ObjectName("Microsoft.Warehouse.Tracking", "Whse. Item Tracking Line")),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.ProductionBOM", "Production BOM Comment Line"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Comp. Cmt Line")),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Comment Line"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Rtng Comment Line")),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Personnel"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Routing Personnel")),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Quality Measure"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Rtng Qlty Meas.")),
            new TableRelation(
                new ObjectName("Microsoft.Manufacturing.Routing", "Routing Tool"),
                new ObjectName("Microsoft.Manufacturing.Document", "Prod. Order Routing Tool")),
            new TableRelation(
                new ObjectName("Microsoft.Pricing.Worksheet", "Price Worksheet Line"),
                new ObjectName("Microsoft.Pricing.PriceList", "Price List Line")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Job", "Job"),
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Job", "Job Task"),
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Task Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.Project.Planning", "Job Planning Line"),
                new ObjectName("Microsoft.Projects.Project.Archive", "Job Planning Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Comment Line"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Cmt. Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail"),
                new ObjectName("Microsoft.Integration.Graph", "Employee Time Reg Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Detail Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Header"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Header Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Line Archive"),
                new ObjectName("Microsoft.Projects.TimeSheet", "Time Sheet Line")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Comment", "Purch. Comment Line"),
                new ObjectName("Microsoft.Purchases.Archive", "Purch. Comment Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Cr. Memo Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Entity Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purchase Order Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Header")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header"),
                new ObjectName("Microsoft.Purchases.Archive", "Purchase Header Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Line Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line"),
                new ObjectName("Microsoft.Purchases.Archive", "Purchase Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Hdr."),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Cr. Memo Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Hdr."),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Line"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Line Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Cr. Memo Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Header"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Entity Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Header"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Line"),
                new ObjectName("Microsoft.Integration.Entity", "Purch. Inv. Line Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Inv. Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Rcpt. Header"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Purch. Rcpt. Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Return Shipment Header"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Header")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.History", "Return Shipment Line"),
                new ObjectName("Microsoft.Purchases.Document", "Purchase Line")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Payables", "Vendor Ledger Entry"),
                new ObjectName("Microsoft.Purchases.Payables", "Vendor Ledger Entry Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Purchases.Vendor", "Vendor"),
                new ObjectName("Microsoft.Purchases.Vendor", "Vendor Templ.")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Comment", "Sales Comment Line"),
                new ObjectName("Microsoft.Sales.Archive", "Sales Comment Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Customer", "Customer"),
                new ObjectName("Microsoft.CRM.Contact", "Contact")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Customer", "Customer"),
                new ObjectName("Microsoft.Sales.Customer", "Customer Templ.")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Cr. Memo Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Entity Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Order Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Quote Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Header"),
                new ObjectName("Microsoft.Sales.Archive", "Sales Header Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Line Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Document", "Sales Line"),
                new ObjectName("Microsoft.Sales.Archive", "Sales Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Finance Charge Memo Header"),
                new ObjectName("Microsoft.Sales.FinanceCharge", "Issued Fin. Charge Memo Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.FinanceCharge", "Finance Charge Memo Line"),
                new ObjectName("Microsoft.Sales.FinanceCharge", "Issued Fin. Charge Memo Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Return Receipt Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Return Receipt Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Cr. Memo Entity Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Line Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Cr.Memo Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Entity Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.Integration.Entity", "Sales Invoice Line Aggregate")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.Intercompany.Outbox", "IC Outbox Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Invoice Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Header"),
                new ObjectName("Microsoft.Sales.Document", "Sales Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.History", "Sales Shipment Line"),
                new ObjectName("Microsoft.Sales.Document", "Sales Line")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Receivables", "Cust. Ledger Entry"),
                new ObjectName("Microsoft.Finance.ReceivablesPayables", "CV Ledger Entry Buffer")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Reminder", "Reminder Header"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Header")),
            new TableRelation(
                new ObjectName("Microsoft.Sales.Reminder", "Reminder Line"),
                new ObjectName("Microsoft.Sales.Reminder", "Issued Reminder Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Archive", "Service Comment Line Archive"),
                new ObjectName("Microsoft.Service.Comment", "Service Comment Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Comment", "Service Comment Line"),
                new ObjectName("Microsoft.Service.Contract", "Filed Serv. Contract Cmt. Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Contract/Service Discount"),
                new ObjectName("Microsoft.Service.Contract", "Filed Contract/Serv. Discount")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Service Contract Header"),
                new ObjectName("Microsoft.Service.Contract", "Filed Service Contract Header")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Service Contract Line"),
                new ObjectName("Microsoft.Service.Contract", "Filed Contract Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Contract", "Service Hour"),
                new ObjectName("Microsoft.Service.Contract", "Filed Contract Service Hour")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.Archive", "Service Header Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Header")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.History", "Service Invoice Header")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Header"),
                new ObjectName("Microsoft.Service.History", "Service Shipment Header")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Item Line"),
                new ObjectName("Microsoft.Service.Archive", "Service Item Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Item Line"),
                new ObjectName("Microsoft.Service.History", "Service Shipment Item Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.Archive", "Service Line Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.History", "Service Cr.Memo Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.History", "Service Invoice Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Line"),
                new ObjectName("Microsoft.Service.History", "Service Shipment Line")),
            new TableRelation(
                new ObjectName("Microsoft.Service.Document", "Service Order Allocation"),
                new ObjectName("Microsoft.Service.Archive", "Service Order Allocat. Archive")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.Activity.History", "Registered Whse. Activity Hdr.")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Pick Header")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Put-away Header")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Header"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Registered Invt. Movement Hdr.")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.Activity.History", "Registered Whse. Activity Line")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Pick Line")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Posted Invt. Put-away Line")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Activity", "Warehouse Activity Line"),
                new ObjectName("Microsoft.Warehouse.InventoryDocument", "Registered Invt. Movement Line")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Document", "Warehouse Receipt Header"),
                new ObjectName("Microsoft.Warehouse.History", "Posted Whse. Receipt Header")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Document", "Warehouse Receipt Line"),
                new ObjectName("Microsoft.Warehouse.History", "Posted Whse. Receipt Line")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Document", "Warehouse Shipment Line"),
                new ObjectName("Microsoft.Warehouse.History", "Posted Whse. Shipment Line")),
            new TableRelation(
                new ObjectName("Microsoft.Warehouse.Tracking", "Whse. Item Tracking Line"),
                new ObjectName("Microsoft.Inventory.Tracking", "Reservation Entry")),
            new TableRelation(
                new ObjectName("System.Automation", "Approval Comment Line"),
                new ObjectName("System.Automation", "Posted Approval Comment Line")),
            new TableRelation(
                new ObjectName("System.Automation", "Approval Entry"),
                new ObjectName("System.Automation", "Posted Approval Entry")),
            new TableRelation(
                new ObjectName("System.Automation", "Workflow - Record Change"),
                new ObjectName("System.Automation", "Workflow Record Change Archive")),
            new TableRelation(
                new ObjectName("System.Automation", "Workflow Step Argument Archive"),
                new ObjectName("System.Automation", "Workflow Step Argument")),
            new TableRelation(
                new ObjectName("System.Automation", "Workflow Step Instance"),
                new ObjectName("System.Automation", "Workflow Step Instance Archive")),
            new TableRelation(
                new ObjectName("System.Environment.Configuration", "Notification Entry"),
                new ObjectName("System.Environment.Configuration", "Sent Notification Entry")),
            new TableRelation(
                new ObjectName("System.Feedback", "Onboarding Signal"),
                new ObjectName("System.Feedback", "Onboarding Signal Buffer")),
            new TableRelation(
                new ObjectName("System.Integration", "API Webhook Notification"),
                new ObjectName("System.Integration", "API Webhook Notification Aggr")),
            new TableRelation(
                new ObjectName("System.IO", "Config. Setup"),
                new ObjectName("Microsoft.Foundation.Company", "Company Information")),
            new TableRelation(
                new ObjectName("System.Security.AccessControl", "Aggregate Permission Set"),
                new ObjectName("System.Security.AccessControl", "Permission Set Buffer")),
            new TableRelation(
                new ObjectName("System.Utilities", "Date"),
                new ObjectName("System.DateTime", "Date Lookup Buffer"))
        );

    public static TableRelation? TryFindTableRelation(ITableTypeSymbol table)
    {
        foreach (var item in TableRelations)
        {
            if (Matches(item.Table, table) || Matches(item.RelatedTable, table))
                return item;
        }

        return null;
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
}