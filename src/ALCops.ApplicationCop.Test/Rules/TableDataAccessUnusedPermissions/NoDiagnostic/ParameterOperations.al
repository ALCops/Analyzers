codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata "My Table" = md|];

    internal procedure ModifyRecord(var MyRecord: Record "My Table")
    begin
        MyRecord.Modify(false);
    end;

    internal procedure DeleteRecord(var MyRecord: Record "My Table")
    begin
        MyRecord.Delete(false);
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
