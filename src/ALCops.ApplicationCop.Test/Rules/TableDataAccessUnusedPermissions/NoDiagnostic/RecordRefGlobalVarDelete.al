codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = d|];

    internal procedure DeleteAnyRecord(TableNo: Integer)
    begin
        GlobalRecordRef.Open(TableNo);
        GlobalRecordRef.DeleteAll(false);
    end;

    var
        GlobalRecordRef: RecordRef;
}

table 50000 MyTable
{
    Caption = '', Locked = true;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
