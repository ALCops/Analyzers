codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure ReadRecord(): Boolean
    begin
        exit(MyTable.FindFirst());
    end;

    internal procedure ModifyHelper()
    var
        MyTable: Codeunit MyHelper;
    begin
        MyTable.Modify();
    end;

    var
        MyTable: Record MyTable;
}

codeunit 50001 MyHelper
{
    internal procedure Modify()
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
