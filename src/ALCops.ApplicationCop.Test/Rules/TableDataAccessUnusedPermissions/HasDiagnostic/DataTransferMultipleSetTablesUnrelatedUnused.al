codeunit 50000 MyCodeunit
{
    Permissions = tabledata TableA = rm,
                  tabledata TableB = rm,
                  [|tabledata MyOtherTable = d|];

    internal procedure CopyValues(UseSecond: Boolean)
    var
        MyDataTransfer: DataTransfer;
    begin
        if UseSecond then
            MyDataTransfer.SetTables(Database::TableB, Database::TableB)
        else
            MyDataTransfer.SetTables(Database::TableA, Database::TableA);
        MyDataTransfer.AddFieldValue(1, 2);
        MyDataTransfer.CopyFields();
    end;
}

table 50000 TableA
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

table 50001 TableB
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

table 50002 MyOtherTable
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
