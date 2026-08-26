codeunit 50100 MyCodeunit
{
    procedure GetRecordOrDefault(EntryNo: Integer): Record MyTable
    var
        MyTable: Record MyTable;
    begin
        if EntryNo = 0 then
            exit(MyTable);

        [|MyTable.Get(EntryNo)|];
        MyTable.TestField(MyField);
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
