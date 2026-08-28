codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = rm,
                  [|tabledata MyOtherTable = d|];

    internal procedure CopyValues()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::MyTable, Database::MyTable);
        MyDataTransfer.AddFieldValue(1, 2);
        MyDataTransfer.CopyFields;
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

table 50001 MyOtherTable
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
