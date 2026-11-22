codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyRecordRef: RecordRef;
    begin
        MyRecordRef.Open(Database::MyTable);
        if [|MyRecordRef.FindSet()|] then
            repeat
                MyRecordRef.SetTable(MyTable);
                ProcessRecord(MyTable);
            until MyRecordRef.Next() = 0;
    end;

    local procedure ProcessRecord(var MyTable: Record MyTable)
    begin
        MyTable.Validate(MyField, 1);
        MyTable.Modify(true);
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
