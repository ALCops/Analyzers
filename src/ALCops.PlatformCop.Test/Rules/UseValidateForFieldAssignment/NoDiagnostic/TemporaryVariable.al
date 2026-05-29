codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TempSalesLine: Record "Sales Line" temporary;
    begin
        TempSalesLine.[|"Unit Price"|] := 100;
    end;
}

table 50100 "Sales Line"
{
    fields
    {
        field(1; "Document No."; Code[20]) { }
        field(2; "Unit Price"; Decimal) { }
    }
}
