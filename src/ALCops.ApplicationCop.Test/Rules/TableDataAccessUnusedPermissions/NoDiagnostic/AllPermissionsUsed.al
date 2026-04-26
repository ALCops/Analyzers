codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rimd|];

    trigger OnRun()
    var
        MyTable: Record MyTable;
    begin
        MyTable.FindFirst();
        MyTable.Insert();
        MyTable.Modify();
        MyTable.Delete();
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
