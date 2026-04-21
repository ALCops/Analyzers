codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        Condition: Boolean;
    begin
        MyTable.SetLoadFields(MyTable.MyField);
        while Condition do
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
