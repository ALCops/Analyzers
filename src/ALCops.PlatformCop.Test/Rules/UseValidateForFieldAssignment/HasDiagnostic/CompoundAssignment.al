codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SalesLine: Record "Sales Line";
    begin
        SalesLine.[|Quantity|] += 1;
    end;
}

table 50100 "Sales Line"
{
    fields
    {
        field(1; "Document No."; Code[20]) { }
        field(2; Quantity; Decimal) { }
    }
}
