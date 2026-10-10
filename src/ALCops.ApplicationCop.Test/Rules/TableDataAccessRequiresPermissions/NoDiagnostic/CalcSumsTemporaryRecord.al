codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTempRecord: Record MyTable temporary;
    begin
        [|MyTempRecord.CalcSums(MyField);|]
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
