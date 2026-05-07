codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = md;

    trigger OnRun()
    var
        MyTable: Record MyTable;
    begin
        MyTable.Modify(false);
        MyTable.Delete(false);
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
