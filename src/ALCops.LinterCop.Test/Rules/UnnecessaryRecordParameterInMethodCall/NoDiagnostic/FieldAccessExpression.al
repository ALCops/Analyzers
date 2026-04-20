table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    procedure MyProcedure(MyParam: Text)
    begin
    end;
}

codeunit 50100 MyCodeunit
{
    procedure CallMyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.MyProcedure([|MyTable."Name"|]);
    end;
}
