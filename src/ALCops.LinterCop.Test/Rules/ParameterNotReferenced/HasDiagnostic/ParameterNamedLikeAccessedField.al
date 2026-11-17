table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

codeunit 50100 ParamLikeAccessedField
{
    procedure MyProcedure([|Name: Text|]): Text
    var
        MyTable: Record MyTable;
    begin
        exit(MyTable.Name);
    end;
}
