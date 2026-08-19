codeunit 50100 MyCodeunit
{
    procedure FindRecord(ShouldFind: Boolean): Record MyTable
    var
        MyTable: Record MyTable;
    begin
        if ShouldFind then begin
            [|MyTable.FindFirst()|];
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
