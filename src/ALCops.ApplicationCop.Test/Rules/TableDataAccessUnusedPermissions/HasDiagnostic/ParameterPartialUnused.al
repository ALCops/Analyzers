codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata "My Table" = rimd|];

    internal procedure ModifyRecord(var MyRecord: Record "My Table")
    begin
        MyRecord.Modify(false);
    end;
}

table 50000 "My Table"
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
