codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTempTable = rimd|];

    trigger OnRun()
    var
        MyTempTable: Record MyTempTable;
    begin
        MyTempTable.Insert();
        MyTempTable.Modify();
        MyTempTable.FindFirst();
        MyTempTable.Delete();
    end;
}

table 50000 MyTempTable
{
    Caption = '', Locked = true;
    TableType = Temporary;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
