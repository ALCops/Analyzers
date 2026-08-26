codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata "My Table" = md|];

    internal procedure DoModify(var RecordRefToModify: RecordRef; RunTrigger: Boolean)
    begin
        RecordRefToModify.Modify(RunTrigger);
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
