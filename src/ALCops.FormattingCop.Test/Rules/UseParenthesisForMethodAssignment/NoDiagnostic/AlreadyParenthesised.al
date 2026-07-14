codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyOther: Codeunit MyOther;
    begin
        [|MyTable.ReadIsolation(IsolationLevel::ReadCommitted)|];
        [|MyOther.SetValue(5)|];
    end;
}

codeunit 50101 MyOther
{
    procedure SetValue(NewValue: Integer)
    begin
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
