codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTable: Record MyTable;
        Id: Guid;
    begin
        [|MyTable.GetBySystemId(Id);|]
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
