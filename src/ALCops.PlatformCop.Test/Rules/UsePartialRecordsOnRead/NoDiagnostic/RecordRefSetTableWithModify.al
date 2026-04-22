codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyRecordID: RecordID)
    var
        MyTable: Record MyTable;
        RecordRef: RecordRef;
    begin
        [|RecordRef.Get(MyRecordID)|];
        RecordRef.SetTable(MyTable);
        MyTable.Validate(MyField, 1);
        MyTable.Modify(true);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
