// Purchase Header Archive -> Purchase Header is a curated TransferFields relation. The tables
// have no namespace (pre-namespace Base App), so ownership falls back to the module publisher,
// which the test fixture sets to "Microsoft". In the Base App field 5043 is a FlowField, which the
// rule skips; the fixture models it as Normal fields to exercise the type path.
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
        [|field(5043; "No. of Archived Versions"; Integer) { }|]
    }
}

table 5109 "Purchase Header Archive"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        [|field(5043; "Interaction Exist"; Boolean) { }|]
    }
}
