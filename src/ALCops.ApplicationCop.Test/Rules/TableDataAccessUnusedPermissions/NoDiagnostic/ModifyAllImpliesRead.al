codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = rm|];

    internal procedure ModifyUnfiltered(var MyRecord: Record MyTable)
    begin
        MyRecord.ModifyAll(MyField, 1, true);
    end;

    internal procedure ModifyFiltered(var MyRecord: Record MyTable)
    begin
        MyRecord.SetRange(MyKey, 1, 100);
        MyRecord.ModifyAll(MyField, 1, false);
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
