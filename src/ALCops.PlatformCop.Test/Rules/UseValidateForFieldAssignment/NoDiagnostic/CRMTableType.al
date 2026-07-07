codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        CrmAccount: Record "CRM Account";
    begin
        CrmAccount.[|"Unit Price"|] := 100;
    end;
}

table 50100 "CRM Account"
{
    TableType = CRM;

    fields
    {
        field(1; "Document No."; Code[20]) { }
        field(2; "Unit Price"; Decimal) { }
    }

    keys
    {
        key(PK; "Document No.") { }
    }
}
