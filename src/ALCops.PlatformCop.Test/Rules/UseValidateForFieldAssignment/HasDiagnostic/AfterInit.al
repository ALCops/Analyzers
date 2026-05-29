codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SalesLine: Record "Sales Line";
    begin
        SalesLine.Init();
        SalesLine.[|"Document No."|] := 'DOC001';
        SalesLine.[|"Unit Price"|] := 100;
        SalesLine.Insert(true);
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
