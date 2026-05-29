codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SalesLine: Record "Sales Line";
    begin
        SalesLine.[|"Document No."|] := 'DOC001';
    end;
}

table 50100 "Sales Line"
{
    fields
    {
        field(1; "Document No."; Code[20]) { }
    }
    keys
    {
        key(PK; "Document No.") { Clustered = true; }
    }
}
