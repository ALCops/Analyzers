table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

codeunit 50100 ParamUsedAsSetRangeField
{
    procedure MyProcedure([|Name: Text|])
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetRange(Name, 'x');
    end;
}
