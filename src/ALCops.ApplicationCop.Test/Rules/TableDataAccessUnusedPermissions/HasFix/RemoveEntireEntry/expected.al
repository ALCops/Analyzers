codeunit 50000 MyCodeunit
{
    Permissions = tabledata OtherTable = r;

    trigger OnRun()
    var
        OtherTable: Record OtherTable;
    begin
        OtherTable.FindFirst();
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

table 50001 OtherTable
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
