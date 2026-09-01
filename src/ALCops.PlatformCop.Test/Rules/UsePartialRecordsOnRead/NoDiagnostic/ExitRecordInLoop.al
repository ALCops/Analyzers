codeunit 50100 MyCodeunit
{
    procedure FindMatchingRecord(TargetValue: Integer): Record MyTable
    var
        MyTable: Record MyTable;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                if MyTable.MyField = TargetValue then
                    exit(MyTable);
            until MyTable.Next() = 0;
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
