codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata Alpha = r|],
                  [|tabledata Beta = i|],
                  [|tabledata Charlie = r|],
                  [|tabledata Delta = i|];

    internal procedure CopyValues(Stop: Boolean)
    var
        MyDataTransfer: DataTransfer;
        Counter: Integer;
    begin
        while Counter < 2 do begin
            MyDataTransfer.SetTables(Database::Alpha, Database::Beta);
            if Stop then
                break;
            MyDataTransfer.SetTables(Database::Charlie, Database::Delta);
            Counter += 1;
        end;
        MyDataTransfer.AddFieldValue(1, 1);
        MyDataTransfer.CopyRows();
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

table 50002 Charlie
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

table 50003 Delta
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
