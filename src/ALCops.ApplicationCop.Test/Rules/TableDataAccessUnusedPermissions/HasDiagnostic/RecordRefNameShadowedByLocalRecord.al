codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure ReadRecord(): Boolean
    var
        RecordRef: Record MyTable;
    begin
        exit(RecordRef.FindFirst());
    end;

    var
        RecordRef: RecordRef;
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
