codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure CopyValues()
    var
        MyHelper: Record HelperTable;
    begin
        MyHelper.CopyRows();
    end;
}

table 50001 HelperTable
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

    internal procedure CopyRows()
    begin
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
