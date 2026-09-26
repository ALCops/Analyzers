// Same curated pair as CuratedPair_MicrosoftPublisher, but the module is not Microsoft's
// ("Default Publisher", no namespace): tables that merely share a curated name are not the
// Base App tables, so the mismatch is reported at the call. Field-level reporting stays off
// for curated pairs.
codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        PurchaseHeader: Record "Purchase Header";
        PurchaseHeaderArchive: Record "Purchase Header Archive";
    begin
        [|PurchaseHeader.TransferFields(PurchaseHeaderArchive)|];
    end;
}

table 38 "Purchase Header"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(5043; "No. of Archived Versions"; Integer) { }
    }
}

table 5109 "Purchase Header Archive"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(5043; "Interaction Exist"; Boolean) { }
    }
}
