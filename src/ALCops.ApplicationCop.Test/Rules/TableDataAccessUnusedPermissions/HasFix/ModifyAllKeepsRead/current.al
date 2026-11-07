codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rimd|];

    internal procedure ModifyUnfiltered(var MyRecord: Record MyTable)
    begin
        MyRecord.ModifyAll(MyField, 1, true);
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
