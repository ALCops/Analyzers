codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = rd;

    internal procedure DeleteUnfiltered(var MyRecord: Record MyTable)
    begin
        MyRecord.DeleteAll(true);
    end;
}

table 50000 MyTable
{
    Caption = '',  Locked = true;

    fields
    {
        field(1; MyKey; Integer)
        {
            Caption = '',  Locked = true;
            DataClassification = ToBeClassified;
        }
        field(2; MyField; Integer)
        {
            Caption = '',  Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
