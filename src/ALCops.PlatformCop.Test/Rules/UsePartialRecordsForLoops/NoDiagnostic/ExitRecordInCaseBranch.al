codeunit 50100 MyCodeunit
{
    procedure GetRecordByMode(Mode: Integer): Record MyTable
    var
        MyTable: Record MyTable;
        Total: Integer;
    begin
        case Mode of
            1:
                begin
                    if [|MyTable.FindSet()|] then
                        repeat
                            Total += MyTable.MyField;
                        until MyTable.Next() = 0;
                    exit(MyTable);
                end;
            2:
                begin
                    if [|MyTable.Find('+')|] then
                        repeat
                            Total += MyTable.MyField;
                        until MyTable.Next(-1) = 0;
                    exit(MyTable);
                end;
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
