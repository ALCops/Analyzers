codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        CdsAccount: Record "CDS Account";
    begin
        CdsAccount.[|"Unit Price"|] := 100;
    end;
}

table 50100 "CDS Account"
{
    TableType = CDS;

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
