table 50100 MyTable
{
    fields
    {
        field(1; "No."; Integer) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50100 ParamUsedAsRecordReceiver
{
    procedure MyProcedure(var [|MyTable: Record MyTable|])
    begin
        MyTable.SetRange(Name, 'x');
        MyTable.Get(1);
    end;
}
