codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = d;

    internal procedure DeleteRecord(var MyRecord: Record MyTable)
    begin
        [|MyRecord.Delete(true);|]
        [|MyRecord.Delete(false);|]
    end;
}

codeunit 50001 MyModifier
{
    Permissions = tabledata MyTable = m;

    internal procedure ModifyRecord(var MyRecord: Record MyTable)
    begin
        [|MyRecord.Modify(true);|]
        [|MyRecord.Modify(false);|]
    end;
}

codeunit 50002 MyInserter
{
    Permissions = tabledata MyTable = i;

    internal procedure InsertRecord(var MyRecord: Record MyTable)
    begin
        [|MyRecord.Insert(true);|]
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
