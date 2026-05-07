codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata "My Table" = md|];

    var
        MyTable: Record "My Table";

    trigger OnRun()
    begin
        MyTable.Modify(false);
        MyTable.Delete(false);
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
