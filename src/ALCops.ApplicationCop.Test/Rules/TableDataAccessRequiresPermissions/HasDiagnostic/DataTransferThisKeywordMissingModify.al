codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = r;

    internal procedure CopyValues()
    begin
        this.GlobalDataTransfer.SetTables(Database::MyTable, Database::MyTable);
        this.GlobalDataTransfer.AddFieldValue(1, 2);
        [|this.GlobalDataTransfer.CopyFields();|]
    end;

    var
        GlobalDataTransfer: DataTransfer;
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
