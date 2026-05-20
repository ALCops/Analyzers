codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    local procedure MyProcedure() MyTable: Record MyTable;
    begin
        MyTable.Get(1);
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
