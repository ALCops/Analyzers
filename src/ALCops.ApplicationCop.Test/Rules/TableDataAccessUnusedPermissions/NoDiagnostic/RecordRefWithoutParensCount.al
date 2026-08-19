codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    internal procedure CountAnyTable(TableNo: Integer): Integer
    var
        MyRecordRef: RecordRef;
    begin
        MyRecordRef.Open(TableNo);
        exit(MyRecordRef.Count);
    end;
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
