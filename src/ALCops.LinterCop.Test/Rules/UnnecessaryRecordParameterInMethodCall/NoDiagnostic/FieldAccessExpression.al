table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    procedure DoSth(MyParam: Text)
    begin
    end;
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.DoSth([|MyTable."Name"|]);
    end;
}
