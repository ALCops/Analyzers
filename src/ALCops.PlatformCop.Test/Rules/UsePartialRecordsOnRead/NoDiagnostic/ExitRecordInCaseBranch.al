codeunit 50100 MyCodeunit
{
    procedure GetRecordByMode(Mode: Integer): Record MyTable
    var
        MyTable: Record MyTable;
    begin
        case Mode of
            1:
                begin
                    [|MyTable.FindFirst()|];
                    exit(MyTable);
                end;
            2:
                begin
                    [|MyTable.FindLast()|];
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
