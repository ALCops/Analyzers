codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure ConfigureOnly()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::MyTable, Database::MyTable);
        MyDataTransfer.AddSourceFilter(1, '%1', 0);
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
        field(2; MyOtherField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
