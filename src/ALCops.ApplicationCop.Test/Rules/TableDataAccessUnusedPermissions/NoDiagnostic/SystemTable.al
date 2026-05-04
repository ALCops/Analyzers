codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MySystemTable = r|];

    trigger OnRun()
    var
        MySystemTable: Record MySystemTable;
    begin
        MySystemTable.Get();
    end;
}

table 2000000168 MySystemTable
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
