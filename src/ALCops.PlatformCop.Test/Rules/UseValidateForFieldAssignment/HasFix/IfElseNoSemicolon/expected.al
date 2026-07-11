codeunit 50100 MyCodeunit
{
    procedure MyProcedure(NewPrice: Decimal)
    var
        SalesLine: Record "Sales Line";
    begin
        if NewPrice > 0 then
            SalesLine.Validate("Unit Price", NewPrice)
        else begin
        end;
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
