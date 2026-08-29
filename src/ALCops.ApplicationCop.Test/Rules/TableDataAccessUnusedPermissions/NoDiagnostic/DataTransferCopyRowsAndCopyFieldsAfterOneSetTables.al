codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata Alpha = r|],
                  [|tabledata Beta = im|];

    internal procedure CopyValues()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::Alpha, Database::Beta);
        MyDataTransfer.AddFieldValue(1, 1);
        MyDataTransfer.CopyRows();
        MyDataTransfer.AddFieldValue(1, 1);
        MyDataTransfer.CopyFields();
    end;
}

table 50000 Alpha
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

table 50001 Beta
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
