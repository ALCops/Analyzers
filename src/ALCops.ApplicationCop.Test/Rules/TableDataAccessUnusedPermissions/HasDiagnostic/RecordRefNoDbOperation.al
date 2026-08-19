codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rimd|];

    internal procedure InspectTable(MyRecord: Record MySourceTable): Integer
    var
        MyRecordRef: RecordRef;
    begin
        MyRecordRef.GetTable(MyRecord);
        exit(MyRecordRef.Number);
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

table 50001 MySourceTable
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
