codeunit 50100 MyCodeunit
{
    procedure GetSelectedRecord(): Record MyTable
    var
        MyTable: Record MyTable;
        SelectedTable: Record MyTable;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                SelectedTable.Get(MyTable.MyField);
            until MyTable.Next() = 0;
        exit(SelectedTable);
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
