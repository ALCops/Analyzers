codeunit 50000 MyCodeunit
{

    internal procedure SumField(var MyRecord: Record MyTable): Decimal
    begin
        MyRecord.SetRange(MyField, 1, 100);
        [|MyRecord.CalcSums(MyField);|]
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
