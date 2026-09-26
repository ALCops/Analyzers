// Both tables are Microsoft's, but Purchase Header Archive -> Customer is not a curated
// TransferFields relation: every mismatch is reported, at the call and on both fields.
codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        Customer: Record Customer;
        PurchaseHeaderArchive: Record "Purchase Header Archive";
    begin
        [|Customer.TransferFields(PurchaseHeaderArchive)|];
    end;
}

table 18 Customer
{
    fields
    {
        field(1; "No."; Code[20]) { }
        [|field(21; "Currency Code"; Code[10]) { }|]
    }
}

table 5109 "Purchase Header Archive"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        [|field(21; "Posting Date"; Date) { }|]
    }
}
