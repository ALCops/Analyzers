codeunit 50000 MyCodeunit
{
    TableNo = MyTable;
    Permissions = [|tabledata MyTable = m|];

    trigger OnRun()
    begin
        Modify();
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
