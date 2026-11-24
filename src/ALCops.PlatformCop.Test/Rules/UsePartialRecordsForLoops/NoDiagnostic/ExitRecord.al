codeunit 50100 MyCodeunit
{
    procedure GetRecord(): Record MyTable
    var
        MyTable: Record MyTable;
        Total: Integer;
    begin
        [|MyTable.FindSet()|];
        repeat
            Total += MyTable.MyField;
        until MyTable.Next() = 0;
        exit(MyTable);
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
