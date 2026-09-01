codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    internal procedure StepNextInAnyTable(TableNo: Integer): Boolean
    var
        MyRecordRef: RecordRef;
    begin
        MyRecordRef.Open(TableNo);
        exit(MyRecordRef.Next() <> 0);
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
