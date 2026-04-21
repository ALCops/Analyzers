codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        Condition: Boolean;
    begin
        if Condition then begin
            MyTable.SetLoadFields(MyTable.MyField);
            MyTable.Get();
        end else
            [|MyTable.Get()|];
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    keys
    {
        key(PK; MyField) { }
    }
}
