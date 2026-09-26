// Purchase Header Archive -> Purchase Header is a curated TransferFields relation. The tables
// have no namespace (pre-namespace Base App), so ownership falls back to the module publisher,
// which the test fixture sets to "Microsoft". Field 151 differs by name in the Base App.
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
        [|field(151; "Quote No."; Code[20]) { }|]
    }
}

table 5109 "Purchase Header Archive"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        [|field(151; "Purchase Quote No."; Code[20]) { }|]
    }
}
