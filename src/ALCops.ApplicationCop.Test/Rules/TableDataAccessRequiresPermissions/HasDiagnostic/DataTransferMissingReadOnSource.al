codeunit 50000 MyCodeunit
{
    Permissions = tabledata DestinationTable = m;

    trigger OnRun()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::SourceTable, Database::DestinationTable);
        MyDataTransfer.AddFieldValue(1, 1);
        [|MyDataTransfer.CopyFields();|]
    end;
}

table 50000 SourceTable
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

table 50001 DestinationTable
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
