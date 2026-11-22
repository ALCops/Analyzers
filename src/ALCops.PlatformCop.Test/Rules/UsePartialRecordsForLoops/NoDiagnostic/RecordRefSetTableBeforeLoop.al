codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Integer
    var
        MyTable: Record MyTable;
        MyRecordRef: RecordRef;
        Counter: Integer;
    begin
        MyRecordRef.Open(Database::MyTable);
        MyRecordRef.SetTable(MyTable);
        if [|MyRecordRef.FindSet()|] then
            repeat
                Counter += 1;
            until MyRecordRef.Next() = 0;
        exit(Counter);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; MyField; Integer) { }
    }

    keys
    {
        key(PK; "Entry No.") { }
    }
}
