codeunit 50100 MyCodeunit
{
    procedure FindRecord(ShouldFind: Boolean): Record MyTable
    var
        MyTable: Record MyTable;
        Total: Integer;
    begin
        if ShouldFind then begin
            if [|MyTable.FindSet()|] then
                repeat
                    Total += MyTable.MyField;
                until MyTable.Next() = 0;
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
