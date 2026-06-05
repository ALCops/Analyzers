codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        result: Boolean;
    begin
        result := MyTable.IsEmpty;
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
