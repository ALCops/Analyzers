codeunit 50000 MyCodeunit
{
    Permissions = [|tabledata MyTable = r|];

    internal procedure SumField(): Decimal
    var
        MyRecord: Record MyTable temporary;
    begin
        MyRecord.CalcSums(MyField);
        exit(MyRecord.MyField);
    end;
}

table 50000 MyTable
{
    Caption = '', Locked = true;

    fields
    {
        field(1; MyKey; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
        field(2; MyField; Decimal)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
