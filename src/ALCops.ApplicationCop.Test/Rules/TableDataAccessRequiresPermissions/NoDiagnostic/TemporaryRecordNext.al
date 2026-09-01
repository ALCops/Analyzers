codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTempRecord: Record MyTable temporary;
        Total: Integer;
    begin
        repeat
            Total += MyTempRecord.MyField;
        [|until MyTempRecord.Next() = 0;|]
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
