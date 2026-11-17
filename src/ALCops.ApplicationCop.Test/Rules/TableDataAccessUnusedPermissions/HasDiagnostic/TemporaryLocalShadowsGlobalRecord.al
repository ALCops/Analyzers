codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure ReadRecord(): Boolean
    begin
        exit(MyTable.FindFirst());
    end;

    internal procedure ModifyTemporaryLocal()
    var
        MyTable: Record MyTable temporary;
    begin
        MyTable.Modify();
    end;

    internal procedure ModifyTemporaryParameter(var MyTable: Record MyTable temporary)
    begin
        MyTable.Modify();
    end;

    var
        MyTable: Record MyTable;
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
