codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata Alpha = r|],
                  [|tabledata Beta = i|];

    internal procedure CopyValues(SourceTableNo: Integer; DestinationTableNo: Integer)
    var
        MyDataTransfer: DataTransfer;
        Counter: Integer;
    begin
        MyDataTransfer.SetTables(Database::Alpha, Database::Beta);
        repeat
            MyDataTransfer.AddFieldValue(1, 1);
            MyDataTransfer.CopyRows();
            MyDataTransfer.SetTables(SourceTableNo, DestinationTableNo);
            Counter += 1;
        until Counter > 1;
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
