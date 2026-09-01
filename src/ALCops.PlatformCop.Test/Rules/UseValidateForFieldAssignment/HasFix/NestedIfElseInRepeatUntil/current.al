codeunit 50100 MyCodeunit
{
    procedure MyProcedure(NewPrice: Decimal)
    var
        SalesLine: Record "Sales Line";
    begin
        repeat
            if NewPrice > 0 then
                SalesLine.[|"Unit Price"|] := NewPrice
            else
                NewPrice += 1;
        until NewPrice > 10;
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
