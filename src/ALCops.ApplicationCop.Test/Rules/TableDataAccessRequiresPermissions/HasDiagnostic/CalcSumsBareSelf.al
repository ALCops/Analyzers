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

    procedure SumField(): Decimal
    begin
        [|CalcSums(MyField);|]
        exit(MyField);
    end;
}
