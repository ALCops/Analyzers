codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTempTable: Record "My Temp Buffer";
    begin
        MyTempTable.[|"Unit Price"|] := 100;
    end;
}

table 50100 "My Temp Buffer"
{
    TableType = Temporary;

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
