codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure ModifyTemporaryLocal()
    var
        MyRef: Record MyTable temporary;
    begin
        MyRef.Modify();
    end;

    var
        MyRef: RecordRef;
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
