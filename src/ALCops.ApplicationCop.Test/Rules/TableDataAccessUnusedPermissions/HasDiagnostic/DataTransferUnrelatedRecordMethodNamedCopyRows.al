codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure CopyValues()
    var
        MyHelper: Codeunit MyHelper;
    begin
        MyHelper.CopyRows();
    end;
}

codeunit 50001 MyHelper
{
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
