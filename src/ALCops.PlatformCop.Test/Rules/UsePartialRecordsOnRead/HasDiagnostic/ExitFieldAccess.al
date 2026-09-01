codeunit 50100 MyCodeunit
{
    procedure GetFieldValue(EntryNo: Integer): Integer
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.Get(EntryNo)|];
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
