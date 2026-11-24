codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Integer
    var
        MyTable: Record MyTable;
        Condition: Boolean;
        Total: Integer;
    begin
        if Condition then
            MyTable.SetLoadFields(MyTable.MyField)
        else
            MyTable.SetLoadFields(MyTable.MyField);
        [|MyTable.FindSet()|];
        repeat
            Total += MyTable.MyField;
        until MyTable.Next() = 0;
        exit(Total);
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
