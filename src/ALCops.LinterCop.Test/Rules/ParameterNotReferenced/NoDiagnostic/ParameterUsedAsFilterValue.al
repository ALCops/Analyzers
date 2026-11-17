table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

codeunit 50100 ParamUsedAsFilterValue
{
    procedure MyProcedure([|Name: Text[100]|])
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetRange(Name, Name);
    end;
}
