codeunit 50100 MyCodeunit
{
    procedure GetFieldValue(): Integer
    var
        MyTable: Record MyTable;
        Total: Integer;
    begin
        [|MyTable.FindSet()|];
        repeat
            Total += MyTable.MyField;
        until MyTable.Next() = 0;
        exit(MyTable.MyField);
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
