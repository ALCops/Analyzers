codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        myInt: Integer;
    begin
        myInt := MyTable.Count;
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
