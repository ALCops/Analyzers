table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|Clear(MyTable)|];
    end;
}
