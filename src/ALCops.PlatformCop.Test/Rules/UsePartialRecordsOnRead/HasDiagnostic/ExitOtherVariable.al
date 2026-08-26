codeunit 50100 MyCodeunit
{
    procedure GetSelectedRecord(EntryNo: Integer): Record MyTable
    var
        MyTable: Record MyTable;
        SelectedTable: Record MyTable;
    begin
        [|MyTable.Get(EntryNo)|];
        SelectedTable.Get(MyTable.MyField);
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
