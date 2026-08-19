codeunit 50100 MyCodeunit
{
    procedure GetRecord(EntryNo: Integer): Record MyTable
    var
        MyTable: Record MyTable;
    begin
        if EntryNo = 0 then begin
            [|MyTable.FindFirst()|];
            Error('EntryNo is empty. Defaulting to entry: %1', MyTable.MyField);
        end else begin
            MyTable.Get(EntryNo);
            exit(MyTable);
        end;
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
