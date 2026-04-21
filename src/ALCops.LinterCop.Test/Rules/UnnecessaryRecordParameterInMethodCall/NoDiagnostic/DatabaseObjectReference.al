table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    procedure MyProcedure(TableNo: Integer; var MyTableParam: Record MyTable)
    begin
    end;
}

codeunit 50100 MyCodeunit
{
    procedure CallMyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.MyProcedure([|DATABASE::MyTable|], MyTable);
    end;
}
